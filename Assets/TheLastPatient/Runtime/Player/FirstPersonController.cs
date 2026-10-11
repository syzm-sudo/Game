using UnityEngine;

namespace TheLastPatient
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        public PlayerInputReader input;
        public Transform cameraPivot;
        public float walkSpeed = 3.1f;
        public float sprintSpeed = 5.1f;
        public float crouchSpeed = 1.5f;
        public float sensitivity = 0.1f;
        public float Pitch { get; private set; }
        public bool IsCrouching { get; private set; }
        private CharacterController controller;
        private float falling;
        private float footsteps;

        private void Awake() => controller = GetComponent<CharacterController>();
        private void Update()
        {
            var game = GameDirector.Instance;
            if (game == null || input == null || cameraPivot == null || !game.CanControl) return;
            var look = input.Look;
            transform.Rotate(0, look.x * sensitivity, 0);
            Pitch = Mathf.Clamp(Pitch - look.y * sensitivity, -80, 80);
            cameraPivot.localRotation = Quaternion.Euler(Pitch, 0, 0);
            bool wantsCrouch = input.Crouch;
            // Keep the player crouched if a ceiling obstructs standing up.
            if (!wantsCrouch && IsCrouching)
                wantsCrouch = Physics.CheckSphere(transform.position + Vector3.up * 1.68f, 0.25f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            IsCrouching = wantsCrouch;
            controller.height = IsCrouching ? 1.05f : 1.8f;
            controller.center = Vector3.up * controller.height * 0.5f;
            cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition,
                Vector3.up * (IsCrouching ? 0.88f : 1.62f), Time.deltaTime * 12);
            float speed = IsCrouching ? crouchSpeed : input.Sprint ? sprintSpeed : walkSpeed;
            var movement = Vector2.ClampMagnitude(input.Move, 1);
            if (controller.isGrounded && falling < 0) falling = -2;
            falling += Physics.gravity.y * Time.deltaTime;
            controller.Move((transform.forward * movement.y * speed + transform.right * movement.x * speed
                + Vector3.up * falling) * Time.deltaTime);
            if (controller.isGrounded && movement.sqrMagnitude > 0.05f)
            {
                footsteps -= Time.deltaTime;
                if (footsteps <= 0) { game.ambience.Footstep(IsCrouching); footsteps = input.Sprint && !IsCrouching ? 0.32f : 0.52f; }
            }
            if (transform.position.y < -5) game.RescuePlayer();
        }

        public void Teleport(Vector3 position, float yaw, float pitch = 0)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            Pitch = Mathf.Clamp(pitch, -80, 80);
            cameraPivot.localRotation = Quaternion.Euler(Pitch, 0, 0);
            controller.enabled = true;
            falling = 0;
        }
    }
}
