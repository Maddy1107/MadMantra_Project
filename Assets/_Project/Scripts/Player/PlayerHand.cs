using System.Collections;
using UnityEngine;

namespace MadMantra.Player
{
    public class PlayerHand : MonoBehaviour
    {
        [Header("Reach Target / Offset")]
        [SerializeField] private Vector3 reachOffset = new Vector3(-0.06f, 0.04f, 0.15f);

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

        public void PlayInteractAnimation()
        {
            StopAllCoroutines();
            StartCoroutine(PunchAnimation());
        }

        private IEnumerator PunchAnimation()
        {
            Vector3 targetPosition = restPosition + reachOffset;

            // 1. Move hand forward
            for (float t = 0; t < 1f; t += Time.deltaTime * reachSpeed)
            {
                transform.localPosition = Vector3.Lerp(restPosition, targetPosition, t);
                yield return null;
            }

            // 2. Move hand back
            for (float t = 0; t < 1f; t += Time.deltaTime * retractSpeed)
            {
                transform.localPosition = Vector3.Lerp(targetPosition, restPosition, t);
                yield return null;
            }

            transform.localPosition = restPosition;
        }
    }
}
