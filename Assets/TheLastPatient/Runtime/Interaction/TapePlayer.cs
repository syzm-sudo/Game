using UnityEngine;

namespace TheLastPatient
{
    public sealed class TapePlayer : Interactable
    {
        public string tapeId;
        public DialogueDefinition dialogue;
        public string objectiveId;
        public override void Interact()
        {
            if (!CanInteract()) return;
            var game = GameDirector.Instance;
            game.ambience.TapeClick();
            game.dialogue.Play(dialogue, () =>
            {
                game.story.AddDocument(tapeId);
                game.story.SetFlag(StoryIds.TapeHeard);
                game.story.CompleteObjective(objectiveId);
            });
        }
    }
}
