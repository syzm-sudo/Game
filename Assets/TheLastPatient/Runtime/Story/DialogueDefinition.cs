using System;
using UnityEngine;

namespace TheLastPatient
{
    [Serializable]
    public sealed class DialogueLine
    {
        public string speaker;
        [TextArea(2, 6)] public string text;
        public AudioClip audio;
        [Min(1)] public float holdSeconds = 3;
    }

    [Serializable]
    public sealed class DialogueChoice
    {
        public string id;
        public string text;
        public string flag;
    }

    [CreateAssetMenu(menuName = "The Last Patient/Dialogue")]
    public sealed class DialogueDefinition : ScriptableObject
    {
        public string id;
        public DialogueLine[] lines;
        public DialogueChoice[] choices;
    }
}
