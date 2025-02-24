# First-person standard character setup

To set up a first-person standard character, perform the following steps:

1. Open the Package Manager Window (**Window > Package Manager**) and select the Character Controller package.
1. Open the **Samples** tab, and then select **Import** to import the Standard Characters assets in your project. Unity adds the standard character files to your project, under the `Samples/Character Controller/[version]/Standard Characters` folder.
1. [Create a subscene](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/manual/conversion-subscenes.html), if you haven't already.
1. Navigate to the `Samples/Character Controller/[version]/Standard Characters/FirstPerson/Prefabs` folder. Drag the **FirstPersonCharacter** and **FirstPersonPlayer** prefabs into the subscene.
1. Select the **FirstPersonPlayer** GameObject.
1. In the Inspector, navigate to the **First Person Player Authoring** component and under **Controlled Character**, set the **FirstPersonCharacter** GameObject:
    ![Screenshot](./images/first-person-authoring-script.jpg)
1. Open the **FirstPersonCharacter** GameObject's hierarchy and select the **View** GameObject.
1. Navigate to the `Samples/Character Controller/[version]/Standard Characters/Common/Scripts/Camera` folder. Drag the `MainEntityCameraAuthoring` script onto the **View** GameObject:
    ![[Screenshot]](./images/first-person-view-script.jpg)<br/>
    This component marks the **View** entity as the entity that your GameObject camera must follow. The **View** GameObject represents the camera point of the first person character. When you control the look input of the character, Unity rotates the **View** entity up and down.
1. Make sure your scene has a camera GameObject that isn't in a subscene. Drag the `MainGameObjectCamera` script onto the camera. This component marks the camera as the GameObject that must copy the entity marked with `MainEntityCameraAuthoring` every frame:
    ![Screenshot](./images/first-person-camera-script.jpg)
