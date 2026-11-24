using System.Collections;
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
        [SerializeField] private GameObject keyBadge;
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private Text feedbackText;

        private Coroutine feedbackRoutine;

        private void Awake()
        {
            Instance = this;
            HidePrompt();
            SetCrosshair(false);
            SetPuzzleHint(false);
            SetKeyBadge(false);
            SetObjective("");
            feedbackPanel?.SetActive(false);
        }

        public void SetCrosshair(bool visible) => crosshair?.SetActive(visible);
        public void SetPuzzleHint(bool visible) => puzzleHintPanel?.SetActive(visible);
        public void SetKeyBadge(bool visible) => keyBadge?.SetActive(visible);
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

        public void ShowFeedback(string message, float duration = 2.5f)
        {
            if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
            feedbackRoutine = StartCoroutine(FeedbackRoutine(message, duration));
        }

        private IEnumerator FeedbackRoutine(string message, float duration)
        {
            feedbackPanel?.SetActive(true);
            if (feedbackText != null) feedbackText.text = message;
            yield return new WaitForSeconds(duration);
            feedbackPanel?.SetActive(false);
        }
    }
}
