using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MadMantra.Audio;

namespace MadMantra.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD")]
        [SerializeField] private GameObject crosshair;
        [SerializeField] private GameObject promptPanel;
        [SerializeField] private Text promptText;
        [SerializeField] private GameObject objectivePanel;
        [SerializeField] private Text objectiveText;
        [SerializeField] private GameObject puzzleHintPanel;
        [SerializeField] private GameObject keyBadge;
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private Text feedbackText;

        [Header("Subtitles")]
        [SerializeField] private GameObject subtitlePanel;
        [SerializeField] private Text speakerText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private GameObject continuePrompt;
        [SerializeField] private float charsPerSecond = 40f;

        private Coroutine feedbackRoutine;
        private Coroutine typeRoutine;
        private string fullSubtitle = "";

        public bool IsTyping { get; private set; }

        private void Awake()
        {
            Instance = this;
            HidePrompt();
            HideSubtitle();
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

        public void ShowSubtitle(string speaker, string body)
        {
            fullSubtitle = body ?? "";
            subtitlePanel?.SetActive(true);
            if (speakerText != null) speakerText.text = speaker ?? "";
            continuePrompt?.SetActive(false);

            if (typeRoutine != null) StopCoroutine(typeRoutine);
            typeRoutine = StartCoroutine(TypewriterRoutine());
        }

        public void SkipTypewriter()
        {
            if (!IsTyping) return;
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            if (subtitleText != null) subtitleText.text = fullSubtitle;
            IsTyping = false;
            continuePrompt?.SetActive(true);
        }

        public void HideSubtitle()
        {
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            IsTyping = false;
            subtitlePanel?.SetActive(false);
            continuePrompt?.SetActive(false);
        }

        private IEnumerator TypewriterRoutine()
        {
            IsTyping = true;
            if (subtitleText != null) subtitleText.text = "";
            float delay = 1f / Mathf.Max(1f, charsPerSecond);

            for (int i = 1; i <= fullSubtitle.Length; i++)
            {
                if (subtitleText != null) subtitleText.text = fullSubtitle.Substring(0, i);
                if (i % 3 == 0 && fullSubtitle[i - 1] != ' ') AudioManager.Instance?.PlayTypewriterBlip();
                yield return new WaitForSeconds(delay);
            }

            IsTyping = false;
            continuePrompt?.SetActive(true);
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
