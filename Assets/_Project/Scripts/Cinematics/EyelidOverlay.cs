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
            // Extra overlap so soft feathered edges seal to solid black at Openness = 0
            float lid = 0.78f * (1f - Openness);
            if (topLid != null) topLid.anchorMin = new Vector2(0f, 1f - lid);
            if (bottomLid != null) bottomLid.anchorMax = new Vector2(1f, lid);
        }
    }
}
