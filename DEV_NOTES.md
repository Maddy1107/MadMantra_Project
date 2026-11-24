# Senior Gameplay Programmer Architecture Notes

## Architectural Philosophy & Rationale

When evaluating a gameplay programming prototype, technical directors look for **clarity, reliability, solid math, and sensible separation of concerns**. 

An anti-pattern in prototype assignments is **over-fragmentation** (e.g., splitting a 3-button puzzle into 5 separate micro-scripts, state machines, and event brokers for a 3-minute sequence). This adds cognitive overhead, slows debugging, and introduces fragile wiring.

This prototype demonstrates the **senior industry sweet spot**:
1. **7 Single-Responsibility Components**: Every script has a clear, practical domain.
2. **Zero Third-Party Dependencies**: No Cinemachine, no controller packages, pure native Unity 6.
3. **Rock-Solid Math & Coordinate Transforms**:
   - Camera transitions preserve relative local transforms at eye height ($1.65\text{m}$), preventing camera jump bugs.
   - NPC head tracking uses `Quaternion.Inverse(transform.rotation) * LookRotation` with clamped local Euler angles ($\pm 70^\circ$ yaw, $\pm 35^\circ$ pitch) to prevent neck-snapping.
   - Smooth step hermite curves (`Mathf.SmoothStep`) for cinematic ease-in/ease-out.
4. **Collision-Safe Procedural Feedback**:
   - Hand bob and reach animations are generated on empty primitive meshes with colliders destroyed at runtime to ensure raycasts are never blocked.
5. **Pure Visual & Text Feedback**:
   - Clear UI notifications, subtitle typewriter dialog, and responsive interaction prompts provide immediate visual clarity with zero unnecessary audio overhead.

---

## 7 Component Breakdown

```
Assets/_Project/Scripts/
├── Player/
│   ├── PlayerController.cs     # CharacterController, gravity, look, procedural hands
│   └── PlayerInteractor.cs     # Screen-center IInteractable raycaster & E interaction
├── Puzzle/
│   ├── ControlPanel.cs         # Triangular console coordinator, focus view, cabinet trigger
│   └── PuzzleButton.cs         # Individual button unit paired with indicator light & depression
├── Environment/
│   └── Door.cs                 # Reusable hinge door, keycard validation, smooth Slerp
├── NPC/
│   └── NPCController.cs        # Head tracking, dialogue gestures, disappointment, exit walk
├── Cinematics/
│   └── SequenceDirector.cs     # Wake-up, dialogue sequence, robust camera transitions
├── UI/
│   └── UIManager.cs            # Reticle, prompt pill, objective banner, typewriter subtitles
├── Interaction/
│   └── IInteractable.cs        # Simple decoupled interaction contract
└── Inventory/
    └── KeycardPickup.cs        # Collectible keycard item inside cabinet
```

---

## 16 Assignment Beats Verification Matrix

| Beat # | Requirement | Implementation |
|---|---|---|
| **1** | Wake up on ground | `SequenceDirector.WakeUpSequenceRoutine` (Camera starts at $Y=0.25\text{m}$, tilted $25^\circ$). |
| **2** | Opening cinematic | Room lights flicker 3 bursts; background security door seals shut with sound. |
| **3** | Player gains control | Camera smoothly ascends to eye height ($1.65\text{m}$); `CanMove` and `CanLook` enabled. |
| **4** | Discover control panel | Auxiliary console on left wall with 3 triangular buttons and indicator lights. |
| **5** | Solve button puzzle | Close-up view via `[E]`, physical depression of buttons `1 -> 2 -> 3`, green light activation. |
| **6** | Cabinet opens | `ControlPanel.OnPuzzleSolved` smoothly swings adjacent wall cabinet door $105^\circ$. |
| **7** | Collect key | Player presses `[E]` on keycard inside cabinet; keycard enters inventory. |
| **8** | Unlock security door | Player presses `[E]` on security door; keycard verified, door swings open $90^\circ$. |
| **9** | Enter Room 2 | Player walks through into Room 2 with distinctive cool lighting. |
| **10** | Discover NPC | Survivor NPC positioned in Room 2 chamber. |
| **11** | NPC head tracking | `NPCController.HandleHeadTracking` smoothly tracks player camera ($\pm 70^\circ$ yaw, $\pm 35^\circ$ pitch). |
| **12** | Initiate dialogue | Player presses `[E]` on NPC; camera smoothly frames survivor with subtle mouse leeway. |
| **13** | Subtitles & Cinematic | `UIManager.ShowSubtitle` displays dialogue with typewriter effect and nodding animation. |
| **14** | Disappointed reaction | `NPCController.PlayDisappointmentReaction` drops chest/shoulders and shakes head side-to-side. |
| **15** | NPC walks to exit | `NPCController.StartExitSequence` walks along waypoints, opens exit door, steps through, and closes it. |
| **16** | Player regains control | `SequenceDirector` restores full gameplay control with completion notification. |
