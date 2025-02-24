
# Collision shape and dimensions

You can change the shape and dimensions of the character physics shape at authoring time, or at runtime.

## Change shape type at authoring time

To changing the character physics shape at authoring time, change the "Shape Type" in the `PhysicsShape` component of the character GameObject. The only limitation is that it must be a convex shape (all primitives are convex).

## Changing the shape dimensions at runtime

If you want to change the character collider's dimensions at runtime, you can use the following:

```cs
CapsuleCollider* capsuleCollider = (CapsuleCollider*)physicsCollider.ColliderPtr;
CapsuleGeometry capsuleGeometry = capsuleCollider->Geometry;
capsuleGeometry.Radius = 1f;
capsuleCollider->Geometry = capsuleGeometry;
```

You need to make sure that the method in which you perform this has the `unsafe` declaration, and to write back to the `PhysicsCollider` component in the character update job.