=========Models=========
Simply drag the prefabs into the scene.
They're all sharing one texture, and one material.

The cars are available in two versions. As one mesh, and with wheels separate. The separate ones have wheel colliders setup. 

=========Scripts=========

-Street Light-

Streetlight script is simple. You can toggle it either in editor, or ingame by calling the "Toggle" function in the script.

There's a ready street light prefab, but if you wanna make your own, follow the instructions.

Instructions:
1. Make an empty gameobject
2. Parent the "on" and "off" version of the street light model.
3. Add lights to them if you need them. Both enabled.
4. Drag the script onto it.
5. Drag the child objects into the array.
6. Toggle on/off, and play!

-Traffic Light-

Traffic Light script is a bit more complex. You can reset it ingame by calling a "Reset" function in the script. You can change the light timers timer in the inspector, and choose whether it'll start with a green or red light. You'll want this option on crossroads.

There's a ready traffic light prefab, but if you wanna make your own, follow the instructions.

1. Make an empty gameobject.
2. Parent the red, yellow, and green version of the traffic light model.
3. Add lights to them if you need them. All enabled.
4. Drag the script onto it.
5. Drag the child objects into the array in red, yellow, green order.
7. Set the timers. Usually red is longest, green medium, and yellow shortest.
8. Choose whether to start with green or red light.Default is red. 

=========Contact=========
If you have any questions, issues, requests, you're welcome to email me at damaged.grounds@gmail.com, or post in the official thread.