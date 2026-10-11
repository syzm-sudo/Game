using UnityEngine;

namespace TheLastPatient
{
    [CreateAssetMenu(menuName = "The Last Patient/Chapter")]
    public sealed class ChapterDefinition : ScriptableObject
    {
        public string id;
        public string title;
        public ObjectiveDefinition[] objectives;
        public DialogueDefinition intro;
    }
}
