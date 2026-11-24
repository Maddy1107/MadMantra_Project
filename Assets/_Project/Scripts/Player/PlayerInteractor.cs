using System;
using UnityEngine;
using MadMantra.Interaction;
using MadMantra.UI;

namespace MadMantra.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float interactRange = 3.0f;
        [SerializeField] private LayerMask interactMask;

        private PlayerController player;
        private IInteractable currentTarget;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (player != null && !player.CanLook)
            {
                if (currentTarget != null) { currentTarget = null; UIManager.Instance?.HidePrompt(); }
                return;
            }

            CheckRaycast();

            if (currentTarget != null) UIManager.Instance?.ShowPrompt(currentTarget.GetPrompt());
            else UIManager.Instance?.HidePrompt();

            if (Input.GetKeyDown(KeyCode.E) && currentTarget != null)
            {
                currentTarget.Interact(this);
            }
        }

        public Camera ActiveCamera => player != null && player.PlayerCamera != null ? player.PlayerCamera : (GetComponent<Camera>() ?? Camera.main);
        public bool HasKeycard { get => player != null && player.HasKeycard; set { if (player != null) player.HasKeycard = value; } }
        public void SetFocus(Transform target, Action onDone = null) => player?.SetFocus(target, onDone);
        public void PlayHandInteract() => player?.Hand?.PlayInteractAnimation();

        private void CheckRaycast()
        {
            Camera cam = ActiveCamera;
            if (cam == null) return;

            // Cast ray from center of screen (0.5, 0.5)
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactMask))
            {
                // Ignore self
                if (hit.collider.transform.root == transform.root)
                {
                    currentTarget = null;
                    return;
                }

                currentTarget = hit.collider.GetComponentInParent<IInteractable>();
                return;
            }

            currentTarget = null;
        }
    }
}
