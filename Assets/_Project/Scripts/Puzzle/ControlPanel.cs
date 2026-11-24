using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using MadMantra.Interaction;
using MadMantra.Player;
using MadMantra.UI;

namespace MadMantra.Puzzle
{
    public class ControlPanel : MonoBehaviour, IInteractable
    {
        [SerializeField] private PuzzleButton[] puzzleButtons = new PuzzleButton[3];
        [SerializeField] private int[] code = { 0, 1, 2 };
        [SerializeField] private Transform cameraFocusPoint;
        [SerializeField] private UnityEvent onSolved;

        private Action exitFocus;
        private Camera focusCam;
        private Queue<int> remaining;
        private bool busy;

        public bool IsInPuzzleView { get; private set; }
        public bool IsSolved { get; private set; }

        private void Awake() => ResetQueue();
        private void ResetQueue() => remaining = new Queue<int>(code);

        public string GetPrompt() => IsSolved ? "Control Panel (Unlocked)" : "Press [E] to Access Control Panel";

        public void Interact(PlayerInteractor interactor)
        {
            if (IsSolved || IsInPuzzleView || busy) return;
            busy = true;
            focusCam = interactor.ActiveCamera;
            UIManager.Instance?.HidePrompt();
            UIManager.Instance?.SetCrosshair(false);
            exitFocus = () => interactor.SetFocus(null, () => UIManager.Instance?.SetCrosshair(true));
            interactor.SetFocus(cameraFocusPoint, () => { IsInPuzzleView = true; busy = false; UIManager.Instance?.SetPuzzleHint(true); });
        }

        private void Update()
        {
            if (!IsInPuzzleView || busy || IsSolved) return;

            if (Input.GetKeyDown(KeyCode.Escape)) LeavePanel();
            else if (Input.GetMouseButtonDown(0)) CheckButtonClick();
        }

        private void CheckButtonClick()
        {
            if (focusCam == null) return;
            if (!Physics.Raycast(focusCam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 5f)) return;

            PuzzleButton btn = hit.collider.GetComponentInParent<PuzzleButton>();
            if (btn != null) PressButton(btn.ButtonIndex);
        }

        public void PressButton(int index)
        {
            if (IsSolved || busy || puzzleButtons[index].IsPressed) return;

            puzzleButtons[index].Press();
            if (index != remaining.Peek()) { StartCoroutine(FailRoutine()); return; }

            remaining.Dequeue();
            if (remaining.Count == 0) StartCoroutine(SolveRoutine());
        }

        private IEnumerator FailRoutine()
        {
            busy = true;
            foreach (var b in puzzleButtons) b.SetFailState();
            yield return new WaitForSeconds(0.55f);
            foreach (var b in puzzleButtons) b.Release();
            ResetQueue();
            busy = false;
        }

        private IEnumerator SolveRoutine()
        {
            IsSolved = true;
            Debug.Log("[ControlPanel] Puzzle solved!");
            foreach (var b in puzzleButtons) b.SetSolvedState();
            onSolved?.Invoke();
            yield return new WaitForSeconds(0.45f);
            LeavePanel();
        }

        private void LeavePanel()
        {
            if (!IsInPuzzleView) return;
            IsInPuzzleView = false;
            UIManager.Instance?.SetPuzzleHint(false);
            exitFocus?.Invoke();
        }
    }
}
