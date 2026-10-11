using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheLastPatient
{
    public sealed class DialogueManager : MonoBehaviour
    {
        public GameUI ui;
        public PlayerInputReader input;
        public AudioSource voice;
        public bool IsPlaying { get; private set; }
        private readonly Queue<(DialogueDefinition definition, Action done)> queue = new Queue<(DialogueDefinition, Action)>();
        private Coroutine running;

        public void Play(DialogueDefinition definition, Action onComplete = null)
        {
            if (definition == null || definition.lines == null) { onComplete?.Invoke(); return; }
            queue.Enqueue((definition, onComplete));
            if (running == null) running = StartCoroutine(RunQueue());
        }
        private IEnumerator RunQueue()
        {
            IsPlaying = true;
            // Ensure the coroutine handle has been assigned even for an empty dialogue.
            yield return null;
            while (queue.Count > 0)
            {
                var entry = queue.Dequeue();
                foreach (var line in entry.definition.lines)
                {
                    if (line == null) continue;
                    string text = line.text ?? string.Empty;
                    ui.ShowLine(line.speaker, text);
                    if (voice != null && line.audio != null) { voice.clip = line.audio; voice.Play(); }
                    float visible = 0;
                    while (visible < text.Length)
                    {
                        if (input.Skip) { visible = text.Length; ui.Reveal(text.Length); yield return null; break; }
                        visible += Time.unscaledDeltaTime * 34;
                        ui.Reveal(Mathf.FloorToInt(visible)); yield return null;
                    }
                    ui.Reveal(int.MaxValue);
                    float hold = Mathf.Max(line.holdSeconds, line.audio != null ? line.audio.length - text.Length / 34f : 0);
                    for (float elapsed = 0; elapsed < hold; elapsed += Time.unscaledDeltaTime)
                    { if (input.Skip) break; yield return null; }
                    if (voice != null) voice.Stop();
                    yield return null;
                }
                if (entry.definition.choices != null && entry.definition.choices.Length > 0)
                {
                    bool chosen = false;
                    ui.ShowChoices(entry.definition.choices, choice => { GameDirector.Instance.story.Choose(choice); chosen = true; });
                    while (!chosen) yield return null;
                }
                entry.done?.Invoke();
            }
            ui.HideDialogue(); IsPlaying = false; running = null;
        }
        public void Cancel()
        {
            if (running != null) StopCoroutine(running);
            running = null; queue.Clear(); IsPlaying = false;
            if (voice != null) voice.Stop();
            ui.HideDialogue();
        }
    }
}
