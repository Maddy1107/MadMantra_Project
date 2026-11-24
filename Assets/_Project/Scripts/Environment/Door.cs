using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using MadMantra.Interaction;
using MadMantra.Player;
using MadMantra.UI;

namespace MadMantra.Environment
{
    public enum LockMode { Unlocked, Keycard, External }

    public class Door : MonoBehaviour, IInteractable
    {
        [SerializeField] private float openAngle = -90f;
        [SerializeField] private float openDuration = 0.9f;
        [SerializeField] private LockMode lockMode = LockMode.Unlocked;
        [SerializeField] private bool canClose = true;
        [SerializeField] private string lockedMessage = "Locked";
        [SerializeField] private UnityEvent onOpened;

        private Quaternion closedRot;
        private Coroutine swingRoutine;

        public bool IsOpen { get; private set; }
        public bool IsLocked
        {
            get => lockMode != LockMode.Unlocked;
            set => lockMode = value ? LockMode.Keycard : LockMode.Unlocked;
        }

        private void Awake() => closedRot = transform.localRotation;

        public string GetPrompt()
        {
            if (lockMode == LockMode.External) return "";
            if (IsOpen) return canClose ? "Press [E] to Close" : "";
            return lockMode == LockMode.Keycard ? "Press [E] to Unlock" : "Press [E] to Open";
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (lockMode == LockMode.External) return;
            if (IsOpen && !canClose) return;

            if (lockMode == LockMode.Keycard && !interactor.HasKeycard)
            {
                UIManager.Instance?.ShowFeedback(lockedMessage);
                return;
            }

            interactor.PlayHandInteract();
            lockMode = LockMode.Unlocked;
            if (IsOpen) CloseDoor(); else OpenDoor();
        }

        public void OpenDoor()
        {
            if (IsOpen) return;
            IsOpen = true;
            lockMode = LockMode.Unlocked;
            SwingTo(closedRot * Quaternion.Euler(0f, openAngle, 0f));
            onOpened?.Invoke();
        }

        public void CloseDoor()
        {
            if (!IsOpen) return;
            IsOpen = false;
            SwingTo(closedRot);
        }

        private void SwingTo(Quaternion target)
        {
            if (swingRoutine != null) StopCoroutine(swingRoutine);
            swingRoutine = StartCoroutine(SwingRoutine(target));
        }

        private IEnumerator SwingRoutine(Quaternion target)
        {
            Quaternion from = transform.localRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / openDuration)
            {
                transform.localRotation = Quaternion.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            transform.localRotation = target;
            swingRoutine = null;
        }
    }
}
