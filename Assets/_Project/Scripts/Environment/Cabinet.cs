using System.Collections;
using UnityEngine;

namespace MadMantra.Environment
{
    public class Cabinet : MonoBehaviour
    {
        [SerializeField] private Transform doorHinge;
        [SerializeField] private float openAngle = -115f;
        [SerializeField] private float openDuration = 1.0f;
        [SerializeField] private GameObject keycardObject;

        private bool isOpen;

        public void Open()
        {
            if (isOpen) return;
            isOpen = true;

            if (keycardObject != null) keycardObject.SetActive(true);
            if (doorHinge != null) StartCoroutine(SwingOpen());
        }

        private IEnumerator SwingOpen()
        {
            Quaternion from = doorHinge.localRotation;
            Quaternion to = from * Quaternion.Euler(0f, openAngle, 0f);
            for (float e = 0f; e < openDuration; e += Time.deltaTime)
            {
                doorHinge.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, e / openDuration));
                yield return null;
            }
            doorHinge.localRotation = to;
        }
    }
}
