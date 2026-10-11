using UnityEngine;

namespace TheLastPatient
{
    public sealed class FlashlightController : MonoBehaviour
    {
        public Light beam;
        public PlayerInputReader input;
        public float Battery { get; private set; } = 100;
        public bool IsOn { get; private set; }
        private void Update()
        {
            var game = GameDirector.Instance;
            if (game == null || beam == null) return;
            bool owned = game.story.HasItem(StoryIds.HasFlashlight);
            if (game.CanControl && input.Flashlight)
            {
                if (!owned) game.ui.Toast("Finde zuerst eine Taschenlampe im Empfangsbereich.");
                else IsOn = !IsOn;
            }
            if (game.CanControl && owned)
            {
                // Emergency dynamo trickle prevents darkness becoming an unwinnable state.
                Battery = Mathf.Clamp(Battery + (IsOn ? -0.22f : 0.8f) * Time.deltaTime, 0, 100);
                if (Battery <= 0) { IsOn = false; game.ui.Toast("Batterie leer. Im ausgeschalteten Zustand lädt der Not-Dynamo nach."); }
            }
            beam.enabled = owned && IsOn;
            game.ui.SetBattery(owned, Battery, IsOn);
        }
        public void Restore(float battery, bool on) { Battery = Mathf.Clamp(battery, 0, 100); IsOn = on; }
        public void Refill() { Battery = 100; GameDirector.Instance.ui.Toast("Batterien eingesetzt · 100 %"); }
    }
}
