using UnityEngine;
using UnityEngine.UI;

namespace MadMantra.Cinematics
{
    public class EyelidOverlay : MonoBehaviour
    {
        private RectTransform topLid;
        private RectTransform bottomLid;

        public float Openness { get; private set; }

        private void Awake()
        {
            var canvasGo = new GameObject("EyelidCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            topLid = CreateLid(canvas.transform, 1f);
            bottomLid = CreateLid(canvas.transform, 0f);
            SetOpenness(0f);
        }

        public void SetOpenness(float openness)
        {
            Openness = Mathf.Clamp01(openness);
            // Slight overlap so no seam shows when fully closed
            float lid = 0.51f * (1f - Openness);
            topLid.anchorMin = new Vector2(0f, 1f - lid);
            bottomLid.anchorMax = new Vector2(1f, lid);
        }

        private static RectTransform CreateLid(Transform parent, float edge)
        {
            var go = new GameObject("Lid", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Color.black;

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, edge);
            rt.anchorMax = new Vector2(1f, edge);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }
    }
}
