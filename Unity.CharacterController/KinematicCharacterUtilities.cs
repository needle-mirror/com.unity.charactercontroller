using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Material = Unity.Physics.Material;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Unity.CharacterController
{
    /// <summary>
    /// The state of a character hit (enter, exit, stay)
    /// </summary>
    public enum CharacterHitState
    {
        /// <summary>
        /// The hit has been entered
        /// </summary>
        Enter,
        /// <summary>
        /// The hit is being detected
        /// </summary>
        Stay,
        /// <summary>
        /// The hit has been exited
        /// </summary>
        Exit,
    }

    /// <summary>
    /// Identifier for a type of grounding evaluation
    /// </summary>
    public enum GroundingEvaluationType
    {
        /// <summary>
        /// General-purpose grounding evaluation
        /// </summary>
        Default,
        /// <summary>
        /// Grounding evaluation for the ground probing phase
        /// </summary>
        GroundProbing,
        /// <summary>
        /// Grounding evaluation for the overlap decollision phase
        /// </summary>
        OverlapDecollision,
        /// <summary>
        /// Grounding evaluation for the initial overlaps detection phase
        /// </summary>
        InitialOverlaps,
        /// <summary>
        /// Grounding evaluation for movement hits phase
        /// </summary>
        MovementHit,
        /// <summary>
        /// Grounding evaluation for stepping up hits phase
        /// </summary>
        StepUpHit,
    }

    /// <summary>
    /// Comparer for sorting collider cast hits by hit fraction (distance)
    /// </summary>
    public struct HitFractionComparer : IComparer<ColliderCastHit>
    {
        /// <summary>
        /// Compares two hits for sorting them by distance
        /// </summary>
        /// <param name="x"> First hit </param>
        /// <param name="y"> Second hit </param>
        /// <returns> Positive integer if the first hit has a greater distance; negative integer otherwise </returns>
        public int Compare(ColliderCastHit x, ColliderCastHit y)
        {
            if (x.Fraction > y.Fraction)
            {
                return 1;
            }
            else if (x.Fraction < y.Fraction)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// A common hit struct for cast hits and distance hits
    /// </summary>
    [System.Serializable]
    public struct BasicHit
    {
        /// <summary>
        /// Hit entity
        /// </summary>
        public Entity Entity;
        /// <summary>
        /// Hit rigidbody index
        /// </summary>
        public int RigidBodyIndex;
        /// <summary>
        /// Hit collider key
        /// </summary>
        public ColliderKey ColliderKey;
        /// <summary>
        /// Hit point
        /// </summary>
        public float3 Position;
        /// <summary>
        /// Hit normal
        /// </summary>
        public float3 Normal;
        /// <summary>
        /// Hit material
        /// </summary>
        public Material Material;

        /// <summary>
        /// Constructs a basic hit from a raycast hit
        /// </summary>
        /// <param name="hit"> Raycast hit</param>
        public BasicHit(RaycastHit hit)
        {
            Entity = hit.Entity;
            RigidBodyIndex = hit.RigidBodyIndex;
            ColliderKey = hit.ColliderKey;
            Position = hit.Position;
            Normal = hit.SurfaceNormal;
            Material = hit.Material;
        }

        /// <summary>
        /// Constructs a basic hit from a collider cast hit
        /// </summary>
        /// <param name="hit"> Collider cast hit </param>
        public BasicHit(ColliderCastHit hit)
        {
            Entity = hit.Entity;
            RigidBodyIndex = hit.RigidBodyIndex;
            ColliderKey = hit.ColliderKey;
            Position = hit.Position;
            Normal = hit.SurfaceNormal;
            Material = hit.Material;
        }

        /// <summary>
        /// Constructs a basic hit from a distance hit
        /// </summary>
        /// <param name="hit"> Distance hit </param>
        public BasicHit(DistanceHit hit)
        {
            Entity = hit.Entity;
            RigidBodyIndex = hit.RigidBodyIndex;
            ColliderKey = hit.ColliderKey;
            Position = hit.Position;
            Normal = hit.SurfaceNormal;
            Material = hit.Material;
        }

        /// <summary>
        /// Constructs a basic hit from a character hit
        /// </summary>
        /// <param name="hit"> Character hit </param>
        public BasicHit(KinematicCharacterHit hit)
        {
            Entity = hit.Entity;
            RigidBodyIndex = hit.RigidBodyIndex;
            ColliderKey = hit.ColliderKey;
            Position = hit.Position;
            Normal = hit.Normal;
            Material = hit.Material;
        }

        /// <summary>
        /// Constructs a basic hit from a velocity projection hit
        /// </summary>
        /// <param name="hit"> Velocity projection hit </param>
        public BasicHit(KinematicVelocityProjectionHit hit)
        {
            Entity = hit.Entity;
            RigidBodyIndex = hit.RigidBodyIndex;
            ColliderKey = hit.ColliderKey;
            Position = hit.Position;
            Normal = hit.Normal;
            Material = hit.Material;
        }
    }

    /// <summary>
    /// Collection of utility functions for characters
    /// </summary>
    public static class KinematicCharacterUtilities
    {
        /// <summary>
        /// Returns an entity query builder that includes all basic components of a character
        /// </summary>
        /// <returns> The EntityQueryBuilder for basic character component s </returns>
        public static EntityQueryBuilder GetBaseCharacterQueryBuilder()
        {
            return new EntityQueryBuilder(Allocator.Temp)
                .WithAll<
                    LocalTransform,
                    PhysicsCollider,
                    PhysicsVelocity,
                    PhysicsMass,
                    PhysicsWorldIndex>()
                .WithAll<
                    KinematicCharacterProperties,
                    KinematicCharacterBody,
                    StoredKinematicCharacterData>()
                .WithAll<
                    KinematicCharacterHit,
                    StatefulKinematicCharacterHit,
                    KinematicCharacterDeferredImpulse,
                    KinematicVelocityProjectionHit>();
        }

        /// <summary>
        /// Returns an entity query builder that includes all basic components of a character as well as the interpolation component
        /// </summary>
        /// <returns> The EntityQueryBuilder for interpolated characters </returns>
        public static EntityQueryBuilder GetInterpolatedCharacterQueryBuilder()
        {
            return GetBaseCharacterQueryBuilder()
                .WithAll<CharacterInterpolation>();
        }

        /// <summary>
        /// Adds all the required character components to an entity
        /// </summary>
        /// <param name="dstManager"> The entity manager used for adding components and buffers </param>
        /// <param name="entity"> The entity to create the character on </param>
        /// <param name="authoringProperties"> The properties of the character </param>
        public static void CreateCharacter(
            EntityManager dstManager,
            Entity entity,
            AuthoringKinematicCharacterProperties authoringProperties)
        {
            // Base character components
            dstManager.AddComponentData(entity, new KinematicCharacterProperties(authoringProperties));
            dstManager.AddComponentData(entity, KinematicCharacterBody.GetDefault());
            dstManager.AddComponentData(entity, new StoredKinematicCharacterData());

            dstManager.AddBuffer<KinematicCharacterHit>(entity);
            dstManager.AddBuffer<KinematicCharacterDeferredImpulse>(entity);
            dstManager.AddBuffer<StatefulKinematicCharacterHit>(entity);
            dstManager.AddBuffer<KinematicVelocityProjectionHit>(entity);

            // Kinematic physics body components
            dstManager.AddComponentData(entity, new PhysicsVelocity());
            dstManager.AddComponentData(entity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));
            dstManager.AddComponentData(entity, new PhysicsGravityFactor { Value = 0f });
            dstManager.AddComponentData(entity, new PhysicsCustomTags { Value = authoringProperties.CustomPhysicsBodyTags.Value });

            // Interpolation
            if (authoringProperties.InterpolatePosition || authoringProperties.InterpolateRotation)
            {
                dstManager.AddComponentData(entity, new CharacterInterpolation
                {
                    InterpolateRotation = authoringProperties.InterpolateRotation ? (byte)1 : (byte)0,
                    InterpolatePosition = authoringProperties.InterpolatePosition ? (byte)1 : (byte)0,
                });
            }
        }

        /// <summary>
        /// Adds all the required character components to an entity
        /// </summary>
        /// <param name="commandBuffer"> The entity command buffer used to add components and buffers to the entity </param>
        /// <param name="entity"> The entity to create the character on </param>
        /// <param name="authoringProperties"> The properties of the character </param>
        public static void CreateCharacter(
            EntityCommandBuffer commandBuffer,
            Entity entity,
            AuthoringKinematicCharacterProperties authoringProperties)
        {
            // Base character components
            commandBuffer.AddComponent(entity, new KinematicCharacterProperties(authoringProperties));
            commandBuffer.AddComponent(entity, KinematicCharacterBody.GetDefault());
            commandBuffer.AddComponent(entity, new StoredKinematicCharacterData());

            commandBuffer.AddBuffer<KinematicCharacterHit>(entity);
            commandBuffer.AddBuffer<KinematicCharacterDeferredImpulse>(entity);
            commandBuffer.AddBuffer<StatefulKinematicCharacterHit>(entity);
            commandBuffer.AddBuffer<KinematicVelocityProjectionHit>(entity);

            // Kinematic physics body components
            commandBuffer.AddComponent(entity, new PhysicsVelocity());
            commandBuffer.AddComponent(entity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));
            commandBuffer.AddComponent(entity, new PhysicsGravityFactor { Value = 0f });
            commandBuffer.AddComponent(entity, new PhysicsCustomTags { Value = authoringProperties.CustomPhysicsBodyTags.Value });

            // Interpolation
            if (authoringProperties.InterpolatePosition || authoringProperties.InterpolateRotation)
            {
                commandBuffer.AddComponent(entity, new CharacterInterpolation
                {
                    InterpolateRotation = authoringProperties.InterpolateRotation ? (byte)1 : (byte)0,
                    InterpolatePosition = authoringProperties.InterpolatePosition ? (byte)1 : (byte)0,
                });
            }
        }

        /// <summary>
        /// Handles the conversion from GameObject to Entity for a character
        /// </summary>
        /// <param name="baker"> The baker that want to bake a character </param>
        /// <param name="authoring"> The monobehaviour used for authoring the character </param>
        /// <param name="authoringProperties"> The properties of the character </param>
        /// <typeparam name="T"> The type of the monobehaviour used for authoring the character </typeparam>
        public static void BakeCharacter<T>(
            Baker<T> baker,
            T authoring,
            AuthoringKinematicCharacterProperties authoringProperties) where T : MonoBehaviour
        {
            BakeCharacter(baker, authoring.gameObject, authoringProperties);
        }

        /// <summary>
        /// Handles the conversion from GameObject to Entity for a character
        /// </summary>
        /// <param name="baker"> The baker that want to bake a character </param>
        /// <param name="authoringGameObject"> The GameObject used for authoring the character </param>
        /// <param name="authoringProperties"> The properties of the character </param>
        public static void BakeCharacter(
            IBaker baker,
            GameObject authoringGameObject,
            AuthoringKinematicCharacterProperties authoringProperties)
        {
            if (authoringGameObject.transform.lossyScale != UnityEngine.Vector3.one)
            {
                UnityEngine.Debug.LogError("ERROR: kinematic character objects do not support having a scale other than (1,1,1). Conversion will be aborted");
                return;
            }
            if (authoringGameObject.gameObject.GetComponent<Rigidbody>() != null)
            {
                UnityEngine.Debug.LogError("ERROR: kinematic character objects cannot have a Rigidbody component. The correct physics components will be setup automatically during conversion. Conversion will be aborted");
                return;
            }

            Entity characterEntity = baker.GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace);

            // Base character components
            baker.AddComponent(characterEntity, new KinematicCharacterProperties(authoringProperties));
            baker.AddComponent(characterEntity, KinematicCharacterBody.GetDefault());
            baker.AddComponent(characterEntity, new StoredKinematicCharacterData());

            baker.AddBuffer<KinematicCharacterHit>(characterEntity);
            baker.AddBuffer<KinematicCharacterDeferredImpulse>(characterEntity);
            baker.AddBuffer<StatefulKinematicCharacterHit>(characterEntity);
            baker.AddBuffer<KinematicVelocityProjectionHit>(characterEntity);

            // Kinematic physics body components
            baker.AddComponent(characterEntity, new PhysicsVelocity());
            baker.AddComponent(characterEntity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));
            baker.AddComponent(characterEntity, new PhysicsGravityFactor { Value = 0f });
            baker.AddComponent(characterEntity, new PhysicsCustomTags { Value = authoringProperties.CustomPhysicsBodyTags.Value });

            // Interpolation
            if (authoringProperties.InterpolatePosition || authoringProperties.InterpolateRotation)
            {
                baker.AddComponent(characterEntity, new CharacterInterpolation
                {
                    InterpolateRotation = authoringProperties.InterpolateRotation ? (byte)1 : (byte)0,
                    InterpolatePosition = authoringProperties.InterpolatePosition ? (byte)1 : (byte)0,
                });
            }
        }

        /// <summary>
        /// Creates a character hit buffer element based on the provided parameters
        /// </summary>
        /// <param name="newHit"> The detected hit </param>
        /// <param name="characterIsGrounded"> Whether or not the character is currently grounded </param>
        /// <param name="characterRelativeVelocity"> The character's relative velocity </param>
        /// <param name="isGroundedOnHit"> Whether or not the character would be grounded on this hit </param>
        /// <returns> The resulting character hit </returns>
        public static KinematicCharacterHit CreateCharacterHit(
            in BasicHit newHit,
            bool characterIsGrounded,
            float3 characterRelativeVelocity,
            bool isGroundedOnHit)
        {
            KinematicCharacterHit newCharacterHit = new KinematicCharacterHit
            {
                Entity = newHit.Entity,
                RigidBodyIndex = newHit.RigidBodyIndex,
                ColliderKey = newHit.ColliderKey,
                Normal = newHit.Normal,
                Position = newHit.Position,
                WasCharacterGroundedOnHitEnter = characterIsGrounded,
                IsGroundedOnHit = isGroundedOnHit,
                CharacterVelocityBeforeHit = characterRelativeVelocity,
                CharacterVelocityAfterHit = characterRelativeVelocity,
            };

            return newCharacterHit;
        }

        /// <summary>
        /// Incrementally rotates a rotation at a variable rate, based on a rotation delta that should happen over a fixed time delta
        /// </summary>
        /// <param name="modifiedRotation"> The source rotation being modified </param>
        /// <param name="fixedRateRotation"> The rotation that needs to happen over a fixed time delta </param>
        /// <param name="deltaTime"> The variable time delta </param>
        /// <param name="fixedDeltaTime"> The reference fixed time delta </param>
        public static void AddVariableRateRotationFromFixedRateRotation(ref quaternion modifiedRotation, quaternion fixedRateRotation, float deltaTime, float fixedDeltaTime)
        {
            if (fixedDeltaTime > 0f)
            {
                float rotationRatio = deltaTime / fixedDeltaTime;
                quaternion rotationFromCharacterParent = math.slerp(quaternion.identity, fixedRateRotation, rotationRatio);
                modifiedRotation = math.mul(modifiedRotation, rotationFromCharacterParent);
            }
        }

        /// <summary>
        /// Sets various properties involved in making the character detect different forms of collisions.
        /// Warning: it is up to you to ensure that the collider passed as parameter to this function is unique, otherwise this will change the collision response for
        /// all characters that share this collider.
        /// </summary>
        /// <param name="active">Whether or not collisions should be active</param>
        /// <param name="characterProperties">The character properties component</param>
        /// <param name="collider">The character's collider component</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCollisionDetectionActive(bool active, ref KinematicCharacterProperties characterProperties, ref PhysicsCollider collider)
        {
            characterProperties.EvaluateGrounding = active;
            characterProperties.DetectMovementCollisions = active;
            characterProperties.DecollideFromOverlaps = active;
            collider.Value.Value.SetCollisionResponse(active ? CollisionResponsePolicy.Collide : CollisionResponsePolicy.None);
        }
    }
}
