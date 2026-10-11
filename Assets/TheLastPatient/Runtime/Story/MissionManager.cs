using UnityEngine;

namespace TheLastPatient
{
    public sealed class MissionManager : MonoBehaviour
    {
        public StoryManager story;
        public GameUI ui;
        private void OnEnable() { if (story != null) story.Changed += Refresh; }
        private void OnDisable() { if (story != null) story.Changed -= Refresh; }
        private void Start() => Refresh();
        public void Refresh()
        {
            if (story == null || ui == null) return;
            var current = story.ActiveObjective;
            ui.SetMission(story.chapter != null ? story.chapter.title : "Die Ankunft",
                current != null ? current.description : "Kapitel 1 abgeschlossen.");
        }
    }
}
