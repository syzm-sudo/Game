using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TheLastPatient.Tests
{
    public sealed class StoryProgressionTests
    {
        private GameObject root;
        private StoryManager story;
        private ChapterDefinition chapter;
        private readonly List<ScriptableObject> assets = new List<ScriptableObject>();

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Story test"); story = root.AddComponent<StoryManager>();
            chapter = ScriptableObject.CreateInstance<ChapterDefinition>(); assets.Add(chapter);
            chapter.id = StoryIds.Chapter;
            string[] ids = { StoryIds.Enter, StoryIds.Flashlight, StoryIds.Reception, StoryIds.Key, StoryIds.Tape, StoryIds.Ward };
            chapter.objectives = new ObjectiveDefinition[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                var objective = ScriptableObject.CreateInstance<ObjectiveDefinition>(); assets.Add(objective);
                objective.id = ids[i]; chapter.objectives[i] = objective;
            }
            story.chapter = chapter;
        }
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            foreach (var asset in assets) Object.DestroyImmediate(asset);
            assets.Clear();
        }

        [Test]
        public void FutureObjectivesCannotSkipTheReceptionPuzzle()
        {
            Assert.That(story.CompleteObjective(StoryIds.Key), Is.False);
            Assert.That(story.ActiveObjective.id, Is.EqualTo(StoryIds.Enter));
            Assert.That(story.State.completedObjectives, Is.Empty);
        }

        [Test]
        public void EntryEventSetsPrerequisitesBeforeCheckpointNotificationAndRunsOnlyOnce()
        {
            var entry = ScriptableObject.CreateInstance<StoryEventDefinition>(); assets.Add(entry);
            entry.id = "hospital_entry"; entry.once = true;
            entry.setFlags = new[] { StoryIds.Inside }; entry.completeObjectives = new[] { StoryIds.Enter };
            int eventCount = 0; story.EventRaised += _ => eventCount++;
            story.ObjectiveCompleted += _ => Assert.That(story.HasFlag(StoryIds.Inside), Is.True);
            Assert.That(story.Raise(entry), Is.True);
            Assert.That(story.Raise(entry), Is.False);
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(story.ActiveObjective.id, Is.EqualTo(StoryIds.Flashlight));
        }

        [Test]
        public void WardIsLockedUntilBothKeyAndTapeArePresent()
        {
            string[] required = { StoryIds.WardKey, StoryIds.TapeHeard };
            Assert.That(story.HasAll(required), Is.False);
            story.AddItem(StoryIds.WardKey);
            Assert.That(story.HasAll(required), Is.False);
            story.SetFlag(StoryIds.TapeHeard);
            Assert.That(story.HasAll(required), Is.True);
        }

        [Test]
        public void FullChapterCompletesAndJsonRoundTripKeepsStoryAndInventory()
        {
            story.SetFlag(StoryIds.Inside); story.CompleteObjective(StoryIds.Enter);
            story.AddItem(StoryIds.HasFlashlight); story.CompleteObjective(StoryIds.Flashlight);
            story.AddDocument(StoryIds.ReceptionFile); story.CompleteObjective(StoryIds.Reception);
            story.AddItem(StoryIds.WardKey); story.CompleteObjective(StoryIds.Key);
            story.AddDocument(StoryIds.FirstTape); story.SetFlag(StoryIds.TapeHeard); story.CompleteObjective(StoryIds.Tape);
            story.SetFlag(StoryIds.SliceComplete); story.CompleteObjective(StoryIds.Ward);
            story.State.position = new Vector3(0, 0, 32);
            var loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(story.State));
            Assert.That(SaveSystem.Validate(loaded, chapter, out var reason), Is.True, reason);
            story.Restore(loaded);
            Assert.That(story.ActiveObjective, Is.Null);
            Assert.That(story.State.currentObjective, Is.Empty);
            Assert.That(story.HasItem(StoryIds.WardKey), Is.True);
            Assert.That(story.State.documents, Contains.Item(StoryIds.FirstTape));
            Assert.That(story.HasFlag(StoryIds.SliceComplete), Is.True);
        }

        [Test]
        public void InventoryAndObjectivesAreIdempotent()
        {
            story.AddItem(StoryIds.WardKey); story.AddItem(StoryIds.WardKey);
            Assert.That(story.State.inventory.Count, Is.EqualTo(1));
            Assert.That(story.CompleteObjective(StoryIds.Enter), Is.True);
            Assert.That(story.CompleteObjective(StoryIds.Enter), Is.False);
            Assert.That(story.State.completedObjectives.Count, Is.EqualTo(1));
        }

        [Test]
        public void CorruptSaveWithMissingKeyCannotCreateALockedDoorSoftlock()
        {
            var data = new SaveData { completedObjectives = new List<string>
                { StoryIds.Enter, StoryIds.Flashlight, StoryIds.Reception, StoryIds.Key } };
            data.flags.Add(StoryIds.Inside); data.flags.Add(StoryIds.HasFlashlight); data.inventory.Add(StoryIds.HasFlashlight);
            Assert.That(SaveSystem.Validate(data, chapter, out _), Is.False);
        }

        [Test]
        public void OutOfOrderAndOutOfBoundsSavesAreRejected()
        {
            var data = new SaveData(); data.completedObjectives.Add(StoryIds.Tape);
            Assert.That(SaveSystem.Validate(data, chapter, out _), Is.False);
            data.completedObjectives.Clear(); data.position = new Vector3(0, -50, 5);
            Assert.That(SaveSystem.Validate(data, chapter, out _), Is.False);
            data.position = new Vector3(float.NaN, 0, 0);
            Assert.That(SaveSystem.Validate(data, chapter, out _), Is.False);
        }

        [Test]
        public void RestoreRecomputesCurrentObjectiveInsteadOfTrustingStaleSaveText()
        {
            var data = new SaveData { currentObjective = "stale.id" };
            story.Restore(data);
            Assert.That(story.State.currentObjective, Is.EqualTo(StoryIds.Enter));
        }

        [Test]
        public void StoryChoiceSurvivesJsonRoundTripWithoutDuplicateEntries()
        {
            var choice = new DialogueChoice { id = "choice.emily", flag = "flag.trust_emily" };
            story.Choose(choice); story.Choose(choice);
            story.Restore(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(story.State)));
            Assert.That(story.State.choices.Count, Is.EqualTo(1));
            Assert.That(story.HasFlag("flag.trust_emily"), Is.True);
        }
    }
}
