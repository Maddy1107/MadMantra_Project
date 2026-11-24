using UnityEngine;
using UnityEngine.UI;

namespace MadMantra.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private GameObject crosshair;
        [SerializeField] private GameObject promptPanel;
        [SerializeField] private Text promptText;
        [SerializeField] private GameObject objectivePanel;
        [SerializeField] private Text objectiveText;
        [SerializeField] private GameObject puzzleHintPanel;

        private void Awake()
        {
            Instance = this;
            HidePrompt();
            SetCrosshair(false);
            SetPuzzleHint(false);
            SetObjective("");
        }

        public void SetCrosshair(bool visible) => crosshair?.SetActive(visible);
        public void SetPuzzleHint(bool visible) => puzzleHintPanel?.SetActive(visible);
        public void HidePrompt() => ShowPrompt("");

        public void ShowPrompt(string message)
        {
            bool show = !string.IsNullOrEmpty(message);
            promptPanel?.SetActive(show);
            if (promptText != null) promptText.text = message ?? "";
        }

        public void SetObjective(string objective)
        {
            bool show = !string.IsNullOrEmpty(objective);
            objectivePanel?.SetActive(show);
            if (objectiveText != null) objectiveText.text = objective ?? "";
        }
    }
}
