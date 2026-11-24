using System.Collections;
using UnityEngine;
using MadMantra.Player;
using MadMantra.Environment;
using MadMantra.UI;

namespace MadMantra.Cinematics
{
    public class SequenceDirector : MonoBehaviour
    {
        public static SequenceDirector Instance { get; private set; }

        // Groggy wake-up blinks: (target openness, seconds to reach it)
        private static readonly Vector2[] BlinkKeys =
        {
            new Vector2(0f, 1.2f),   // still unconscious
            new Vector2(0.35f, 0.8f),
            new Vector2(0f, 0.3f),
            new Vector2(0f, 0.5f),
            new Vector2(0.7f, 0.6f),
            new Vector2(0.1f, 0.2f),
            new Vector2(1f, 0.7f),
        };

        [Header("Face-Down Pose")]
        [SerializeField] private Vector3 lyingCameraOffset = new Vector3(0f, -1.45f, 0.15f);
        [SerializeField] private Vector3 lyingHandOffset = new Vector3(-0.15f, 0.24f, -0.12f);
        [SerializeField] private float headDownPitch = 40f;
        [SerializeField] private float headUpPitch = 0f;

        [Header("Timing")]
        [SerializeField] private float headLiftDuration = 1.6f;
        [SerializeField] private float reactionPause = 0.8f;
        [SerializeField] private float getUpDuration = 4.5f;
        [SerializeField] private float settleDuration = 0.8f;
        [SerializeField] private float flickerDuration = 6f;

        [Header("Door Slam")]
        [SerializeField] private Transform doorHinge;
        [SerializeField] private float doorOpenAngle = -90f;
        [SerializeField] private float doorCloseDuration = 0.6f;

        [Header("Debug")]
        [SerializeField] private bool skipWakeUp = true;

        [Header("References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private LightFlicker[] flickerLights;

        private EyelidOverlay eyelids;

        private void Awake()
        {
            Instance = this;
            eyelids = gameObject.AddComponent<EyelidOverlay>();
        }

        private void Start()
        {
            if (player == null) return;

            if (skipWakeUp)
            {
                eyelids.SetOpenness(1f);
                player.CanMove = true;
                player.CanLook = true;
                UIManager.Instance?.SetCrosshair(true);
                UIManager.Instance?.SetObjective("Inspect the room and access the control panel.");
                return;
            }

            StartCoroutine(WakeUpRoutine());
        }

        private IEnumerator WakeUpRoutine()
        {
            player.CanMove = false;
            player.CanLook = false;

            Transform mount = player.CameraMount;
            Transform hand = player.Hand != null ? player.Hand.transform : null;
            Vector3 standingPos = mount.localPosition;
            Quaternion standingRot = mount.localRotation;
            Vector3 lyingPos = standingPos + lyingCameraOffset;
            Quaternion headDownRot = Quaternion.Euler(headDownPitch, 0f, 0f);
            Quaternion headUpRot = Quaternion.Euler(headUpPitch, 0f, 0f);

            // Plant the hand on the floor using the head-up pose, then keep it there in world space
            mount.localPosition = lyingPos;
            mount.localRotation = headUpRot;
            Vector3 handFloorPos = Vector3.zero;
            Quaternion handFloorRot = Quaternion.identity;
            if (hand != null)
            {
                player.Hand.SetPoseOffset(lyingHandOffset);
                handFloorPos = hand.position;
                handFloorRot = hand.rotation;
            }

            mount.localRotation = headDownRot;
            if (hand != null) hand.SetPositionAndRotation(handFloorPos, handFloorRot);

            Quaternion doorClosedRot = Quaternion.identity;
            if (doorHinge != null)
            {
                doorClosedRot = doorHinge.localRotation;
                doorHinge.localRotation = doorClosedRot * Quaternion.Euler(0f, doorOpenAngle, 0f);
            }

            foreach (var fl in flickerLights)
            {
                if (fl != null) fl.Flicker(flickerDuration);
            }

            // 1. Eyes flutter open
            foreach (Vector2 key in BlinkKeys)
            {
                float from = eyelids.Openness;
                for (float e = 0f; e < key.y; e += Time.deltaTime)
                {
                    eyelids.SetOpenness(Mathf.Lerp(from, key.x, Mathf.SmoothStep(0f, 1f, e / key.y)));
                    yield return null;
                }
                eyelids.SetOpenness(key.x);
            }

            // 2. Lift head off the floor
            for (float e = 0f; e < headLiftDuration; e += Time.deltaTime)
            {
                mount.localRotation = Quaternion.Slerp(headDownRot, headUpRot, Mathf.SmoothStep(0f, 1f, e / headLiftDuration));
                if (hand != null) hand.SetPositionAndRotation(handFloorPos, handFloorRot);
                yield return null;
            }
            mount.localRotation = headUpRot;
            if (hand != null) player.Hand.SetPoseOffset(lyingHandOffset);

            // 3. Door slams shut in front of the player
            if (doorHinge != null)
            {
                Quaternion doorOpenRot = doorHinge.localRotation;
                for (float e = 0f; e < doorCloseDuration; e += Time.deltaTime)
                {
                    float t = e / doorCloseDuration;
                    doorHinge.localRotation = Quaternion.Slerp(doorOpenRot, doorClosedRot, t * t);
                    yield return null;
                }
                doorHinge.localRotation = doorClosedRot;
            }

            yield return new WaitForSeconds(reactionPause);

            // 4. Get up to standing
            for (float e = 0f; e < getUpDuration; e += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, e / getUpDuration);
                float swayX = Mathf.Sin(e * 2f) * 0.02f * (1f - t);
                float swayY = Mathf.Cos(e * 1.5f) * 0.015f * (1f - t);

                mount.localPosition = Vector3.Lerp(lyingPos, standingPos, t) + new Vector3(swayX, swayY, 0f);
                mount.localRotation = Quaternion.Slerp(headUpRot, standingRot, t);
                if (hand != null) player.Hand.SetPoseOffset(Vector3.Lerp(lyingHandOffset, Vector3.zero, t));
                yield return null;
            }

            mount.localPosition = standingPos;
            mount.localRotation = standingRot;
            if (hand != null) player.Hand.SetPoseOffset(Vector3.zero);

            yield return new WaitForSeconds(settleDuration);

            player.CanMove = true;
            player.CanLook = true;
            UIManager.Instance?.SetCrosshair(true);
            UIManager.Instance?.SetObjective("Inspect the room and access the control panel.");
        }
    }
}
