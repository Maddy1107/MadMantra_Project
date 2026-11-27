using UnityEngine;

namespace MadMantra.Cinematics
{
    public class EyelidOverlay : MonoBehaviour
    {
        [SerializeField] private RectTransform topLid;
        [SerializeField] private RectTransform bottomLid;

        public void SetOpenness(float openness)
        {
            // Extra overlap so soft feathered edges seal to solid black at 0
            float lid = 0.78f * (1f - Mathf.Clamp01(openness));
            if (topLid != null) topLid.anchorMin = new Vector2(0f, 1f - lid);
            if (bottomLid != null) bottomLid.anchorMax = new Vector2(1f, lid);
        }
    }
}
