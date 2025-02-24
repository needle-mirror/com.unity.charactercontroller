
# Input handling

There are challenges to be aware of when it comes to processing player input that is meant to be used during a fixed timestep update (as is the case with the character physics update).


## Understanding fixed timestep updates

ECS has a `FixedStepSimulationSystemGroup` that acts as the default group where systems update at a fixed rate. On every regular update, Unity keeps track of how long it's been since the last time it ran a fixed update. Then, it runs the amount of fixed updates that corresponds to how many should've happened in that time. In practice this is capped to a maximum amount of fixed updates per frame.

Fixed updates happen at a fixed timestep instead of a variable one, and this makes them ideal for logic that needs consistency and predictability, like physics or server game simulation.

For example, if your application has a fixed timestep of `0.02` seconds (or, 50 fps), it needs 50 fixed updates to happen every second no matter what the frame rate is. If your application has a high framerate and most frames take less than `0.02` seconds, then there will be a lot of frames where no fixed updates happen, because the amount of time since the last fixed update is less than `0.02` seconds.

However, if your application has a low framerate where some frames take `0.05` seconds, then multiple fixed updates will be triggered during these frames to catch up with the amount of fixed updates that should've happened in that `0.05` seconds time and the leftover time since the last fixed update on the previous frame.

Therefore, on any regular frame, you have to assume that fixed updates might happen zero times, one time, or many times.


## Fixed timestep input issues and solutions

When you have systems that update at a fixed timestep that can consume punctual input events (like button presses or releases), the following issues might happen:
* If the frame rate is higher than the fixed rate, you might get multiple regular updates between two fixed updates. If your button press happened during any of these regular frames in-between fixed updates, that input event will never be detected by the next fixed update because it was only detected as "pressed" for one regular frame where no fixed update was triggered.
* If the frame rate is lower than the fixed rate, you might get multiple fixed updates between two regular updates. Since the state of your button press is only updated during regular updates, having the button press detected on one frame might lead to multiple fixed updates in a row detecting the button as "pressed".

Using a "Jump" button press as an example,
* A frame rate that is higher than the fixed update rate could lead to jump events not being detected by the character physics update.
* A frame rate that is lower than the fixed update rate could lead to jump events being detected twice in a row by the character physics update.

To get around this problem, when processing input events at a fixed update, you can do the following:

* Remember if the input event happened at any point since the last fixed update and not just if it happened on this frame. This solves the problem of input events being lost between fixed updates at high frame rates.
* Either reset input events at the end of each fixed update, or only process input events on the first fixed update that happened since the last regular update. This solves the problem of input events being processed multiple times at low frame rates.

Player input handling in the standard characters is structured in a way that solves all of these problems:

* The `FixedInputEvent` struct handles all button press and release events.
* When an input event happens in regular update, Unity calls `FixedInputEvent.Set(tick)`. When querying whether or not it should process an input event in a fixed update, it calls `FixedInputEvent.IsSet(tick)`
* The `tick` parameter is a counter that counts how many fixed updates happened so far. `FixedTickSystem` updates this parameter at the end of the `FixedStepSimulationSystemGroup`
* `FixedInputEvent` uses this tick to determine if an input event happened at any point since the last fixed update, and if the input event truly happened on this fixed update or a previous one.
