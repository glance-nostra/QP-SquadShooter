using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using DG.Tweening;
using Fusion;
using UnityEngine.SceneManagement;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class GameController : NetworkBehaviour
    {

        public static GameController instace;
        public NetworkRunner _runner;
        [Space(10)]
        [Header("Base need boolean")]
        public bool GamePlay; // Check that game is running or not

        [Space(10)]
        [Header("All panels")]
        public GameObject panelStart, panelPause; // All panels
        public Image gunfiller;


        [Space(10)]
        [Header("All bot managing variables")]
        public ObjectPoolManager Objectpool;
        public List<Entity> botAll; // All bot gameobject
        [Space(10)]
        [Header("Player managing variables")]
        public Player_Manager player; // The main player gameobject
        public float playerDistance; // Distance bewtween player and exit gate
        public float shotingInterval;
        public bool redyforBrustShooting;
        public Weapon[] Allwepons;
        [Space(10)]
        [Header("Game start anim")]
        public Text GameStartAnimText; // Game start count down text'
        public TextMeshProUGUI waringtext;
        public TextMeshProUGUI[] remaning_bot;
        public TextMeshProUGUI[] remaning_bot2;
        [Space(10)]
        [Header("Ground manager")]
        public List<GameObject> grounds; // All ground
        public GameObject currentGround; // Ground on using
        [Range(1f, 5f)]
        public int activeGround; // Number of ground on using
        public SafeZone zome;
        [Space(10)]
        [Header("Sound manager")]
        public bool Sound = true; // bool which find sound should play or not
        public Image SoundButtonImage; // Sound button image
        public Sprite SoundOn, SoundOff; // Sound on and off sprite
        public SoundManage SoundManage; // Sound manager
        public GameObject gameWinpanel;
        public GameObject gameDrawpanel;
        public GameObject gameLosspanel;
        public GameObject DiconnectPanle;
        public Transform damageIndicaterHolder;
        [Space(10)]
        [Header("Blood Particle Effrcts")]
        public List<ParticleSystem> AllBloodParticles;
        public int BloodParticleCount = 0;
        public Transform BulletsHolder;
        [Space(10)]
        [Header("powerups")]
        public GameObject[] allPowerups;
        public Transform[] allPowerupsPostion;

        [Header("pooling")]
        public GameObject damage_indicator;
        public GameObject ShootPartical;
        public Transform Holder;

        [SerializeField] private TextMeshProUGUI fpsTest;
        public TextMeshProUGUI StatsText;
        public TextMeshProUGUI displayRoomCode;
        private float deltaTime = 0.0f;
        public GameObject gamecompletePnale;
        public int botcount;
        public TextMeshProUGUI timeText, timeText2;


        [Networked, OnChangedRender(nameof(TimeUpdate))]
        public float remainingTime { get; set; }
        public Vector3 safeZoneminmum, safeZonemaximum;
        public GameObject playercontroller;
        public List<Entity> allcharacter = new List<Entity>();

        public Button[] weponSwitchButton1;
        public Image MainfiregameIcon;
        // Start

        public FixedJoystick joystickShoot;
        public bool shooting;
        private void Awake()
        {

            instace = this;
        }

        private void Start()
        {

            Objectpool.CreatePool("DamageIndicator", damage_indicator, 10, Holder);
            Objectpool.CreatePool("ShootingPartical", ShootPartical, 40, Holder);
            Time.timeScale = 1f; // Make game continue
            SoundLoad();

        }

        public void TimeUpdate()
        {

            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);

            if (minutes <= 0)
            {
                minutes = 0;
            }
            if (seconds <= 0)
            {
                seconds = 0;
            }

            timeText.text = string.Format("{0:00} : {1:00}", minutes, seconds);
            timeText2.text = string.Format("{0:00} : {1:00}", minutes, seconds);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_StartGame()
        {
            StartGame();
        }

        public void LeaveRoom()
        {
            if (_runner)
            {
                roomCodeInput.text = "";
                displayRoomCode.text = "";
                StatsText.text = "Left the Room";
                _runner.Shutdown();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);


            }
        }
        bool gameover;
        private void UpdateTimeDisplay()
        {
            if (!timeText) return;
            if (remainingTime > 0)
            {
                if (Object.HasStateAuthority)
                {
                    int minutes = Mathf.FloorToInt(remainingTime / 60);
                    int seconds = Mathf.FloorToInt(remainingTime % 60);
                    timeText.text = string.Format("{0:00} : {1:00}", minutes, seconds);
                    timeText2.text = string.Format("{0:00} : {1:00}", minutes, seconds);

                    remainingTime -= Time.deltaTime;
                }
            }
            else
            {
                if (gameover == false)
                {
                    gameover = true;

                    GameCompepted();
                }
            }

        }
        public int Heightstkillcount;
        public void GameCompepted()
        {
            // gamecompletePnale.SetActive(true);




            //  BotCount();



            for (int i = 1; i < allcharacter.Count; i++)
            {

                if (allcharacter[i].Object.HasInputAuthority)
                {
                    gameLosspanel.SetActive(true);
                }
                //entity.killCount = 0;

                if (allcharacter[i].name.Contains("You"))
                {
                    allcharacter[i].powerupsConter.fillAmount = 0;
                }
            }
            if (allcharacter[0].Object.HasInputAuthority)
            {
                gameWinpanel.SetActive(true);
                gameLosspanel.SetActive(false);

            }
            if (allcharacter[1].killCount == allcharacter[0].killCount)
            {
                gameDrawpanel.SetActive(true);
                gameWinpanel.SetActive(false);
                gameLosspanel.SetActive(false);
            }

            StartCoroutine(waitshutdown());

        }
        IEnumerator waitshutdown()
        {
            yield return new WaitForSeconds(.5f);
            Runner.Shutdown();
        }
        public void SoundLoad()
        {
            // If statements for checking sound setting
            if (PlayerPrefs.GetString("Sound", "true") == "true")
            {
                SoundManage.SoundOnOff(1);
                SoundButtonImage.sprite = SoundOn;
                Sound = true;
                PlayerPrefs.SetString("Sound", "true");
            }
            else
            {
                SoundManage.SoundOnOff(0);
                SoundButtonImage.sprite = SoundOff;
                Sound = false;
                PlayerPrefs.SetString("Sound", "false");

            }
        }

        // Update
        private void Update()
        {

            // deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            // float fps = 1.0f / deltaTime;
            // fpsTest.text = "FPS: " + Mathf.Ceil(fps).ToString();
            if (GamePlay == false || player.is_death)
                return;

            UpdateTimeDisplay();

            if (redyforBrustShooting)
            {
                holdtime += Time.deltaTime;
                gunfiller.fillAmount = holdtime;
                Debug.Log(holdtime + "hold time");
            }

            //playerDistance = Vector3.Distance(player.transform.position, currentGround.GetComponent<Ground>().ExitGate.transform.position);
        }

        // Game will paused through it
        public void PauseGame()
        {
            GamePlay = false;
            Time.timeScale = 0f;
            panelPause.SetActive(true);
        }

        // Game will continue run
        public void ContinueGame()
        {
            Time.timeScale = 1f;
            panelPause.SetActive(false);
            GamePlay = true;

        }

        float holdtime = 0;
        public void onBulletHolding()
        {
            redyforBrustShooting = true;
        }

        public void OnBulletHoldingRemove()
        {

            redyforBrustShooting = false;
            if (holdtime >= 1)
            {
                shotingInterval = .01f;
                StartCoroutine(burstshooting());
            }

        }

        public IEnumerator burstshooting()
        {
            yield return new WaitForSeconds(1);
            gunfiller.fillAmount = 0;
            holdtime = 0;
            shotingInterval = 1;
        }

        // Start the game
        public void StartGame()
        {
            Application.targetFrameRate = -60;
            panelPause.SetActive(false);
            panelStart.SetActive(false);
            gamecompletePnale.SetActive(false);
            SetGround();
            //  StartCoroutine(StartGameAnim());
            if (Object && Object.IsValid)
            {
                remainingTime = 300;
            }

            // StartCoroutine(GameCompleted());
        }

        // Quit the game
        public void EndGame()
        {

            Application.Quit();
        }
        public BasicSpawner spawner;
        public TMP_InputField roomCodeInput;


        public void OnCreateRoomClick()
        {

            // spawner.CreateRoom();
            //StartCoroutine(ShowRoomCodeWhenReady());
        }


        public void OnJoinRoomClick()
        {
            // spawner.JoinRoom(roomCodeInput.text);
        }

        IEnumerator ShowRoomCodeWhenReady()
        {
            yield return new WaitUntil(() => !string.IsNullOrEmpty(spawner.RoomCode));
            displayRoomCode.text = $"Room Code: {spawner.RoomCode}";
        }

        // Restart current game
        public void RestartGame()
        {

            Time.timeScale = 1f;
            StopAllCoroutines();
            StartGame();

        }

        // Sound on and off
        public void SoundOnOffClick()
        {
            if (Sound == true)
            {
                SoundManage.SoundOnOff(0);
                SoundButtonImage.sprite = SoundOff;
                Sound = false;
                PlayerPrefs.SetString("Sound", "false");
            }
            else
            {
                SoundManage.SoundOnOff(1);
                SoundButtonImage.sprite = SoundOn;
                Sound = true;
                PlayerPrefs.SetString("Sound", "true");
            }
        }

        // Count down animation for starting the game
        public IEnumerator StartGameAnim()
        {

            //Camera.main.gameObject.GetComponent<Camera_Follower>().shouldFollow = false;
            //Camera.main.transform.position = new Vector3(0, 20, -45);
            //Camera.main.transform.eulerAngles = new Vector3(30, 0, 0);
            GamePlay = false;
            GameStartAnimText.gameObject.SetActive(true);
            //  SoundManage.SoundPlayStop(0);
            GameStartAnimText.text = "3";
            yield return new WaitForSeconds(1);
            GameStartAnimText.text = "2";
            yield return new WaitForSeconds(1);
            GameStartAnimText.text = "1";
            yield return new WaitForSeconds(1);
            GameStartAnimText.text = "Go!";

            yield return new WaitForSeconds(1);
            Time.timeScale = 1f;
            GameStartAnimText.gameObject.SetActive(false);
            //StartCoroutine(Powerupsspwn());
            //  player.player_Movement.playerRigidbody.isKinematic = false;
            //Camera.main.gameObject.GetComponent<Camera_Follower>().shouldFollow = true;
            //Camera.main.gameObject.GetComponent<Camera_Follower>().isArriveOrignalPos = false;
            GamePlay = true;
            GameController.instace.BotCount();
            SoundManage.GetComponent<AudioSource>().Play();
        }

        public IEnumerator Powerupsspwn()
        {
            while (true)
            {
                //getting all powerups 
                Vector3 spawnpostion = allPowerupsPostion[Random.Range(0, allPowerupsPostion.Length)].position;
                Debug.Log(spawnpostion);
                //getting all powerups positon
                GameObject powerups = allPowerups[Random.Range(0, allPowerups.Length)];
                powerups.gameObject.SetActive(true);
                powerups.transform.position = spawnpostion;
                yield return new WaitForSeconds(Random.Range(5, 10));

            }

            //sellecting random powerups and its positon

            //activating powerups


        }
        //[Rpc(RpcSources.All, RpcTargets.All)]
        //public void Rpc_Playerjoined(int id)
        //{

        //    if (_playerObjects.TryGetValue(id, out GameObject playerObject))
        //    {
        //        // You got the GameObject of the player who joined!
        //        Debug.Log("Player object found: " + playerObject.name);

        //        // Do something with it
        //        playerObject.GetComponent<Renderer>().material.color = Color.green; // Example
        //    }
        //    else
        //    {
        //        Debug.LogWarning("No GameObject found for ID: " + id);
        //    }


        //}
        // Counting bot for game end
        public void BotCount()
        {

            //if(botcount == 0)
            //{
            //    botcount = -1;
            //    StopAllCoroutines();
            //    GameCompepted();
            //}
            // Sort characters based on kill count in descending order
            if (Object && Object.IsValid)
            {
                allcharacter = allcharacter.OrderByDescending(c => c.killCount).ToList();

                // Update UI text fields with animation
                for (int i = 0; i < remaning_bot.Length; i++)
                {
                    if (i < allcharacter.Count)
                    {
                        allcharacter[i].winner = false;
                        string playerName = allcharacter[i].name;
                        string killCount = allcharacter[i].killCount.ToString();
                        string newText = $"{playerName}  {killCount}";

                        // Animate text change
                        AnimateTextChange(remaning_bot[i], newText);
                        AnimateTextChange(remaning_bot2[i], newText);
                        // remaning_bot2[i].transform.localScale = remaning_bot[i].transform.localScale;
                        //  AnimateTextChange(remaning_bot2[i], newText);
                    }
                    else
                    {
                        //AnimateTextChange(remaning_bot[i], "");
                    }
                }
                allcharacter[0].winner = true;
            }
        }

        private void AnimateTextChange(TextMeshProUGUI uiText, string newText)
        {
            // Fade out old text
            uiText.DOFade(0, 0.2f).OnComplete(() =>
            {
                uiText.text = newText; // Change text
            uiText.DOFade(1, 0.2f); // Fade in new text
        });

            uiText.transform.DOScale(1.2f, 0.2f).SetLoops(2, LoopType.Yoyo).OnComplete(() =>
            {
                uiText.transform.localScale = Vector3.one; // Ensure final scale is (1,1,1)
        });
        }

        public void playersize(float value)
        {
            foreach (var item in allcharacter)
            {
                item.transform.localScale = new Vector3(value, value, value);
            }
        }

        public static bool testc;
        // Set ground, bots and player
        void SetGround()
        {

            // Select ground
            if (Object && Object.IsValid)
                remainingTime = 300;
            currentGround = null;
            for (int i = 0; i < Holder.childCount; i++)
            {
                Holder.GetChild(i).gameObject.SetActive(false);
            }
            for (int i = 0; i < grounds.Count; i++)
            {
                grounds[i].gameObject.SetActive(false);
            }

            activeGround = Random.Range(0, grounds.Count);

            currentGround = grounds[activeGround];
            currentGround.SetActive(true);

            //// Declare ground variable
            Ground groundSctipt = currentGround.GetComponent<Ground>();

            zome.transform.localScale = zome.startingsclae;
            zome.start = true;


            for (int i = 0; i < weponSwitchButton1.Length; i++)
            {
                weponSwitchButton1[i].transform.GetChild(0).GetComponent<Image>().sprite = emtygun;
            }
            //   MainfiregameIcon.sprite = player.allCollectedWepon[0].icon;
            // player.gameObject.SetActive(true);
        }
        public Sprite emtygun;
        // Show blood particles effect
        public void ShowBlood(Vector3 posPlay)
        {
            if (BloodParticleCount >= AllBloodParticles.Count - 1)
            {
                BloodParticleCount--;
            }
            ParticleSystem currentParticle = AllBloodParticles[BloodParticleCount];
            AllBloodParticles[BloodParticleCount].transform.position = posPlay;
            AllBloodParticles[BloodParticleCount].Play();
            float startLifetime = AllBloodParticles[BloodParticleCount].main.startLifetime.constant;
            BloodParticleCount++;
            StartCoroutine(ResetBloodarticle(startLifetime));
        }

        IEnumerator ResetBloodarticle(float time)
        {
            yield return new WaitForSeconds(time);
            BloodParticleCount--;
        }

    }
}