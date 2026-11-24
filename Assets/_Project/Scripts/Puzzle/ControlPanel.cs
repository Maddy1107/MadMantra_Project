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
        [SerializeField] private Transform solvedFocusPoint;
        [SerializeField] private float solvedFocusDuration = 1.4f;
        [SerializeField] private UnityEvent onSolved;

        private Action<Transform, Action> setFocus;
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
            setFocus = interactor.SetFocus;
            UIManager.Instance?.HidePrompt();
            UIManager.Instance?.SetCrosshair(false);
            setFocus(cameraFocusPoint, () => { IsInPuzzleView = true; busy = false; UIManager.Instance?.SetPuzzleHint(true); });
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
            foreach (var b in puzzleButtons) b.SetSolvedState();
            UIManager.Instance?.SetPuzzleHint(false);

            yield return new WaitForSeconds(0.3f);

            if (solvedFocusPoint != null && setFocus != null)
            {
                bool arrived = false;
                setFocus(solvedFocusPoint, () => arrived = true);
                yield return new WaitUntil(() => arrived);
            }

            onSolved?.Invoke();
            yield return new WaitForSeconds(solvedFocusDuration);
            LeavePanel();
        }

        private void LeavePanel()
        {
            if (!IsInPuzzleView) return;
            IsInPuzzleView = false;
            UIManager.Instance?.SetPuzzleHint(false);
            setFocus?.Invoke(null, () => UIManager.Instance?.SetCrosshair(true));
        }
    }
}
