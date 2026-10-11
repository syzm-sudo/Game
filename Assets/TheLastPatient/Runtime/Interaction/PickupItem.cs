using UnityEngine;

namespace TheLastPatient
{
    public sealed class PickupItem : Interactable
    {
        public string itemId;
        public string objectiveId;
        public bool battery;
        public override bool Available => battery || !GameDirector.Instance.story.HasItem(itemId);
        private void Start() { ApplyState(); GameDirector.Instance.StateRestored += ApplyState; }
        private void OnDestroy() { if (GameDirector.Instance != null) GameDirector.Instance.StateRestored -= ApplyState; }
        private void ApplyState() => gameObject.SetActive(battery || !GameDirector.Instance.story.HasItem(itemId));
        public override void Interact()
        {
            if (!CanInteract()) return;
            var game = GameDirector.Instance;
            if (battery) { game.flashlight.Refill(); return; }
            game.story.AddItem(itemId);
            if (!string.IsNullOrEmpty(objectiveId)) game.story.CompleteObjective(objectiveId);
            game.flashlight.Restore(100, true);
            game.ui.Toast("Taschenlampe gefunden · [F] ein-/ausschalten");
            gameObject.SetActive(false);
        }
    }
}
