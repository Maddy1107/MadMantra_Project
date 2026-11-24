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

        [Header("Pose")]
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
        [SerializeField] private EyelidOverlay eyelids;
        [SerializeField] private LightFlicker[] flickerLights;

        private void Awake() => Instance = this;

        private void Start()
        {
            if (player == null) return;

            if (skipWakeUp)
            {
                eyelids?.SetOpenness(1f);
                EnableGameplay();
                return;
            }

            StartCoroutine(WakeUpRoutine());
        }

        private IEnumerator WakeUpRoutine()
        {
            player.CanMove = player.CanLook = false;

            Transform mount = player.CameraMount;
            Vector3 standPos = mount.localPosition;
            Quaternion standRot = mount.localRotation;
            Vector3 liePos = standPos + lyingCameraOffset;
            Quaternion headDown = Quaternion.Euler(headDownPitch, 0f, 0f);
            Quaternion headUp = Quaternion.Euler(headUpPitch, 0f, 0f);

            // Start face-down on floor, door open, lights flickering
            mount.SetLocalPositionAndRotation(liePos, headDown);
            player.Hand?.SetPoseOffset(lyingHandOffset);
            foreach (var fl in flickerLights) fl?.Flicker(flickerDuration);

            Quaternion doorClosed = doorHinge != null ? doorHinge.localRotation : Quaternion.identity;
            if (doorHinge != null) doorHinge.localRotation = doorClosed * Quaternion.Euler(0f, doorOpenAngle, 0f);

            // 4 clear beats:
            yield return BlinkAwake();
            yield return LiftHead(mount, headDown, headUp);
            yield return SlamDoor(doorClosed);
            yield return new WaitForSeconds(reactionPause);
            yield return StandUp(mount, liePos, standPos, headUp, standRot);

            yield return new WaitForSeconds(settleDuration);
            EnableGameplay();
        }

        private IEnumerator BlinkAwake()
        {
            if (eyelids == null) yield break;
            eyelids.SetOpenness(0f);
            yield return new WaitForSeconds(1.0f);
            yield return BlinkTo(0.4f, 0.7f);
            yield return BlinkTo(0.0f, 0.3f);
            yield return new WaitForSeconds(0.4f);
            yield return BlinkTo(1.0f, 0.8f);
        }

        private IEnumerator BlinkTo(float target, float duration)
        {
            float from = eyelids.Openness;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                eyelids.SetOpenness(Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t)));
                yield return null;
            }
            eyelids.SetOpenness(target);
        }

        private IEnumerator LiftHead(Transform mount, Quaternion from, Quaternion to)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / headLiftDuration)
            {
                mount.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            mount.localRotation = to;
        }

        private IEnumerator SlamDoor(Quaternion closedRot)
        {
            if (doorHinge == null) yield break;
            Quaternion openRot = doorHinge.localRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / doorCloseDuration)
            {
                doorHinge.localRotation = Quaternion.Slerp(openRot, closedRot, t * t);
                yield return null;
            }
            doorHinge.localRotation = closedRot;
        }

        private IEnumerator StandUp(Transform mount, Vector3 fromPos, Vector3 toPos, Quaternion fromRot, Quaternion toRot)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / getUpDuration)
            {
                float s = Mathf.SmoothStep(0f, 1f, t);
                mount.SetLocalPositionAndRotation(Vector3.Lerp(fromPos, toPos, s), Quaternion.Slerp(fromRot, toRot, s));
                player.Hand?.SetPoseOffset(Vector3.Lerp(lyingHandOffset, Vector3.zero, s));
                yield return null;
            }
            mount.SetLocalPositionAndRotation(toPos, toRot);
            player.Hand?.SetPoseOffset(Vector3.zero);
        }

        private void EnableGameplay()
        {
            player.CanMove = player.CanLook = true;
            UIManager.Instance?.SetCrosshair(true);
            UIManager.Instance?.SetObjective("Inspect the room and access the control panel.");
        }
    }
}
