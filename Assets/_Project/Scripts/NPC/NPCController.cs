using System;
using System.Collections;
using UnityEngine;
using MadMantra.Dialogue;
using MadMantra.Environment;
using MadMantra.Interaction;
using MadMantra.Player;
using MadMantra.UI;

namespace MadMantra.NPC
{
    public class NPCController : MonoBehaviour, IInteractable
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform dialogueFocusPoint;
        [SerializeField] private DialogueData dialogue;
        [SerializeField] private Door exitDoor;
        [SerializeField] private Vector3 exitInsidePos = new Vector3(0f, 0f, 10.0f);
        [SerializeField] private Vector3 exitOutsidePos = new Vector3(0f, 0f, 12.6f);
        [SerializeField] private float disappointedDuration = 2.6f;

        private Action exitFocus;
        private bool isFocused;
        private bool hasTalked;

        public string GetPrompt() => (isFocused || hasTalked) ? "" : "Press [E] to Speak with Survivor";

        public void Interact(PlayerInteractor interactor)
        {
            if (isFocused || hasTalked) return;
            UIManager.Instance?.HidePrompt();
            UIManager.Instance?.SetCrosshair(false);
            exitFocus = () => interactor.SetFocus(null, () => UIManager.Instance?.SetCrosshair(true));
            interactor.SetFocus(dialogueFocusPoint, OnFocusArrived);
        }

        private void OnFocusArrived()
        {
            isFocused = true;
            if (dialogue != null && DialogueRunner.Instance != null)
                DialogueRunner.Instance.Play(dialogue, FinishDialogue, _ => { if (animator != null) animator.SetTrigger("Talk"); });
        }

        private void FinishDialogue()
        {
            hasTalked = true;
            isFocused = false;
            StartCoroutine(DisappointedAndExitRoutine());
        }

        private IEnumerator DisappointedAndExitRoutine()
        {
            if (animator != null) animator.SetTrigger("Disappointed");
            yield return new WaitForSeconds(disappointedDuration);

            exitFocus?.Invoke();

            yield return TurnTo(Quaternion.identity);
            yield return WalkTo(exitInsidePos);

            exitDoor?.OpenDoor();
            yield return new WaitForSeconds(0.45f);

            yield return WalkTo(exitOutsidePos);
            exitDoor?.CloseDoor();

            UIManager.Instance?.SetObjective("Prototype Complete");
            UIManager.Instance?.ShowFeedback("The survivor left through the exit.");
        }

        private void Update()
        {
            if (isFocused && Input.GetKeyDown(KeyCode.Escape))
            {
                DialogueRunner.Instance?.Stop();
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
