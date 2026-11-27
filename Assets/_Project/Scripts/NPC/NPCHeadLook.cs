using UnityEngine;

namespace MadMantra.NPC
{
    [RequireComponent(typeof(Animator))]
    public class NPCHeadLook : MonoBehaviour
    {
        [SerializeField] private float maxRange = 6f;
        [SerializeField] private float maxAngle = 60f;
        [SerializeField] private float blendSpeed = 4f;

        private Animator animator;
        private Transform target;
        private float weight;

        public bool IsTracking { get; set; } = true;

        private void Awake() => animator = GetComponent<Animator>();

        private void OnAnimatorIK(int layerIndex)
        {
            if (target == null && Camera.main != null) target = Camera.main.transform;

            bool canLook = IsTracking && target != null
                && Vector3.Distance(transform.position, target.position) <= maxRange
                && Vector3.Angle(transform.forward, target.position - transform.position) <= maxAngle;

            weight = Mathf.MoveTowards(weight, canLook ? 1f : 0f, blendSpeed * Time.deltaTime);
            animator.SetLookAtWeight(weight, 0.15f, 0.85f, 0.9f, 0.6f);
            if (target != null) animator.SetLookAtPosition(target.position);
        }
    }
}
