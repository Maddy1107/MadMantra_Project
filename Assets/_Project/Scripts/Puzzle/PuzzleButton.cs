using UnityEngine;

namespace MadMantra.Puzzle
{
    public class PuzzleButton : MonoBehaviour
    {
        [SerializeField] private int buttonIndex;
        [SerializeField] private Transform movingMesh;
        [SerializeField] private Renderer indicatorLight;
        [SerializeField] private Vector3 pressOffset = new Vector3(0f, 0f, 0.02f);
        [SerializeField] private float pressSpeed = 12f;

        [SerializeField] private Color offColor = new Color(0.15f, 0.15f, 0.15f);
        [SerializeField] private Color activeColor = new Color(0.15f, 0.9f, 1.0f);
        [SerializeField] private Color solvedColor = new Color(0.2f, 1.0f, 0.3f);
        [SerializeField] private Color failColor = new Color(1.0f, 0.2f, 0.15f);

        private Vector3 restPos;
        private Vector3 targetPos;
        private Material lightMat;

        public int ButtonIndex => buttonIndex;
        public bool IsPressed { get; private set; }
        public Vector3 PressPosition => movingMesh != null ? movingMesh.position : transform.position;

        private void Awake()
        {
            if (movingMesh != null) restPos = targetPos = movingMesh.localPosition;
            if (indicatorLight != null) lightMat = indicatorLight.material;
            ResetLight();
        }

        private void Update()
        {
            if (movingMesh != null)
                movingMesh.localPosition = Vector3.MoveTowards(movingMesh.localPosition, targetPos, pressSpeed * Time.deltaTime * 0.05f);
        }

        public void Press() { IsPressed = true; targetPos = restPos + pressOffset; SetActiveState(); }
        public void Release() { IsPressed = false; targetPos = restPos; ResetLight(); }

        public void SetActiveState() => SetLight(activeColor, true);
        public void SetSolvedState() => SetLight(solvedColor, true);
        public void SetFailState() => SetLight(failColor, true);
        public void ResetLight() => SetLight(offColor, false);

        private void SetLight(Color color, bool on)
        {
            if (lightMat == null) return;
            lightMat.color = color;
            if (on) lightMat.EnableKeyword("_EMISSION");
            lightMat.SetColor("_EmissionColor", on ? color * 1.5f : Color.black);
        }
    }
}
