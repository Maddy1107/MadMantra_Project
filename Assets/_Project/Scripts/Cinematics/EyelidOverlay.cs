using UnityEngine;

namespace MadMantra.Cinematics
{
    public class EyelidOverlay : MonoBehaviour
    {
        [SerializeField] private RectTransform topLid;
        [SerializeField] private RectTransform bottomLid;

        public float Openness { get; private set; }

        public void SetOpenness(float openness)
        {
            Openness = Mathf.Clamp01(openness);
            float lid = 0.51f * (1f - Openness);
            if (topLid != null) topLid.anchorMin = new Vector2(0f, 1f - lid);
            if (bottomLid != null) bottomLid.anchorMax = new Vector2(1f, lid);
        }
    }
}
