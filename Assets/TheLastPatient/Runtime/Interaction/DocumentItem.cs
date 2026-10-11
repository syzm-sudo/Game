using UnityEngine;

namespace TheLastPatient
{
    public sealed class DocumentItem : Interactable
    {
        public string documentId;
        public string title;
        [TextArea(5, 20)] public string text;
        public string objectiveId;
        public string hiddenItem;
        public string hiddenItemObjective;
        public override void Interact()
        {
            if (!CanInteract()) return;
            var game = GameDirector.Instance;
            game.story.AddDocument(documentId);
            game.story.CompleteObjective(objectiveId);
            bool canTake = !string.IsNullOrEmpty(hiddenItem) && !game.story.HasItem(hiddenItem);
            game.ui.ShowDocument(title, text, canTake ? "Schlüssel aus der Akte nehmen" : null, () =>
            {
                game.story.AddItem(hiddenItem);
                game.story.CompleteObjective(hiddenItemObjective);
                game.story.State.puzzles.Add("puzzle.01.file_key");
                game.ui.Toast("Schlüssel zur Patientenstation gefunden.");
            });
        }
    }
}
