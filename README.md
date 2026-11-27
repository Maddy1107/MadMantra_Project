# MadMantra — First-Person Gameplay Prototype

A playable Unity 6 (`6000.3.16f1`, Built-in Render Pipeline) first-person narrative puzzle prototype built from scratch with **zero external packages** and **no Cinemachine**.

---

## Controls
| Input | Action |
| :--- | :--- |
| **WASD** | Move |
| **Mouse** | Look / Subtle clamped camera look during NPC dialogue |
| **E** | Interact (Control Panel, Key, Doors, NPC) & advance dialogue |
| **Left Click** | Press buttons on the Control Panel |
| **Esc** | Exit Control Panel puzzle or NPC dialogue |

---

## Gameplay Flow
1. **Wake-Up Cinematic (`SequenceDirector`, `EyelidOverlay`, `LightFlicker`)**
   - Player wakes up on the floor with curved eyelid blink overlay and flickering overhead lights.
   - As the player lifts their head, the Survivor opens `SecurityDoor_Room1`, walks into Room 2, and closes the door behind him.
   - Player stands up and regains full control.
2. **Control Panel Puzzle & Key Collection (`ControlPanel`, `PuzzleButton`, `PlayerHand`, `KeycardPickup`)**
   - Interacting with the control panel smoothly focuses the camera on the 3 triangular buttons.
   - Clicking a button reaches the cybernetic FPS hand in a shoulder-pivoted arc onto the button, pressing it and lighting its indicator.
   - Solving the sequence (`0 → 1 → 2`) turns all indicators green, smoothly pans the camera to the wall-mounted Key Cabinet as it opens, and reveals the Security Key.
3. **Door Unlock & Interaction (`Door`)**
   - Interacting with the locked `SecurityDoor_Room1` while holding the key focuses the camera onto the lock, slides the key in, turns it `90°`, pulls it out, and swings the door open.
   - Unlocked doors can be opened and closed freely with the player's hand animation.
4. **Room 2 NPC, Dialogue & Exit (`NPCController`, `NPCHeadLook`, `DialogueData`, `DialogueRunner`)**
   - The Survivor's head tracks the player using Humanoid Look-At IK (`OnAnimatorIK`) within clamped angle and range limits.
   - Interacting initiates a cinematic focus camera with **subtle clamped mouse look** (`±15°` yaw, `±10°` pitch) that smoothly returns to the NPC when mouse input stops.
   - Data-driven `ScriptableObject` dialogue (`Survivor_Dialogue.asset`) plays with typewriter subtitles and Mixamo `Talking` animation.
   - On completion, the NPC plays his `Disappointed` reaction, restores player control, walks to `ExitDoor_Room2`, opens it, exits, and closes it behind him.

---

## Architecture Overview (`Assets/_Project/Scripts/`)
| Folder | Scripts | Responsibility |
| :--- | :--- | :--- |
| `Player/` | `PlayerController`, `PlayerInteractor`, `PlayerHand` | Custom FPS movement/look, raycast interaction (`IInteractable`), smooth camera focus blending + clamped dialogue look, procedural arm reach |
| `Interaction/` | `IInteractable` | Shared interface (`GetPrompt()`, `Interact(PlayerInteractor)`) decoupling player from all world objects |
| `Cinematics/` | `SequenceDirector`, `EyelidOverlay` | Coroutine-driven wake-up sequence and eyelid curtain animation |
| `Puzzle/` | `ControlPanel`, `PuzzleButton` | Queue-based sequence validation and button press/indicator states |
| `Environment/` | `Door`, `LightFlicker` | Reusable hinge door (`Unlocked` / `Keycard` / `External`) with key-unlock animation; flickering light controller |
| `Inventory/` | `KeycardPickup` | Collectible key pickup with bob/spin feedback |
| `NPC/` | `NPCController`, `NPCHeadLook` | Mixamo Animator state control, scripted walk/turn exit routine, and Humanoid IK head tracking |
| `Dialogue/` | `DialogueData`, `DialogueRunner` | Generic `ScriptableObject` dialogue container and coroutine subtitle runner |
| `UI/` | `UIManager` | Centralized HUD (crosshair, prompts, objectives, feedback, typewriter subtitles) |
| `Audio/` | `AudioManager` | Dual `AudioSource` manager (looping room ambience + pitched one-shot SFX) |

---

## Opening & Running
1. Open the project in **Unity 6 (`6000.3.16f1`)**.
2. Open **`Assets/_Project/Scenes/MainPrototype.unity`**.
3. Press **Play**.
