using System;
using System.Collections;
using UnityEngine;
using MadMantra.Interaction;
using MadMantra.Player;
using MadMantra.UI;

namespace MadMantra.NPC
{
    public class NPCController : MonoBehaviour, IInteractable
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform dialogueFocusPoint;

        private Action exitFocus;
        private bool isFocused;

        public string GetPrompt() => isFocused ? "" : "Press [E] to Speak with Survivor";

        public void Interact(PlayerInteractor interactor)
        {
            if (isFocused) return;
            UIManager.Instance?.HidePrompt();
            UIManager.Instance?.SetCrosshair(false);
            exitFocus = () => interactor.SetFocus(null, () => UIManager.Instance?.SetCrosshair(true));
            interactor.SetFocus(dialogueFocusPoint, () => isFocused = true);
        }

        private void Update()
        {
            if (isFocused && Input.GetKeyDown(KeyCode.Escape))
            {
                isFocused = false;
                exitFocus?.Invoke();
            }
        }

        public IEnumerator WalkTo(Vector3 targetPos, float speed = 2.0f)
        {
            targetPos.y = transform.position.y;
            if (animator != null) animator.SetBool("IsWalking", true);

            while (transform.position != targetPos)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
                yield return null;
            }

            if (animator != null) animator.SetBool("IsWalking", false);
        }

        public IEnumerator TurnTo(Quaternion targetRot, float degreesPerSec = 240f)
        {
            while (transform.rotation != targetRot)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, degreesPerSec * Time.deltaTime);
                yield return null;
            }
        }
    }
}
