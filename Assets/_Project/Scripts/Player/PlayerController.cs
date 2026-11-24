using UnityEngine;

namespace MadMantra.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.0f;
        [SerializeField] private float gravity = -10.0f;

        [Header("Look")]
        [SerializeField] private Transform cameraMount;
        [SerializeField] private float mouseSensitivity = 2.0f;
        [SerializeField] private float minPitch = -85.0f;
        [SerializeField] private float maxPitch = 85.0f;

        [Header("Hand")]
        [SerializeField] private PlayerHand playerHand;

        private CharacterController controller;
        private float pitch;
        private float verticalVelocity;

        public bool CanMove { get; set; } = true;
        public bool CanLook { get; set; } = true;
        public bool HasKeycard { get; set; } = false;

        public Transform CameraMount => cameraMount;
        public Camera PlayerCamera { get; private set; }
        public PlayerHand Hand => playerHand;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            PlayerCamera = cameraMount != null ? cameraMount.GetComponentInChildren<Camera>() : Camera.main;
            if (playerHand == null)
            {
                playerHand = GetComponentInChildren<PlayerHand>();
            }
        }

        private void Start()
        {
            SetCursorLocked(true);
        }

        private void Update()
        {
            // Click to lock cursor
            if (Input.GetMouseButtonDown(0) && CanLook && Cursor.lockState != CursorLockMode.Locked)
            {
                SetCursorLocked(true);
            }

            HandleLook();
            HandleMovement();
        }

        private void HandleLook()
        {
            if (!CanLook) return;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            pitch = Mathf.Clamp(pitch - mouseY, minPitch, maxPitch);
            if (cameraMount != null)
            {
                cameraMount.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            if (!CanMove)
            {
                verticalVelocity = -2f;
                controller.Move(Vector3.down * 0.05f * Time.deltaTime);
                return;
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");
            Vector3 moveDirection = (transform.right * moveX + transform.forward * moveZ).normalized * walkSpeed;

            verticalVelocity += gravity * Time.deltaTime;
            moveDirection.y = verticalVelocity;

            controller.Move(moveDirection * Time.deltaTime);
        }

        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
