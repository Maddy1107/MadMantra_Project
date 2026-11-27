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

        private void Awake()
        {
            // Remember starting position
            restPosition = transform.localPosition;

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
            Vector3 target = worldPoint.HasValue
                ? transform.parent.InverseTransformPoint(worldPoint.Value) + pressTipOffset
                : restPosition + reachOffset;
            StartCoroutine(ReachRoutine(target, onTouch));
        }

        private IEnumerator ReachRoutine(Vector3 targetLocal, Action onTouch)
        {
            Vector3 startPos = transform.localPosition;

            for (float t = 0f; t < 1f; t += Time.deltaTime * reachSpeed)
            {
                transform.localPosition = Vector3.Lerp(startPos, targetLocal, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            transform.localPosition = targetLocal;
            onTouch?.Invoke();

            yield return new WaitForSeconds(0.08f);

            for (float t = 0f; t < 1f; t += Time.deltaTime * retractSpeed)
            {
                transform.localPosition = Vector3.Lerp(targetLocal, restPosition, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            transform.localPosition = restPosition;
        }
    }
}
