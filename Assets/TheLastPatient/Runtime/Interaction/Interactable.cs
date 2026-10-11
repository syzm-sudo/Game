using UnityEngine;

namespace TheLastPatient
{
    public abstract class Interactable : MonoBehaviour
    {
        public string label;
        public string[] requiredFlags;
        public string lockedMessage = "Untersuche zuerst den Empfang.";
        public virtual string Prompt => label;
        public virtual bool Available => true;
        protected bool CanInteract()
        {
            var game = GameDirector.Instance;
            if (game == null) return false;
            if (game.story.HasAll(requiredFlags)) return true;
            game.ui.Toast(lockedMessage);
            return false;
        }
        public abstract void Interact();
    }
}
