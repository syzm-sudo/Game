using System.IO;
using System.Linq;
using NUnit.Framework;
using TheLastPatient.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheLastPatient.Tests
{
    public sealed class GeneratedSceneTests
    {
        private Scene scene;
        private bool opened;
        private GameDirector game;

        [SetUp]
        public void OpenGeneratedScene()
        {
            Assert.That(File.Exists(PrototypeGenerator.ScenePath), Is.True,
                "Zuerst Tools > The Last Patient > Generate Game Prototype ausführen.");
            scene = SceneManager.GetSceneByPath(PrototypeGenerator.ScenePath);
            opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(PrototypeGenerator.ScenePath, OpenSceneMode.Additive);
            game = Components<GameDirector>().Single();
            Physics.SyncTransforms();
        }
        [TearDown]
        public void CloseGeneratedScene()
        { if (opened && scene.isLoaded) EditorSceneManager.CloseScene(scene, true); }
        private T[] Components<T>() where T : Component => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        [Test]
        public void AllRuntimeSystemsHaveTheirInspectorReferencesAssigned()
        {
            Assert.That(game.story, Is.Not.Null); Assert.That(game.player, Is.Not.Null);
            Assert.That(game.ui, Is.Not.Null); Assert.That(game.input, Is.Not.Null);
            Assert.That(game.dialogue.ui, Is.SameAs(game.ui));
            Assert.That(game.missions.story, Is.SameAs(game.story));
            Assert.That(game.missions.ui, Is.SameAs(game.ui));
            Assert.That(game.player.cameraPivot, Is.Not.Null); Assert.That(game.flashlight.beam, Is.Not.Null);
            Assert.That(game.cutscenes.entrance, Is.Not.Null); Assert.That(game.cutscenes.watcher, Is.Not.Null);
            Assert.That(game.cutscenes.intro, Is.Not.Null); Assert.That(game.cutscenes.blackout, Is.Not.Null);
            Assert.That(game.introEvent, Is.Not.Null); Assert.That(game.introEvent.cutsceneId, Is.EqualTo("intro"));
            Assert.That(game.cutscenes.watcherDialogue, Is.Not.Null); Assert.That(game.cutscenes.ending, Is.Not.Null);
            Assert.That(game.dialogue.voice, Is.Not.Null); Assert.That(game.ambience.effects, Is.Not.Null);
            Assert.That(game.ambience.hospitalLights.Length, Is.GreaterThan(0));
            Assert.That(game.story.chapter.objectives.Length, Is.EqualTo(6));
            Assert.That(game.story.chapter.objectives.Select(o => o.id).Distinct().Count(), Is.EqualTo(6));
        }

        [Test]
        public void FlashlightFileAndRecorderCanBeReachedWithTheActualInteractionRaycast()
        {
            var flashlight = Components<PickupItem>().Single(item => !item.battery);
            var file = Components<DocumentItem>().Single();
            var tape = Components<TapePlayer>().Single();
            AssertReachable(flashlight, new Vector3(0, 1.62f, 3.22f));
            AssertReachable(file, new Vector3(0, 1.62f, 7.1f));
            AssertReachable(tape, new Vector3(0, 1.62f, 8.85f));
        }
        private static void AssertReachable(Interactable target, Vector3 eye)
        {
            var direction = target.transform.position - eye;
            Assert.That(Physics.Raycast(eye, direction.normalized, out var hit, 2.7f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore), Is.True, target.name);
            Assert.That(hit.collider.GetComponentInParent<Interactable>(), Is.SameAs(target),
                "Ein anderes Objekt blockiert die Interaktion mit " + target.name);
        }

        [Test]
        public void WardDoorAndFinalTriggerRequireKeyAndCompletedTape()
        {
            var ward = Components<StoryDoor>().Single(door => door.doorId == "ward");
            Assert.That(ward.requiredFlags, Is.EquivalentTo(new[] { StoryIds.WardKey, StoryIds.TapeHeard }));
            var end = Components<StoryTrigger>().Single(trigger => trigger.storyEvent.id == "chapter01_end");
            Assert.That(end.storyEvent.requiredFlags, Is.EquivalentTo(ward.requiredFlags));
            Assert.That(end.storyEvent.completeObjectives, Contains.Item(StoryIds.Ward));
            Assert.That(Components<StoryDoor>().Single(door => door.doorId == "entrance").lockAfterEntry, Is.True);
        }

        [Test]
        public void PrefabsAndSceneAreRegisteredForAStandaloneBuild()
        {
            foreach (var name in new[] { "Player", "Watcher", "EntranceDoor", "WardDoor", "FlashlightPickup", "PatientFile", "TapeRecorder" })
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(PrototypeGenerator.Root + "/Prefabs/" + name + ".prefab"), Is.Not.Null, name);
            Assert.That(EditorBuildSettings.scenes.Any(item => item.enabled && item.path == PrototypeGenerator.ScenePath), Is.True);
        }
    }
}
