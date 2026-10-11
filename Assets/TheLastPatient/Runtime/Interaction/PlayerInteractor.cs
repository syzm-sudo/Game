using UnityEngine;

namespace TheLastPatient
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        public Camera view;
        public PlayerInputReader input;
        public float reach = 2.7f;
        private void Update()
        {
            var game = GameDirector.Instance;
            if (game == null || view == null) return;
            Interactable target = null;
            if (game.CanControl && Physics.Raycast(view.transform.position, view.transform.forward, out var hit,
                reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                target = hit.collider.GetComponentInParent<Interactable>();
            game.ui.SetInteraction(target != null && target.Available ? "[E] " + target.Prompt : string.Empty);
            if (game.CanControl && target != null && target.Available && input.Interact) target.Interact();
        }
    }
}
