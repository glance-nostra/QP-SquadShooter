using UnityEngine;
using Fusion;
using DG.Tweening.Core.Easing;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class Player_Movement : NetworkBehaviour
    {
        public enum MovementType { Rigidbody, Translate, CharacterController }
        public MovementType movementType = MovementType.Rigidbody;

        [Header("Movement Settings")]
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 10f;

        [Header("Animations Controller")]
        public Animator playerAnimator;

        [Header("Game Manager")]
        public GameController gameManager;

        [Header("Current player state")]
        public AnimState playerState;

        private Player_Manager player;
        private Vector3 movementDirection;
        private Rigidbody rb;
        private float walksoundtime;

        private void OnDestroy()
        {
            gameManager.allcharacter.Remove(this.gameObject.GetComponent<Entity>());
        }
        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            gameManager = GameController.instace;
            gameManager.allcharacter.Add(this.gameObject.GetComponent<Entity>());
        }
        public override void Spawned()
        {
            rb = GetComponent<Rigidbody>();
            player = GetComponent<Player_Manager>();
            gameManager = GameController.instace;

            if (Object.HasInputAuthority)
            {
                Camera_Follower cam = Camera.main.GetComponent<Camera_Follower>();
                if (cam != null)
                {
                    cam.SetTarget(this.transform); // Only local player sets the camera
                }
            }
        }


        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || !gameManager.GamePlay || player.is_death)
            {
                movementDirection = Vector3.zero;
                return;
            }

            if (GetInput(out PlayerInputData inputData))
            {
                //Debug.Log($"[{Object.InputAuthority}] Input: {inputData.Horizontal}, {inputData.Vertical}");

                Vector3 targetDirection = new Vector3(inputData.Horizontal, 0, inputData.Vertical).normalized;
                movementDirection = Vector3.Lerp(movementDirection, targetDirection, Runner.DeltaTime * acceleration);

                if (movementDirection.magnitude < 0.1f)
                {
                    movementDirection = Vector3.zero;
                }
                else
                {
                    if (rb.linearVelocity != Vector3.zero && walksoundtime > 0.25f)
                    {
                        walksoundtime = 0;
                        player.playerAudio.PlayOneShot(player.runSurface);
                    }
                    walksoundtime += Runner.DeltaTime;
                }

                switch (movementType)
                {
                    case MovementType.Rigidbody:
                        MoveWithRigidbody();
                        break;
                }
                if (gameManager.joystickShoot.Direction.magnitude == 0)
                {
                    RotatePlayer();
                }
                UpdateAnimation();
            }
        }

        void MoveWithRigidbody()
        {
            Vector3 targetVelocity = movementDirection * player.moveSpeed;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Runner.DeltaTime * acceleration);
        }

        void RotatePlayer()
        {
            if (movementDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movementDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * player.rotationSpeed);
            }
        }

        void UpdateAnimation()
        {
            if (!player.Enemy)
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
            else
            {
                Vector3 relativeMovement = transform.InverseTransformDirection(movementDirection);
                if (relativeMovement.z > 0.1f)
                    AnimationController(AnimState.RunningForward);
                else if (relativeMovement.z < -0.1f)
                    AnimationController(AnimState.RunningBackward);
                else if (relativeMovement.x > 0.1f)
                    AnimationController(AnimState.RunningRight);
                else if (relativeMovement.x < -0.1f)
                    AnimationController(AnimState.RunningLeft);
                else
                    AnimationController(AnimState.Idle);
            }
        }

        public void AnimationController(AnimState newState)
        {
            if (playerState == newState) return;

            playerState = newState;
            playerAnimator.SetBool("Idle", newState == AnimState.Idle);
            playerAnimator.SetBool("Running", newState == AnimState.RunningForward);
            playerAnimator.SetBool("Right", newState == AnimState.RunningRight);
            playerAnimator.SetBool("Backward", newState == AnimState.RunningBackward);
            playerAnimator.SetBool("Left", newState == AnimState.RunningLeft);
        }
    }

    public enum AnimState
    {
        Idle,
        RunningForward,
        RunningBackward,
        RunningLeft,
        RunningRight
    }
}