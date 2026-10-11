using System;
using UnityEngine;

namespace TheLastPatient
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GameDirector : MonoBehaviour
    {
        public static GameDirector Instance { get; private set; }
        public StoryManager story;
        public MissionManager missions;
        public DialogueManager dialogue;
        public CutsceneManager cutscenes;
        public FirstPersonController player;
        public PlayerInputReader input;
        public FlashlightController flashlight;
        public GameUI ui;
        public HospitalAmbience ambience;
        public StoryEventDefinition introEvent;
        public Vector3 startPosition = new Vector3(0, 0.1f, -12);
        public event Action StateRestored;
        private bool checkpointPending;
        public bool CanControl => !ui.IsModal && !cutscenes.IsPlaying && !dialogue.IsPlaying && Time.timeScale > 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.targetFrameRate = 60;
            Time.timeScale = 1;
        }
        private void Start()
        {
            story.ObjectiveCompleted += OnObjectiveCompleted;
            story.EventRaised += OnEvent;
            UpdateCursor();
            story.Raise(introEvent);
        }
        private void Update()
        {
            if (input.Pause)
            {
                if (cutscenes.IsPlaying) cutscenes.Skip();
                else if (ui.IsModal && !ui.IsEnding && !dialogue.IsPlaying) ui.CloseModal();
                else if (!ui.IsModal && !dialogue.IsPlaying) ui.ShowPause();
            }
            if (input.Journal && !cutscenes.IsPlaying && !dialogue.IsPlaying && !ui.IsEnding)
            { if (ui.IsModal) ui.CloseModal(); else ui.ShowJournal(); }
            if (input.Save && CanControl) Save();
            if (input.Load && !cutscenes.IsPlaying && !dialogue.IsPlaying) Load();
            if (checkpointPending && CanControl) { checkpointPending = false; Save(false); }
            UpdateCursor();
        }
        private void OnObjectiveCompleted(string id) => checkpointPending = true;
        private void OnEvent(StoryEventDefinition definition)
        {
            if (!string.IsNullOrEmpty(definition.cutsceneId)) cutscenes.Play(definition.cutsceneId);
            if (definition.dialogue != null) dialogue.Play(definition.dialogue);
        }
        public void UpdateCursor()
        {
            bool free = ui != null && ui.IsModal;
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }
        public void NewGame()
        {
            cutscenes.Cancel(); dialogue.Cancel(); ui.ResetPanels();
            story.Restore(new SaveData()); player.Teleport(startPosition, 0); flashlight.Restore(100, false);
            ambience.ApplyState(); StateRestored?.Invoke(); checkpointPending = false;
            story.Raise(introEvent);
        }
        public void Save(bool notify = true)
        {
            try
            {
                var state = story.State;
                state.position = player.transform.position;
                state.yaw = player.transform.eulerAngles.y; state.pitch = player.Pitch;
                state.battery = flashlight.Battery; state.flashlightOn = flashlight.IsOn;
                SaveSystem.Write(state);
                if (notify) ui.Toast("Spielstand gespeichert.");
            }
            catch (Exception error) when (error is System.IO.IOException || error is UnauthorizedAccessException)
            { ui.Toast("Speichern fehlgeschlagen. Prüfe den Speicherordner."); Debug.LogException(error); }
        }
        public bool Load()
        {
            try
            {
                var state = SaveSystem.Read(story.chapter);
                cutscenes.Cancel(); dialogue.Cancel(); ui.ResetPanels();
                story.Restore(state); player.Teleport(state.position, state.yaw, state.pitch);
                flashlight.Restore(state.battery, state.flashlightOn);
                ambience.ApplyState(); StateRestored?.Invoke(); checkpointPending = false;
                if (story.HasFlag(StoryIds.SliceComplete)) ui.ShowEnding();
                else ui.Toast("Checkpoint geladen.");
                return true;
            }
            catch (Exception error) when (error is System.IO.IOException || error is UnauthorizedAccessException || error is ArgumentException)
            { ui.Toast(error.Message); Debug.LogWarning("Checkpoint konnte nicht geladen werden: " + error.Message); return false; }
        }
        public void RescuePlayer()
        {
            if (SaveSystem.Exists && Load()) return;
            player.Teleport(story.HasFlag(StoryIds.Inside) ? new Vector3(0, 0.1f, 4) : startPosition, 0);
            ui.Toast("Du wurdest an einen sicheren Punkt zurückgesetzt.");
        }
        private void OnApplicationQuit() { Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; }
        private void OnDestroy()
        {
            if (Instance != this) return;
            if (story != null) { story.ObjectiveCompleted -= OnObjectiveCompleted; story.EventRaised -= OnEvent; }
            Instance = null; Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
    }
}
