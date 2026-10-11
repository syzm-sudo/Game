using System.Collections;
using UnityEngine;

namespace TheLastPatient
{
    public sealed class CutsceneManager : MonoBehaviour
    {
        public DialogueDefinition intro;
        public DialogueDefinition blackout;
        public DialogueDefinition watcherDialogue;
        public DialogueDefinition ending;
        public GameObject watcher;
        public StoryDoor entrance;
        public bool IsPlaying { get; private set; }
        private bool skipRequested;
        private Coroutine running;
        private Quaternion savedCameraRotation;
        private Quaternion savedPlayerRotation;

        public void Play(string id)
        {
            if (IsPlaying || string.IsNullOrEmpty(id)) return;
            if (id != "intro" && id != "blackout" && id != "watcher" && id != "chapter_end") return;
            skipRequested = false;
            var player = GameDirector.Instance.player;
            savedCameraRotation = player.cameraPivot.localRotation;
            savedPlayerRotation = player.transform.rotation;
            running = StartCoroutine(Run(id));
        }
        public void Skip() { skipRequested = true; GameDirector.Instance.dialogue.Cancel(); }
        private IEnumerator Wait(float seconds)
        {
            for (float t = 0; t < seconds && !skipRequested; t += Time.unscaledDeltaTime) yield return null;
        }
        private IEnumerator Talk(DialogueDefinition definition)
        {
            if (skipRequested) yield break;
            var manager = GameDirector.Instance.dialogue;
            manager.Play(definition);
            while (manager.IsPlaying && !skipRequested) yield return null;
        }
        private IEnumerator Run(string id)
        {
            IsPlaying = true;
            var game = GameDirector.Instance;
            game.ui.Toast("Esc · Sequenz überspringen");
            if (id == "intro")
            {
                game.player.transform.rotation = Quaternion.Euler(0, -18, 0);
                for (float t = 0; t < 1 && !skipRequested; t += Time.unscaledDeltaTime / 3)
                {
                    game.player.transform.rotation = Quaternion.Euler(0, Mathf.Lerp(-18, 0, Mathf.SmoothStep(0, 1, t)), 0);
                    yield return null;
                }
                yield return Talk(intro);
            }
            else if (id == "blackout")
            {
                entrance.SlamAndLock();
                for (int i = 0; i < 5 && !skipRequested; i++)
                { game.ambience.SetHospitalLights(i % 2 == 0); yield return Wait(0.16f); }
                game.ambience.SetHospitalLights(false);
                yield return Talk(blackout);
            }
            else if (id == "watcher")
            {
                watcher.SetActive(true);
                game.player.transform.rotation = Quaternion.Euler(0, 0, 0);
                game.player.cameraPivot.localRotation = Quaternion.identity;
                game.ambience.DoorSound();
                yield return Wait(2);
                watcher.SetActive(false);
                yield return Talk(watcherDialogue);
            }
            else if (id == "chapter_end")
            {
                game.player.transform.rotation = Quaternion.Euler(0, 0, 0);
                yield return Talk(ending);
                yield return Wait(1);
            }
            Finish(id);
        }
        private void Finish(string id)
        {
            var game = GameDirector.Instance;
            watcher.SetActive(false);
            game.player.cameraPivot.localRotation = savedCameraRotation;
            game.player.transform.rotation = savedPlayerRotation;
            game.ambience.ApplyState();
            if (id == "intro") game.story.SetFlag(StoryIds.IntroSeen);
            IsPlaying = false; running = null;
            if (id == "chapter_end") { game.Save(false); game.ui.ShowEnding(); }
            else game.ui.Toast(id == "intro" ? "Finde einen Weg in das Krankenhaus. Interagiere mit der Eingangstür [E]." : "[J] Journal · [F5] Speichern · [F9] Laden");
        }
        public void Cancel()
        {
            if (running != null) StopCoroutine(running);
            if (IsPlaying)
            {
                var player = GameDirector.Instance.player;
                player.cameraPivot.localRotation = savedCameraRotation;
                player.transform.rotation = savedPlayerRotation;
            }
            watcher.SetActive(false);
            running = null; IsPlaying = false; skipRequested = false;
        }
    }
}
