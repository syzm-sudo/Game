using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLastPatient
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private InputActionMap actions;
        private InputAction move, look, sprint, crouch, interact, flashlight, pause, skip, journal, save, load;
        public Vector2 Move => move.ReadValue<Vector2>();
        public Vector2 Look => look.ReadValue<Vector2>();
        public bool Sprint => sprint.IsPressed();
        public bool Crouch => crouch.IsPressed();
        public bool Interact => interact.WasPressedThisFrame();
        public bool Flashlight => flashlight.WasPressedThisFrame();
        public bool Pause => pause.WasPressedThisFrame();
        public bool Skip => skip.WasPressedThisFrame();
        public bool Journal => journal.WasPressedThisFrame();
        public bool Save => save.WasPressedThisFrame();
        public bool Load => load.WasPressedThisFrame();

        private void Awake()
        {
            actions = new InputActionMap("Hospital");
            move = actions.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            look = actions.AddAction("Look", InputActionType.Value, "<Mouse>/delta");
            sprint = Button("Sprint", "<Keyboard>/leftShift");
            crouch = Button("Crouch", "<Keyboard>/leftCtrl");
            interact = Button("Interact", "<Keyboard>/e");
            flashlight = Button("Flashlight", "<Keyboard>/f");
            pause = Button("Pause", "<Keyboard>/escape");
            skip = Button("Skip", "<Keyboard>/space");
            journal = Button("Journal", "<Keyboard>/j");
            save = Button("Save", "<Keyboard>/f5");
            load = Button("Load", "<Keyboard>/f9");
        }
        private InputAction Button(string name, string binding) => actions.AddAction(name, InputActionType.Button, binding);
        private void OnEnable() => actions?.Enable();
        private void OnDisable() => actions?.Disable();
        private void OnDestroy() => actions?.Dispose();
    }
}
