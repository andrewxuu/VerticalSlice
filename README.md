# GDIM33 Vertical Slice
## Milestone 1 Devlog
1. One of the main Visual Scripting graphs in the vertical slice manages the inventory open/close state using a Scene Variable called isOpen. When the player presses Tab,
   a button event in the graph reads the current boolean value of isOpen. If it is true, the graph flips it and writes the updated value back to the scene. It also toggles
   the inventory UI visibility by adjusting the alpha level of the Canvas Group. The isOpen variable is referenced by other systems as well: when the inventory is open, the
   camera smoothly lerps to a dedicated inventory view position, similar to how Animal Crossing frames its inventory screen.
   
2. The state machine used in the vertical slice is the player animation state machine. The animator has three paramaters: the float speed, float directionZ and the boolean
   isChopping. The machine has three main states, walk/run, chop, and idle. The player animator script calculates the horizontal movement, comparing it to the character
   controller position and feeds that information to the speed and directionZ variable. If the player is moving backwards, the directionZ variable goes negative, which
   triggers the animator to play the reverse walking animation. The isChopping boolean is controlled by a player interaction script. The boolean is set to true when the
   player holds the "E" key near a tagged tree or rock environment object, it resets to false when the key is released. This chopping state can be triggered at any time as
   long as the player meets the conditions. The walk animation is triggered when the player's speed exceeds 0. The running animation can be triggered when the player speed
   exceeds 5, this only happens when the player is holding down the shift key. The default state when the player stops moving is set to idle.

   The state machine connects directly to two other systems in the game. It ties into the interaction and inventory systems through the Chop state, which plays for as long as the
   progress bar is filling. When the player holds "E" near a resource in the environment, the interaction system locks the Animator into the Chop state and keeps it there while the
   action progresses. The progress bar and the animation stay in sync throughout, reflecting the same resource chopping timer. Once the action completes, the inventory system receives 
   the resource, either wood or stone depending on what was targeted and then the Chop state exits, returning the Animator to Idle. If the player cancels early by releasing the key or 
   stepping away, the state machine resets immediately.

   <img width="1078" height="796" alt="image" src="https://github.com/user-attachments/assets/3fd97ef9-14ae-433f-bfbd-bd5b0a27cfc8" />
   
## Milestone 2 Devlog
Milestone 2 Devlog goes here.
## Milestone 3 Devlog
Milestone 3 Devlog goes here.
## Milestone 4 Devlog
Milestone 4 Devlog goes here.
## Final Devlog
Final Devlog goes here.
## Open-source assets
- [Animations](https://kaylousberg.itch.io/kaykit-character-animations) 
- [Environment](https://kaylousberg.itch.io/kaykit-forest) 
- [Objects](https://kaylousberg.itch.io/resource-bits) 
- [Player](https://kaylousberg.itch.io/kaykit-character-animations) 
- [Camping Objects](https://forsunka.itch.io/low-poly-camping-asset)


