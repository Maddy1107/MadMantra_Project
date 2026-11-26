using System.Collections;
using UnityEngine;

namespace MadMantra.NPC
{
    public class NPCController : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        public IEnumerator WalkTo(Vector3 targetPos, float speed = 2.0f)
        {
            targetPos.y = transform.position.y;
            Vector3 dir = targetPos - transform.position;
            Quaternion look = dir != Vector3.zero ? Quaternion.LookRotation(-dir) : transform.rotation;

            if (animator != null) animator.SetBool("IsWalking", true);

            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, 8f * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPos;

            if (animator != null) animator.SetBool("IsWalking", false);
        }

        public IEnumerator TurnTo(Quaternion targetRot, float degreesPerSec = 240f)
        {
            while (transform.rotation != targetRot)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, degreesPerSec * Time.deltaTime);
                yield return null;
            }
        }
    }
}
