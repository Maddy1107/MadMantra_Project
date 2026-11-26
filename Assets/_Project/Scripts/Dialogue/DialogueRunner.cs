using System;
using System.Collections;
using UnityEngine;
using MadMantra.UI;

namespace MadMantra.Dialogue
{
    public class DialogueRunner : MonoBehaviour
    {
        public static DialogueRunner Instance { get; private set; }

        public bool IsPlaying { get; private set; }
        private Coroutine playRoutine;

        private void Awake() => Instance = this;

        public void Play(DialogueData data, Action onComplete = null, Action<DialogueLine> onLineStarted = null)
        {
            if (data == null || data.lines == null || data.lines.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            if (playRoutine != null) StopCoroutine(playRoutine);
            playRoutine = StartCoroutine(PlayRoutine(data, onComplete, onLineStarted));
        }

        public void Stop()
        {
            if (playRoutine != null) StopCoroutine(playRoutine);
            playRoutine = null;
            IsPlaying = false;
            UIManager.Instance?.HideSubtitle();
        }

        private IEnumerator PlayRoutine(DialogueData data, Action onComplete, Action<DialogueLine> onLineStarted)
        {
            IsPlaying = true;

            foreach (DialogueLine line in data.lines)
            {
                onLineStarted?.Invoke(line);
                UIManager.Instance?.ShowSubtitle(line.speaker, line.text);
                yield return null;

                while (true)
                {
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        if (UIManager.Instance != null && UIManager.Instance.IsTyping)
                            UIManager.Instance.SkipTypewriter();
                        else
                            break;
                    }
                    yield return null;
                }
            }

            UIManager.Instance?.HideSubtitle();
            IsPlaying = false;
            playRoutine = null;
            onComplete?.Invoke();
        }
    }
}
