
# Limitations

This page outlines some known limitations of the Character Controller.

* The root character entity must never be a child of another entity. However, there is a mechanism to imitate parenting. For more information, see the documentation on [parenting](concepts-parenting.md).
* While this character controller offers support for [dynamic rigidbody interactions](dynamic-body-interaction.md), which tries to imitate the behavior of a fully dynamic character, it has the following limitations:
    * You can't constrain the character rigidbody's movement with physics joints. The character is a kinematic rigidbody, so it has the same limitations with joints that any regular kinematic rigidbody has.
    * Characters can simulate believable dynamic physics interactions with a single rigidbody, but dynamic physics interactions between more than two rigidbodies simultaneously aren't physically accurate.
    * A character with `SimulateDynamicBody` set to false (kinematic character) can't push a character with `SimulateDynamicBody` set to true (dynamic character). In order for one character to be able to push another, both of them must have `SimulateDynamicBody` set to true
