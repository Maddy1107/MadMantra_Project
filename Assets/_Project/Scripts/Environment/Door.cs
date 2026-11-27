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
        [SerializeField] private Transform unlockKeyVisual;
        [SerializeField] private UnityEvent onOpened;

        private Quaternion closedRot;
        private Coroutine swingRoutine;
        private bool isUnlocking;

        public bool IsOpen { get; private set; }
        public bool IsLocked
        {
            get => lockMode != LockMode.Unlocked;
            set => lockMode = value ? LockMode.Keycard : LockMode.Unlocked;
        }

        private void Awake()
        {
            closedRot = transform.localRotation;
            if (unlockKeyVisual != null) unlockKeyVisual.gameObject.SetActive(false);
        }

        public string GetPrompt()
        {
            if (lockMode == LockMode.External || isUnlocking) return "";
            if (IsOpen) return canClose ? "Press [E] to Close" : "";
            return lockMode == LockMode.Keycard ? "Press [E] to Unlock" : "Press [E] to Open";
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (lockMode == LockMode.External || isUnlocking) return;
            if (IsOpen && !canClose) return;

            if (lockMode == LockMode.Keycard)
            {
                if (!interactor.HasKeycard)
                {
                    UIManager.Instance?.ShowFeedback(lockedMessage);
                    return;
                }
                if (unlockKeyVisual != null)
                {
                    StartCoroutine(UnlockRoutine());
                    return;
                }
                lockMode = LockMode.Unlocked;
            }

            interactor.PlayHandInteract();
            if (IsOpen) CloseDoor(); else OpenDoor();
        }

        private IEnumerator UnlockRoutine()
        {
            isUnlocking = true;
            UIManager.Instance?.HidePrompt();

            Vector3 inPos = unlockKeyVisual.localPosition;
            Vector3 outPos = inPos - Vector3.forward * 0.18f;
            Quaternion startRot = unlockKeyVisual.localRotation;
            Quaternion turnedRot = startRot * Quaternion.Euler(0f, 0f, 90f);

            unlockKeyVisual.SetLocalPositionAndRotation(outPos, startRot);
            unlockKeyVisual.gameObject.SetActive(true);

            yield return MoveKey(inPos, startRot, 0.35f);
            yield return MoveKey(inPos, turnedRot, 0.25f);
            yield return new WaitForSeconds(0.15f);
            yield return MoveKey(outPos, turnedRot, 0.25f);

            unlockKeyVisual.SetLocalPositionAndRotation(inPos, startRot);
            unlockKeyVisual.gameObject.SetActive(false);

            lockMode = LockMode.Unlocked;
            isUnlocking = false;
            UIManager.Instance?.ShowFeedback("Door Unlocked");
            OpenDoor();
        }

        private IEnumerator MoveKey(Vector3 toPos, Quaternion toRot, float duration)
        {
            Vector3 fromPos = unlockKeyVisual.localPosition;
            Quaternion fromRot = unlockKeyVisual.localRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float s = Mathf.SmoothStep(0f, 1f, t);
                unlockKeyVisual.SetLocalPositionAndRotation(Vector3.Lerp(fromPos, toPos, s), Quaternion.Slerp(fromRot, toRot, s));
                yield return null;
            }
            unlockKeyVisual.SetLocalPositionAndRotation(toPos, toRot);
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
