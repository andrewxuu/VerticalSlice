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
1.  Feature: Campfire Placement & Interaction System

The campfire is the central survival mechanic in my vertical slice. The player must gather wood and stone, craft a campfire item through the inventory, select it, and physically place it in the world. Once placed, they can walk up to it, open a dedicated UI, add fuel to keep it burning, and upgrade the shelter level to improve warmth radius over the three-night survival period.

Step 1 — Crafting the campfire as an inventory item
- Design the campfire as a craftable item that lives in the player's inventory like any other resource
- Set up the recipe system so the campfire can be unlocked through gathering and crafting
- Make the campfire distinguishable from regular items so the game knows it can be placed in the world

Step 2 — Placing the campfire in the world

- Give the player a way to enter placement mode when a placeable item is selected
- Provide visual feedback showing where the campfire will be placed and whether the location is valid
- Confirm the placement and remove the item from inventory once committed

Step 3 — Campfire interaction UI

- Build a status panel that shows the campfire's current state and lets the player interact with it
- Connect player actions (adding fuel, upgrading shelter) to meaningful changes in the game world
- Tie the campfire UI into the existing inventory system so both panels open together seamlessly
 
2.  The task breakdown was genuinely useful for the campfire feature because it forced me to separate what felt like one big feature into three distinct systems: crafting, placement, and the interaction. Without that separation I probably would have tried to wire everything up at once and gotten confused about where state lived. If I were to improve the breakdowns, I would add notes about dependencies between steps, for example, Step 2 can't be tested until Step 1 produces a valid item in inventory, and Step 3's UI callbacks can't be wired until the prefab exists from Step 2. Calling those dependencies out explicitly would help avoid dead ends mid-build.

3. My vertical slice bridges Visual Scripting and C# scripts with shared scene variable called isOpen.  The VS graph runs on On Update Event and detects when Tab is pressed via Get Key Down. It reads isOpen via Get Variable (Scene), negates it, and writes the flipped value back with Set Variable, toggling the inventory open and closed. A second If branch then checks the new state to set the InvCanvasGroup alpha (showing or hiding the inventory UI) and sets an isOpen bool on the PlayerAnimator via Animator Set Bool to freeze or resume animations.

On the C# side, UIManager.cs, CampfireInteraction.cs, and CampfirePlacer.cs all write to the same isOpen Scene Variable when panels open or close. This means the graph responds regardless of whether the UI state was triggered by Tab, C, or Q — it just reads the one shared variable. PlayerController.cs also disables the ScriptMachine component directly as a failsafe to fully stop the graph when any UI is open.

![VS Diagram](image-1.png)


4. Please grade the Campfire Placement and Interaction System. The player crafts a campfire from the inventory Recipes panel (Tab), selects it with a number key, presses Q to enter a ghost placement mode, and left-clicks to place it. Walking up to the placed campfire and pressing C opens the campfire status panel (fuel bar, shelter level, add fuel and upgrade shelter buttons) alongside the shared backpack/crafting panel. 


## Milestone 3 Devlog
1. The snow shader is a custom URP ShaderGraph applied to every hex tile on the map. It can be seen in-game once night begins and snow starts accumulating on the terrain. The graph handles two things: physically raising the top surface of each tile with vertex displacement, and blending the tile color to white as snow deepens. In the vertex stage, a Normal Vector node dotted against a world up vector (0, 1, 0) produces a mask that is 1 on flat top faces and 0 on vertical side faces. This mask is sharpened with a subtract and multiply before being clamped, so only near-horizontal surfaces receive snow. The mask is then scaled by _SnowDepth, a global float that SnowManager.cs updates every frame, to produce the final displacement amount. In the fragment stage, the same mask drives a Lerp between the base texture color and a white snow color, and a second Lerp raises the smoothness value so snowy surfaces appear shinier. Both feed into a URP Lit master node so the snow responds to scene lighting and shadows like any other surface.

3.  Based on playtesting feedback, the biggest pain point was that players had no sense of urgency or consequence during the day -- the warmth meter only started mattering at night, so the daytime loop felt aimless. To address this, I tuned the WarmthSystem drain and regen rates so the transition from day to night is more immediately felt, and I added a visual cue by tinting the warmth bar icy-blue at night (using the warmth-cold USS class) versus amber during the day, giving players a clear at-a-glance read on their current risk state.
   
4. The major addition since Milestone 2 is the snow accumulation system, which ties directly into the core survival loop. SnowManager.cs tracks a CurrentDepth value that rises during night and partially melts during the day, and broadcasts it each frame to all terrain shaders via a global shader property. As snow deepens, it also reduces the player's movement speed through MovementMultiplier, making nighttime resource runs progressively harder across the three-night arc. SnowParticleController.cs drives a particle emitter that fades snowfall in at dusk and out at dawn, matching the same time thresholds used by DayNightCycle.cs. Together these systems mean snow is not cosmetic -- it slows the player, signals danger, and creates a visible record of how deep into the survival run the player is.
   
## Final Devlog
1. The core game play loop involves the player exploring a procedurally generated map to gather wood and stone by chopping trees and mining rocks. The main goal is build a campfire to generate warmth to survive the night. The player must utilize this material gathering system to accomplish that by upgrading their shelter, fueling the fire, and building tools to speed up this process. As the day/night cycle progresses, snow accumulates on the terrain overnight and partially melts during the day, which also slows the player's movement the longer they survive. When warmth hits zero, the player takes cold damage. Dying triggers a death screen with a restart option.

The vertical slice pitch described a cozy camping RPG built around two survival meters, a campfire that players build and upgrade, and resource gathering. Hunger was scraped during development due to scope management, but every other core pillar made it in: the campfire placement and upgrade system, the warmth drain at night, the resource loop, and the escalating pressure (snow accumulation and increasing cold). A player who tries the demo will immediately understands the loop the full game would be built around as it follows a familar staring point like minecraft: the player will spend their days preparing, or struggle at night. The  terrain with scattered trees and rocks also previews what a more developed map would look like. 

2. The snow effect is controlled by SnowManager.cs, which checks the time of day from DayNightCycle.cs each frame and labels it as Day, Night, or Transition. At night, a CurrentDepth float grows over time; during the day it shrinks back toward a minimum floor. Inside LitSnow.shader, the vertex stage uses a dot product between the surface normal and the world up vector to build a coverage mask, so only flat-topped surfaces receive snow while side faces stay bare. Each covered vertex is pushed upward by _SnowDepth * _MaxSnowThickness * mask. In the fragment stage, the albedo fades from the base terrain color to white, and smoothness is blended to match, so the snow is lit by the scene's lighting rather than just painted on top.

<img width="1010" height="1618" alt="image" src="https://github.com/user-attachments/assets/f8a67261-7516-47e0-b565-1f2dc6a0fc74" />
<img width="1029" height="302" alt="image" src="https://github.com/user-attachments/assets/186b8300-d7f1-48fe-8ca4-60cd1b9f2510" />

4. My approach is to think about each system in terms of inputs and outputs before writing any code. An example is the warmth system, the system only needs to know whether it's night and whether a fire is nearby, so I could build and test it independently before connecting it to anything else. I plan to use both tree graphs and task step breakdowns going forward, since they serve different stages: tree graphs are useful early for mapping dependencies and relationships. Task step breakdowns on the other hand are more useful once a system is defined. The breakdown process allows a better understanding of how scope works. In the vertical slice pitch, crafting and shelter upgrades looked like one item, but  in reality it needs a bunch of components like an inventory system, a recipe data scriptable objects, a campfire interaction script, and UI wiring, each with their own edge cases. The systems that went smoothest were the ones I planned carefully before opening Unity. The ones that went poorly were the ones I thought of as a single task and discovered mid-build were actually several steps I need to implement before I can implement the one I am working on. If I were starting over, I would write out task steps for every feature upfront, not just the ones that seemed complex at first glance.



## Open-source assets
- [Animations](https://kaylousberg.itch.io/kaykit-character-animations) 
- [Environment](https://kaylousberg.itch.io/kaykit-forest) 
- [Objects](https://kaylousberg.itch.io/resource-bits) 
- [Player](https://kaylousberg.itch.io/kaykit-character-animations) 
- [Camping Objects](https://forsunka.itch.io/low-poly-camping-asset)



