using System;
using System.Collections;
using UnityEngine;

namespace MadMantra.Player
{
    public class PlayerHand : MonoBehaviour
    {
        [Header("Reach Target / Offset")]
        [SerializeField] private Vector3 reachOffset = new Vector3(-0.06f, 0.04f, 0.15f);
        [SerializeField] private Vector3 pressTipOffset = new Vector3(0.06f, -0.05f, -0.18f);

        [Header("Animation Speeds")]
        [SerializeField] private float reachSpeed = 8.0f;
        [SerializeField] private float retractSpeed = 6.0f;

        private Vector3 restPosition;
        private Quaternion restRotation;

        private void Awake()
        {
            restPosition = transform.localPosition;
            restRotation = transform.localRotation;

            // Remove colliders so hand never blocks raycasts
            foreach (var col in GetComponentsInChildren<Collider>())
            {
                Destroy(col);
            }
        }

        public void SetPoseOffset(Vector3 offset)
        {
            transform.localPosition = restPosition + offset;
        }

        public void PlayInteractAnimation(Vector3? worldPoint = null, Action onTouch = null)
        {
            StopAllCoroutines();
            Vector3 targetPos = worldPoint.HasValue
                ? transform.parent.InverseTransformPoint(worldPoint.Value) + pressTipOffset
                : restPosition + reachOffset;

            Vector3 shoulder = restPosition + new Vector3(0.08f, -0.18f, -0.35f);
            Quaternion targetRot = worldPoint.HasValue
                ? Quaternion.LookRotation(targetPos - shoulder) * Quaternion.Euler(10f, 0f, -8f)
                : restRotation * Quaternion.Euler(-14f, -10f, -12f);

            StartCoroutine(ReachRoutine(targetPos, targetRot, onTouch));
        }

        private IEnumerator ReachRoutine(Vector3 targetPos, Quaternion targetRot, Action onTouch)
        {
            Vector3 startPos = transform.localPosition;
            Quaternion startRot = transform.localRotation;

            // 1. Lift & reach in a natural arc while aiming wrist toward target
            for (float t = 0f; t < 1f; t += Time.deltaTime * reachSpeed)
            {
                float s = Mathf.SmoothStep(0f, 1f, t);
                Vector3 arc = Vector3.up * (Mathf.Sin(s * Mathf.PI) * 0.035f);
                transform.localPosition = Vector3.Lerp(startPos, targetPos, s) + arc;
                transform.localRotation = Quaternion.Slerp(startRot, targetRot, s);
                yield return null;
            }
            transform.SetLocalPositionAndRotation(targetPos, targetRot);
            onTouch?.Invoke();

            // 2. Brief tactile press pause
            yield return new WaitForSeconds(0.08f);

            // 3. Relax smoothly back to rest pose
            for (float t = 0f; t < 1f; t += Time.deltaTime * retractSpeed)
            {
                float s = Mathf.SmoothStep(0f, 1f, t);
                transform.localPosition = Vector3.Lerp(targetPos, restPosition, s);
                transform.localRotation = Quaternion.Slerp(targetRot, restRotation, s);
                yield return null;
            }
            transform.SetLocalPositionAndRotation(restPosition, restRotation);
        }
    }
}
