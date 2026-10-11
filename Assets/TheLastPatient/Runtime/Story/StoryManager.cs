using System;
using UnityEngine;

namespace TheLastPatient
{
    public sealed class StoryManager : MonoBehaviour
    {
        public ChapterDefinition chapter;
        public SaveData State { get; private set; } = new SaveData();
        public event Action Changed;
        public event Action<string> ObjectiveCompleted;
        public event Action<StoryEventDefinition> EventRaised;

        public ObjectiveDefinition ActiveObjective
        {
            get
            {
                if (chapter == null || chapter.objectives == null) return null;
                foreach (var objective in chapter.objectives)
                    if (objective != null && !State.completedObjectives.Contains(objective.id)) return objective;
                return null;
            }
        }

        public bool HasFlag(string id) => !string.IsNullOrEmpty(id) && State.flags.Contains(id);
        public bool HasItem(string id) => State.inventory.Contains(id);
        public bool IsComplete(string id) => State.completedObjectives.Contains(id);
        public bool HasAll(string[] flags)
        {
            if (flags == null) return true;
            foreach (var flag in flags) if (!HasFlag(flag)) return false;
            return true;
        }

        public void SetFlag(string id, bool value = true)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (value && !State.flags.Contains(id)) State.flags.Add(id);
            if (!value) State.flags.Remove(id);
            Changed?.Invoke();
        }

        public void AddItem(string id)
        {
            if (!State.inventory.Contains(id)) State.inventory.Add(id);
            SetFlag(id);
        }

        public void AddDocument(string id)
        {
            if (!State.documents.Contains(id)) State.documents.Add(id);
            Changed?.Invoke();
        }

        public void Choose(DialogueChoice choice)
        {
            if (choice == null) return;
            if (!State.choices.Contains(choice.id)) State.choices.Add(choice.id);
            SetFlag(choice.flag);
        }

        public bool CompleteObjective(string id)
        {
            // Only the next mission may advance: interacting out of order never skips a goal.
            if (ActiveObjective == null || ActiveObjective.id != id) return false;
            State.completedObjectives.Add(id);
            State.currentObjective = ActiveObjective != null ? ActiveObjective.id : string.Empty;
            Changed?.Invoke();
            ObjectiveCompleted?.Invoke(id);
            return true;
        }

        public bool Raise(StoryEventDefinition definition)
        {
            if (definition == null || !HasAll(definition.requiredFlags)) return false;
            string marker = "event." + definition.id;
            if (definition.once && HasFlag(marker)) return false;
            if (definition.once) SetFlag(marker);
            if (definition.setFlags != null) foreach (var flag in definition.setFlags) SetFlag(flag);
            if (definition.completeObjectives != null)
                foreach (var id in definition.completeObjectives) CompleteObjective(id);
            EventRaised?.Invoke(definition);
            return true;
        }

        public void Restore(SaveData data)
        {
            State = data ?? new SaveData();
            State.currentObjective = ActiveObjective != null ? ActiveObjective.id : string.Empty;
            Changed?.Invoke();
        }
    }
}
