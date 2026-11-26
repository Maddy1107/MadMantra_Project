using System;
using UnityEngine;

namespace MadMantra.Dialogue
{
    [Serializable]
    public struct DialogueLine
    {
        public string speaker;
        [TextArea] public string text;
    }

    [CreateAssetMenu(menuName = "MadMantra/Dialogue", fileName = "NewDialogue")]
    public class DialogueData : ScriptableObject
    {
        public DialogueLine[] lines;
    }
}
