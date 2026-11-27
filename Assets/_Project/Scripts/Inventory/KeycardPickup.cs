using UnityEngine;
using MadMantra.Audio;
using MadMantra.Interaction;
using MadMantra.Player;
using MadMantra.UI;

namespace MadMantra.Inventory
{
    public class KeycardPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private float bobAmplitude = 0.03f;
        [SerializeField] private float bobSpeed = 2.5f;
        [SerializeField] private float spinSpeed = 50f;

        private Vector3 startLocalPos;

        private void Awake() => startLocalPos = transform.localPosition;

        private void Update()
        {
            transform.localPosition = startLocalPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobAmplitude);
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
        }

        public string GetPrompt() => "Press [E] to Pick Up Security Keycard";

        public void Interact(PlayerInteractor interactor)
        {
            interactor.PlayHandInteract();
            AudioManager.Instance?.PlayKeyPickup();
            interactor.HasKeycard = true;

            UIManager.Instance?.HidePrompt();
            UIManager.Instance?.SetKeyBadge(true);
            UIManager.Instance?.ShowFeedback("Security Keycard Acquired");
            UIManager.Instance?.SetObjective("Unlock the security door to Room 2.");

            gameObject.SetActive(false);
        }
    }
}
