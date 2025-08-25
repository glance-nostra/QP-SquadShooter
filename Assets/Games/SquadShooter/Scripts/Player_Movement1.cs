using UnityEngine;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class Player_Movement1 : MonoBehaviour
    {
        public enum MovementType { Rigidbody, Translate, CharacterController }
        public MovementType movementType = MovementType.Rigidbody;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 10f;

        [Header("Joystick Input Settings")]
        [SerializeField] private Joystick playerJoystick;

        [Header("Animations Controller")]
        public Animator playerAnimator;

        [Header("Game Manager")]
        public GameController GameManager;

        [Header("Current player state")]
        public AnimState playerState;

        private Player_Manager player;
        private Vector3 movementDirection;
        private Vector3 currentVelocity;
        private Rigidbody rb;
        private CharacterController characterController;

        void Start()
        {
            player = GetComponent<Player_Manager>();
            rb = GetComponent<Rigidbody>();
            characterController = GetComponent<CharacterController>();

            if (rb != null)
            {
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
        }

        void Update()
        {
            if (!GameManager.GamePlay || player.is_death)
            {
                return;
            }

            float horizontalInput = playerJoystick.Horizontal;
            float verticalInput = playerJoystick.Vertical;
            Vector3 targetDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;

            movementDirection = Vector3.Lerp(movementDirection, targetDirection, Time.deltaTime * acceleration);

            if (movementDirection.magnitude < 0.1f)
            {
                movementDirection = Vector3.zero;
            }

            UpdateAnimation(horizontalInput, verticalInput);
        }

        void FixedUpdate()
        {
            if (!GameManager.GamePlay || player.is_death)
            {
                movementDirection = Vector3.zero;
                return;
            }

            switch (movementType)
            {
                case MovementType.Rigidbody:
                    MoveWithRigidbody();
                    break;
                case MovementType.Translate:
                    MoveWithTranslate();
                    break;
                case MovementType.CharacterController:
                    MoveWithCharacterController();
                    break;
            }
        }

        void MoveWithRigidbody()
        {
            Vector3 targetVelocity = movementDirection * moveSpeed;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * acceleration);
            RotatePlayer();
        }

        void MoveWithTranslate()
        {
            transform.position = Vector3.Lerp(transform.position, transform.position + movementDirection * moveSpeed * Time.fixedDeltaTime, Time.fixedDeltaTime * acceleration);
            RotatePlayer();
        }

        void MoveWithCharacterController()
        {
            if (characterController != null)
            {
                Vector3 smoothedMovement = Vector3.Lerp(characterController.velocity, movementDirection * moveSpeed, Time.fixedDeltaTime * acceleration);
                characterController.Move(smoothedMovement * Time.fixedDeltaTime);
                RotatePlayer();
            }
        }

        void RotatePlayer()
        {
            if (movementDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movementDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
            }
        }

        void UpdateAnimation(float horizontal, float vertical)
        {
            if (movementDirection.magnitude > 0.1f)
            {
                AnimationController(AnimState.RunningForward);
            }
            else
            {
                AnimationController(AnimState.Idle);
            }
        }

        public void AnimationController(AnimState newState)
        {
            if (playerState == newState) return;

            playerState = newState;
            playerAnimator.SetBool("Idle", newState == AnimState.Idle);
            playerAnimator.SetBool("Running", newState == AnimState.RunningForward);
        }

        public enum AnimState
        {
            Idle,
            RunningForward
        }
    }
}