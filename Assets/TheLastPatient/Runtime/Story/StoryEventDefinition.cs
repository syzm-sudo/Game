using UnityEngine;

namespace TheLastPatient
{
    [CreateAssetMenu(menuName = "The Last Patient/Story Event")]
    public sealed class StoryEventDefinition : ScriptableObject
    {
        public string id;
        public string[] requiredFlags;
        public string[] setFlags;
        public string[] completeObjectives;
        public DialogueDefinition dialogue;
        public string cutsceneId;
        public bool once = true;
    }
}
