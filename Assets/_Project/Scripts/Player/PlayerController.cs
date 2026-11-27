using System;
using System.Collections;
using UnityEngine;
using MadMantra.Audio;

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
        private Vector3 savedMountPos;
        private Quaternion savedMountRot;
        private bool inFocus;
        private bool focusLookActive;
        private Quaternion focusBaseRot;
        private Vector2 focusOffset;
        private float stepTimer;
        private Coroutine focusRoutine;

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
            if (!CanLook && !focusLookActive) return;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            if (focusLookActive)
            {
                focusOffset.x = Mathf.Clamp(focusOffset.x + mouseX * 0.5f, -15f, 15f);
                focusOffset.y = Mathf.Clamp(focusOffset.y - mouseY * 0.5f, -10f, 10f);
                if (mouseX == 0f && mouseY == 0f)
                    focusOffset = Vector2.Lerp(focusOffset, Vector2.zero, 3f * Time.deltaTime);
                cameraMount.localRotation = focusBaseRot * Quaternion.Euler(focusOffset.y, focusOffset.x, 0f);
                return;
            }

            transform.Rotate(Vector3.up * mouseX);
            pitch = Mathf.Clamp(pitch - mouseY, minPitch, maxPitch);
            if (cameraMount != null) cameraMount.localRotation = Quaternion.Euler(pitch, 0f, 0f);
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

            if (controller.isGrounded && (moveX != 0f || moveZ != 0f))
            {
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f) { AudioManager.Instance?.PlayFootstep(); stepTimer = 0.45f; }
            }
            else stepTimer = 0.1f;
        }

        public void SetFocus(Transform target, Action onDone = null, bool allowSubtleLook = false)
        {
            if (cameraMount == null) return;
            bool enter = target != null;
            focusLookActive = false;
            focusOffset = Vector2.zero;

            if (enter && !inFocus)
            {
                savedMountPos = cameraMount.localPosition;
                savedMountRot = cameraMount.localRotation;
                CanMove = CanLook = false;
                SetCursorLocked(allowSubtleLook);
            }
            inFocus = enter;

            Vector3 toPos = enter ? cameraMount.parent.InverseTransformPoint(target.position) : savedMountPos;
            Quaternion toRot = enter ? Quaternion.Inverse(cameraMount.parent.rotation) * target.rotation : savedMountRot;
            if (focusRoutine != null) StopCoroutine(focusRoutine);
            focusRoutine = StartCoroutine(BlendCamera(toPos, toRot, !enter, enter && allowSubtleLook, onDone));
        }

        private IEnumerator BlendCamera(Vector3 toPos, Quaternion toRot, bool restoreControl, bool enableFocusLook, Action onDone)
        {
            Vector3 fromPos = cameraMount.localPosition;
            Quaternion fromRot = cameraMount.localRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f)
            {
                float s = Mathf.SmoothStep(0f, 1f, t);
                cameraMount.SetLocalPositionAndRotation(Vector3.Lerp(fromPos, toPos, s), Quaternion.Slerp(fromRot, toRot, s));
                yield return null;
            }
            cameraMount.SetLocalPositionAndRotation(toPos, toRot);
            if (enableFocusLook) { focusBaseRot = toRot; focusLookActive = true; }
            if (restoreControl) { SetCursorLocked(true); CanMove = CanLook = true; }
            onDone?.Invoke();
        }

        private void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
