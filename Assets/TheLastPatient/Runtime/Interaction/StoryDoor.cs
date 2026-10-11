using System.Collections;
using UnityEngine;

namespace TheLastPatient
{
    public sealed class StoryDoor : Interactable
    {
        public string doorId;
        public Transform hinge;
        public float openAngle = -95;
        public bool lockAfterEntry;
        public StoryEventDefinition openEvent;
        private bool isOpen;
        private Coroutine motion;
        public override string Prompt => lockAfterEntry && GameDirector.Instance.story.HasFlag(StoryIds.Inside)
            ? "Eingang verriegelt" : isOpen ? "Tür schließen" : label;
        private void Start() { ApplyState(); GameDirector.Instance.StateRestored += ApplyState; }
        private void OnDestroy() { if (GameDirector.Instance != null) GameDirector.Instance.StateRestored -= ApplyState; }
        private void ApplyState()
        {
            if (motion != null) StopCoroutine(motion);
            motion = null;
            isOpen = GameDirector.Instance.story.HasFlag("door." + doorId + ".open");
            if (lockAfterEntry && GameDirector.Instance.story.HasFlag(StoryIds.Inside)) isOpen = false;
            hinge.localRotation = Quaternion.Euler(0, isOpen ? openAngle : 0, 0);
        }
        public void SlamAndLock()
        {
            GameDirector.Instance.story.SetFlag("door." + doorId + ".open", false);
            SetOpen(false);
        }
        public override void Interact()
        {
            var game = GameDirector.Instance;
            if (lockAfterEntry && game.story.HasFlag(StoryIds.Inside)) { game.ui.Toast("Die Tür bewegt sich keinen Millimeter."); return; }
            if (!CanInteract()) return;
            bool opening = !isOpen;
            game.story.SetFlag("door." + doorId + ".open", opening);
            SetOpen(opening);
            if (opening && openEvent != null) game.story.Raise(openEvent);
        }
        private void SetOpen(bool value)
        {
            isOpen = value;
            if (motion != null) StopCoroutine(motion);
            motion = StartCoroutine(Animate(value));
            GameDirector.Instance.ambience.DoorSound();
        }
        private IEnumerator Animate(bool open)
        {
            var initial = hinge.localRotation;
            var target = Quaternion.Euler(0, open ? openAngle : 0, 0);
            for (float t = 0; t < 1; t += Time.deltaTime * 1.7f)
            { hinge.localRotation = Quaternion.Slerp(initial, target, Mathf.SmoothStep(0, 1, t)); yield return null; }
            hinge.localRotation = target;
            motion = null;
        }
    }
}
