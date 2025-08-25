using DG.Tweening.Core.Easing;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public partial class Player_Shooting : NetworkBehaviour
    {

        [Space(10)]
        [Header("Player Manager")]
        public Player_Manager PlayerManager; // Accedd of player manager
        public FixedJoystick Shottingjoystick;

        [Space(10)]
        [Header("All shooting and reloading variables")]
        [SerializeField] private Transform FirePoint; // Starting point for bullet
        [SerializeField] private float bulletSpeed; // Bullet speed
        [SerializeField] private Button FireButton; // Fire button
        [SerializeField] private Image FireReloadingImage; // Fire realoding image
        [SerializeField] private float intervalTime; // Interval timing for next shoot
                                                     // Find that gun is in interval or not
        [SerializeField] private ParticleSystem ShootParticle;
        [SerializeField] private GameObject shootingdirection;

        [Space(10)]
        [Header("Damage variables")]
        public int hitDamage = 5; // Bot damage amount

        public float shakeDuration = 0.2f;
        public float shakeMagnitude = 0.2f;
        private Vector3 originalPosition;

        private Vector2 shootDirection;
        private bool shootPressed;

        private bool gameStarted = false;


        //private void Start()
        //{
        //    GameManager = GameObject.Find("Game Manager").GetComponent<GameManager>();

        //    PlayerManager = GetComponent<Player_Manager>();

        //    Shottingjoystick.entity = this;
        //}
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void Rpc_AnnouncePlayer(string name)
        {


            PlayerManager.gameManager = GameController.instace;

            Shottingjoystick = GameController.instace.joystickShoot;
            if (Object.HasInputAuthority)
                GameController.instace.joystickShoot.entity = this;

            GameController.instace.BotCount();
        }
        public override void Spawned()
        {





        }

        public void StartActualGame()
        {
            gameStarted = true;
            Debug.Log("All players joined. Starting Game!");

            GameController.instace.StartGame(); // 👈 Your GameManager handles gameplay logic
        }


        // Update
        void Update()
        {
            if (GameController.instace.GamePlay == false || PlayerManager.is_death)
            {
                return;
            }
            //DirectionShoot();
            //Debug.DrawRay(transform.position, transform.forward * 100, Color.green);

            //DrawT                                                                    rajectory();
        }

        //public override void FixedUpdateNetwork()
        //{
        //    if (GetInput<PlayerInputData>(out var input))
        //    {
        //        shootDirection = input.ShootDirection;
        //        shootPressed = input.ShootPressed;
        //    }

        //    //// Convert to world direction if needed:
        //    //Vector3 worldShootDir = new Vector3(shootDirection.x, 0, shootDirection.y);

        //    if (shootDirection.magnitude > 0.2f)
        //    {
        //        shootingdirection.SetActive(true);
        //        float angle = Mathf.Atan2(shootDirection.x, shootDirection.y) * Mathf.Rad2Deg;
        //        transform.rotation = Quaternion.Euler(0, angle, 0);
        //    }
        //    else
        //    {
        //        shootingdirection.SetActive(false);
        //    }

        //    if (shootPressed)
        //    {
        //        Shoot();
        //    }
        //}

        private bool wasShootingLastTick = false;


        public override void FixedUpdateNetwork()
        {
            if (!HasInputAuthority || PlayerManager.is_death || PlayerManager.isInInterval)
                return;

            if (GetInput<PlayerInputData>(out var input))
            {
                shootDirection = input.ShootDirection;
                shootPressed = input.ShootPressed;
            }

            // Only rotate and show direction if joystick is active enough
            if (shootDirection.magnitude > 0.01f)
            {
                //shootingdirection.SetActive(true);
                Debug.Log("Shooting");
                float angle = Mathf.Atan2(shootDirection.x, shootDirection.y) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, angle, 0);
                if (shootPressed && !PlayerManager.isInInterval && !PlayerManager.is_death)
                {
                    Shoot();
                }

            }
            else
            {
                //shootingdirection.SetActive(false);
            }


        }


        private bool shooting, shooted;



        // Bullet shoot
        public void Shoot()
        {
            if (GameController.instace.GamePlay == false ||
                PlayerManager.isInInterval ||
                PlayerManager.is_death == true ||
                PlayerManager.gameManager.redyforBrustShooting)
            {
                return;
            }



            // ACTUAL SHOOTING LOGIC HERE
            if (ShootParticle != null)
                ShootParticle.Play();



            PlayerManager.player_Movement.playerAnimator.SetBool("Shoot_Idle", true);
            Debug.Log($"[{Object.InputAuthority}] SHOOT!" + PlayerManager.player_Movement.playerAnimator.GetBool("Shoot_Idle") + gameObject.name);
            PlayerManager.isInInterval = true;
            PlayerManager.player_Movement.playerAnimator.SetFloat("Shooting Speed", PlayerManager.allCollectedWepon[0].firerate * 10);
            Invoke("ResetShooting", .1f); // Adjust timing based on animation length
                                          // Add bullet or damage logic here (raycast or projectile spawn)


        }


        void ResetShooting()
        {
            PlayerManager.player_Movement.playerAnimator.SetBool("Shoot_Idle", false);
            PlayerManager.isInInterval = false;
        }

        // Draw laser aim
        public void DrawTrajectory()
        {

            // Calculate the forward direction (XZ plane)
            Vector3 forwardDirection = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            // Starting point of the trajectory
            Vector3 startPoint = transform.position;
            startPoint.y = .75f;
            // Check for collisions using a raycast
            RaycastHit hit;
            Vector3 endPoint;


            if (Physics.Raycast(startPoint, forwardDirection, out hit))
            {
                // Collision detected
                endPoint = hit.point;
            }
            else
            {
                // No collision
                endPoint = startPoint + transform.forward * 100;
            }
            // Apply points to trajectoryLine
            // Laser.positionCount = 2; // Start and end points only
            //    Laser.SetPosition(0, new Vector3(startPoint.x, .75f, startPoint.z)); // Start point
            //  Laser.SetPosition(1, new Vector3(endPoint.x, .75f, endPoint.z)); // End point
        }


    }
}