
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using Fusion;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class Player_Manager : Entity
    {
        [Space(10)]
        [Header("All Health managing variable")]
        [SerializeField] private float playerHealthIncrement = 1f; // Player health recovery amount
        [SerializeField] private TextMeshProUGUI textHealth, textHealth1; // Player health amount status
        [SerializeField] private Image HealthBarSlider;
        //[SerializeField] private TextMeshPro HealthPerText;
        [SerializeField] private ParticleSystem HealthIncreaserParticle;

        [Space(10)]
        [Header("Player Score Managing variable")]
        [SerializeField] private int playerScore; // Player current score
        [SerializeField] private TextMeshProUGUI textScore, textScore1; // Player score amount status

        [Space(10)]
        [Header("Auto aim & enemy managing variables")]
        public int killcount;
        public float enemyDistance; // Enemy distance for fight
        public int enemyInRadius; // Enemy count in our radius




        [Space(10)]
        [Header("Other player scripts")]
        public Player_Movement player_Movement; // Player movement script access
        public Player_Shooting player_Shooting; // Player shooting script access

        [Space(10)]
        [Header("Player default transform")]
        [SerializeField] private Vector3 startingPos; // Player start position
        [SerializeField] private Vector3 startingEular; // Player start eular (rotation)
                                                        // [SerializeField] private Vector3 startingScale; // Player start scale



        [Space(10)]
        [Header("Audio managing system")]
        public AudioSource playerAudio; // Audio source which handle player audios
        public AudioClip runSurface, runRamp/*, playerDeath*/; // All audio clips
        public bool isSoundPlaying;


        //  public Vector3 startingpoint;

        public override void Start()
        {
            gameManager = GameController.instace;
            base.Start();
            //  startingPos = transform.position;
            ReassignValue();
            player_Movement = GetComponent<Player_Movement>();
            player_Shooting = GetComponent<Player_Shooting>();

        }

        public void OnAnimationEventTriggered() // Method name must match the Animation Event
        {
            Debug.Log("Event Trigered from playermanager1 time");
            Shotting();

        }

        // Update is called once per frame
        public override void Update()
        {
            //if()
            //HealthBar.transform.LookAt(Camera.main.transform.position);
            if (gameManager.GamePlay == false || is_death)
            {
                return;
            }

            base.Update();

            AutoTarget();

        }
        int weponid = 0;
        public void NextWepon()
        {
            //foreach (var item in allWepons)
            //{
            //    item.gameObject.SetActive(false);
            //}
            //my_wepon = allWepons[weponid];
            allCollectedWepon[0].gameObject.SetActive(true);
            weponid++;
            if (weponid > 14)
                weponid = 0;


        }

        public override void GetNeartestEnemy()
        {
            base.GetNeartestEnemy();
            if (Enemy)
            {
                Vector3 targetPosition = new Vector3(Enemy.transform.position.x, transform.position.y, Enemy.transform.position.z);
                transform.LookAt(targetPosition);
                //Shoot();
                transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
                isTargeting = true;
                //  StartFollowing();
            }


        }

        public void Respawing()
        {
            ResetingGame();
        }
        void AutoTarget()
        {

            //if (gameManager.GamePlay == false || isDead)
            //    return;

            GetNeartestEnemy();

        }

        // Player score update in text
        //void ScoreTextUpdate()
        //{
        //    textScore.text = playerScore.ToString("00");
        //    textScore1.text = playerScore.ToString("00");
        //}

        // Player health update in text
        void HealthTextUpdate()
        {
            if (CurrentHealth >= 0)
            {
                textHealth.text = CurrentHealth.ToString("00") + " / " + maxHealth.ToString("00");
                textHealth1.text = CurrentHealth.ToString("00") + " / " + maxHealth.ToString("00");
                HealthBarSlider.fillAmount = maxHealth / 100;

            }
            else
            {
                textHealth.text = "00" + " / " + maxHealth.ToString("00");
                textHealth1.text = "00" + " / " + maxHealth.ToString("00");

            }
        }

        // [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public override void RPC_Death()
        {


            base.RPC_Death();
            Debug.Log("PlayerDeath");
            StartCoroutine(PlayerDeath());



        }



        IEnumerator PlayerDeath()
        {
            isSoundPlaying = true;
            CancelInvoke();

            insideGrass = false;

            yield return new WaitForSeconds(3);

            //transform.position = startingPos;
            ResetingHealth();

        }


        // Kill player
        public void KillPlayer(int ScoreIncrementAmount)
        {
            playerScore += ScoreIncrementAmount;
            // ScoreTextUpdate();
        }

        public override void ResetingGame()
        {
            base.ResetingGame();
            //Camera.main.gameObject.GetComponent<Camera_Follower>().player = this.transform;
            player_Movement.AnimationController(AnimState.Idle);
            playerScore = 0;
            HealthTextUpdate();
            //ScoreTextUpdate();

            // player_Shooting.CollectingBullet();
            isSoundPlaying = false;
            enemyInRadius = 0;
            //listEnemy.Clear();

            //AssignMyWeapone();
            this.transform.position = startingPos;
            this.transform.eulerAngles = startingEular;

            playerAudio.Stop();
        }


        public void ReassignValue()
        {
            startingPos = new Vector3(transform.position.x, transform.position.y + .5f, transform.position.z);
            startingEular = transform.eulerAngles;

        }


    }
}