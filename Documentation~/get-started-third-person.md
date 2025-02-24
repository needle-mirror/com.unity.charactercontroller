
# Third-person standard character setup

To set up a third-person standard character, perform the following steps:

1. Open the Package Manager Window (**Window > Package Manager**) and select the Character Controller package.
1. Open the **Samples** tab, and then select **Import** to import the Standard Characters assets in your project. Unity adds the standard character files to your project, under the `Samples/Character Controller/[version]/Standard Characters` folder.
1. [Create a subscene](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/manual/conversion-subscenes.html), if you haven't already.
1. Navigate to the `Samples/Character Controller/[version]/Standard Characters/ThirdPerson/Prefabs` folder. Drag the **ThirdPersonCharacter**, **ThirdPersonPlayer**, and **OrbitCamera** prefabs into the subscene.
1. Select the **ThirdPersonPlayer** GameObject.
1. In the Inspector, navigate to the **Third Person Player Authoring** component and under **Controlled Character**, set the **ThirdPersonCharacter** GameObject. Under **Controlled Camera**, set the **OrbitCamera** GameObject:
    ![Screenshot](./images/third-person-authoring-script.jpg)
1. Navigate to the `Samples/Character Controller/[version]/Standard Characters/Common/Scripts/Camera` folder. Drag the `MainEntityCameraAuthoring` script onto the **OrbitCamera** GameObject:
    ![[Screenshot]](./images/third-person-view-script.jpg)
1. Make sure your scene has a camera GameObject that isn't in a subscene. Drag the `MainGameObjectCamera` script onto the camera. This component marks the camera as the GameObject that must copy the entity marked with `MainEntityCameraAuthoring` every frame:
    ![Screenshot](./images/first-person-camera-script.jpg)
