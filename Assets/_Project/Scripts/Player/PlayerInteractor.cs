using UnityEngine;
using MadMantra.Interaction;

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

            // If no mask is set in Inspector, default to the "Interactable" layer
            if (interactMask.value == 0)
            {
                int layer = LayerMask.NameToLayer("Interactable");
                interactMask = (layer != -1) ? (1 << layer) : ~0;
            }
        }

        private void Update()
        {
            CheckRaycast();

            if (Input.GetKeyDown(KeyCode.E) && currentTarget != null)
            {
                if (player != null && player.Hand != null)
                {
                    player.Hand.PlayInteractAnimation();
                }

                currentTarget.Interact(this);
            }
        }

        private void CheckRaycast()
        {
            Camera cam = player != null ? player.PlayerCamera : Camera.main;
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
