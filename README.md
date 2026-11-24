# First-Person Gameplay Programmer Assignment Prototype

A complete, playable, and streamlined first-person gameplay prototype built directly in Unity (Unity 6 / 6000.3.16f1) adhering strictly to assignment constraints:
- **Zero third-party packages** (no Cinemachine, no external controllers, no asset store dependencies).
- **Native Unity functionality** (Standard CharacterController, custom camera transitions, procedural animation, coroutines, and UI).
- **Streamlined 7-Component Architecture**: Practical, maintainable, readable code with zero bloat and zero overengineering.

---

## Quick Start & Controls

1. **Open the Scene:**
   Open `Assets/_Project/Scenes/MainPrototype.unity` in the Unity Editor.
   *(Or click `MadMantra > Build Prototype Scene` from the top Unity menu bar at any time to regenerate from scratch)*.
2. **Press Play (▶)** in the Unity Editor.
3. **Player Controls:**
   - **WASD:** First-person movement.
   - **Mouse:** Look around (pitch clamped to -85° to +85°).
   - **Left Shift:** Sprint.
   - **E:** Primary interaction (access control panel, pick up keycard, unlock door, speak to survivor, advance dialogue).
   - **1, 2, 3 or Left Mouse Click:** Depress buttons in the Control Panel puzzle.
   - **Escape:** Back out of puzzle view / Return camera to player.

---

## 16-Step Gameplay Sequence

1. **Unconscious Wake-Up:** The game begins with the player lying on the floor in Room 1 (low camera position, pitch/roll tilt, black screen fade).
2. **Opening Cinematic:** Industrial lights flicker erratically; in the background, a distant bulkhead seals shut with a heavy thud.
3. **Camera Recovery & Control:** The camera smoothly rises and levels out to eye height (1.65m); the player receives movement and mouse look control.
4. **Control Panel Discovery:** Walking left reveals the auxiliary power console with 3 physical buttons arranged in a triangle and 3 indicator lights.
5. **Triangular Puzzle:** Approaching and pressing `[E]` smoothly focuses the camera on the console and enables the cursor. Pressing buttons (via `1/2/3` or mouse click) physically depresses the cylinder and illuminates its status light. Target sequence: `1 -> 2 -> 3`.
6. **Cabinet Pop-Open:** Completing the sequence triggers cabinet release and smoothly swings open the adjacent wall cabinet door.
7. **Keycard Collection:** Player approaches the floating security keycard inside the cabinet and presses `[E]`. Keycard is collected, displaying a notification and updating the objective.
8. **Door Unlock:** Player walks to the locked heavy bulkhead between Room 1 and Room 2. Pressing `[E]` checks keycard ownership, triggers unlock notification, and swings the security door open 90°.
9. **Entering Room 2:** Player steps through into Room 2, illuminated with distinct cool ambient lighting.
10. **NPC Discovery:** An NPC survivor is standing in the chamber.
11. **Smooth Head Tracking:** As the player walks around Room 2, the NPC's head smoothly rotates (clamped within realistic angular limits: ±70° horizontal, ±35° vertical) to maintain eye contact without rotating his body.
12. **Dialogue Initiation:** Pressing `[E]` near the NPC triggers dialogue mode, locking player movement and smoothly transitioning the camera to frame the NPC.
13. **Subtitles & Cinematic Look:** The NPC speaks across narrative lines with a real-time typewriter effect, procedural talking nod, and subtle mouse-look leeway.
14. **Disappointment Reaction:** The NPC drops his shoulders into a slump, hangs his head, and slowly shakes his head side-to-side.
15. **NPC Walk to Exit:** The NPC navigates along waypoints toward the Room 2 exit door, opens the door, steps through, and closes it behind him.
16. **Player Control Restored:** The camera returns to full first-person control with final objective completed banner.

---

## Clean 7-Component Architecture

Instead of over-fragmented micro-classes, the project is organized into 7 cohesive, single-responsibility components:

| Component | Responsibility |
|---|---|
| **`PlayerController.cs`** | CharacterController movement, sprint, gravity, smooth mouse look, procedural hand bob/reach (without colliders). |
| **`PlayerInteractor.cs`** | Screen-center raycasting for `IInteractable`, prompt UI updates, and `[E]` activation. |
| **`ControlPanel.cs`** | Physical console coordinator, sequence validation, close-up camera focus, and wall cabinet hinge trigger. |
| **`PuzzleButton.cs`** | Standalone button component paired with its indicator light, physical press depression, and visual emission states. |
| **`Door.cs`** | Reusable hinged door component with locked/unlocked state, keycard check, and smooth `Quaternion.Slerp` rotation. |
| **`NPCController.cs`** | Smooth head tracking (clamped yaw/pitch), talking animation, disappointment slump/shake, and exit navigation. |
| **`SequenceDirector.cs`** | Central narrative orchestrator: wake-up cinematic, smooth camera transitions, and dialogue progression. |
| **`UIManager.cs`** | Reticle, interaction prompt pill, objective banner, typewriter subtitles, and screen fade CanvasGroup. |

### Auxiliary Components:
- **`IInteractable.cs`**: Standard decoupled interaction interface.
- **`KeycardPickup.cs`**: Simple collectible item pickup component.

---

## Editor Tools

Access from the Unity Editor toolbar under the **`MadMantra`** menu:
- **`MadMantra > Build Prototype Scene`**: Rebuilds the entire prototype scene from scratch with clean primitives, materials, and component wiring.
- **`MadMantra > Validate Prototype Flow`**: Verifies that all 7 core components and scene objects exist and are properly linked.
- **`MadMantra > Run Automated Playmode Test`**: Executes an automated playmode test verifying all 16 gameplay beats end-to-end.
