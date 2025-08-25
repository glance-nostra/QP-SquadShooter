using Fusion;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Fusion;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class Entity : NetworkBehaviour
    {
        //[Networked, OnChangedRender(nameof(Objectnamechange))]
        //public string names { get; set; }

        [Header("Coponets")]
        public GameController gameManager;
        public Entity Enemy; // All enemies in that list
        protected Rigidbody entity_rb;
        protected Collider entity_colider; // Fixed 'colider' to 'collider'
        protected NavMeshAgent entity_navAi;
        protected Animator entity_animator;
        protected AudioSource enity_audio; // Fixed 'enity_audio' to 'entity_audio'
        protected Weapon startingwepon;

        [Networked, OnChangedRender(nameof(HealthShow))]
        public float CurrentHealth { get; set; }


        [Networked]
        public float killCount { get; set; } // Fixed 'killcount' to 'killCount'

        protected float nearestenemydis = 1000f; // Fixed 'nearestenemydis'

        [Space(10)]
        [Header("Health Manager")] // Fixed 'Heath' to 'Health'

        [SerializeField] protected float maxHealth; // Fixed 'MaxHealth' casing
        [SerializeField] private bool healing; // Fixed 'Healing' casing

        public bool isPlayer; // Fixed 'is_player' to follow C# naming conventions
        public bool is_death { get; private set; } // Fixed 'is_death' to 'isDead'


        public float moveSpeed = 5f;
        public float rotationSpeed = 10f;
        public Vector3 startingPosition; // Fixed 'startingpostion' to 'startingPosition'


        [Space(10)]
        [Header("Audio manager")]
        [SerializeField] protected AudioClip playerDeath;

        //parts
        [Space(10)]
        [Header("Whole body object manager")]
        [SerializeField] private List<GameObject> body_parts; // All body parts for activation and deactivation
        [SerializeField] protected GameObject death_partclesystem; // Death particle system
        public bool winner;
        //valus
        public Vector3 starting_pos;
        [SerializeField] private GameObject HealthBarFG; // Health bar on the head image
        [SerializeField] private GameObject Healthbarmain;
        [SerializeField] private TextMeshPro HealthPerText; // Health percantage text
        public bool isTargetSelected; // Check that tarhet is selected or not
        public bool isTargeting; // Check that is finding target or not
                                 //shooting
        public GameObject dropingwepon;
        //shooting
        public bool isInInterval;
        public Image powerupsConter;
        bool shooting;
        public Weapon my_wepon;
        public bool insideGrass;
        public Grass EnteredGrass;
        public Color insidegrass, outsidegrass;
        public float shooting_radious;
        public List<Weapon> allCollectedWepon = new List<Weapon>();
        [Header("powerups")]
        public GameObject shildeffect, speedeffect, passthroughEffect;
        public bool shild;
        Weapon Weponcolleing;
        public GameObject setdestination;
        #region MonoMethods
        public virtual void Awake()
        {
            startingwepon = allCollectedWepon[0];
            enity_audio = GetComponent<AudioSource>();
            entity_rb = GetComponent<Rigidbody>();
            entity_navAi = GetComponent<NavMeshAgent>();
            entity_colider = GetComponent<Collider>();
            entity_animator = GetComponent<Animator>();
            if (entity_navAi)
                entity_navAi.updateRotation = true;

            tempspeed = moveSpeed;
        }

        public void killCountChanged()
        {
            gameManager.BotCount();
        }

        public void Objectnamechange()
        {
            //if(Object.HasInputAuthority == false)
            //    this.gameObject.name = names;
        }
        public virtual void Start()
        {
            // starting_pos = transform.position;

            if (Object)
            {
                //if (Object.HasInputAuthority)
                //{
                //    names = "Player" + Random.Range(0, 1000).ToString();

                //}
                CurrentHealth = maxHealth;

            }
            for (int i = 0; i < transform.childCount - 1; i++)
            {
                body_parts.Add(transform.GetChild(i).gameObject);
            }

            gameManager.SoundLoad();
            ///  Healthbarmain.transform.parent = transform.parent;
        }
        public virtual void Update()
        {


            if (Weponcolleing)
            {
                int id = Weponcolleing.id;
                bool canabletocollect = true;
                for (int i = 0; i < allCollectedWepon.Count; i++)
                {
                    if (allCollectedWepon[i].id == id)
                    {
                        canabletocollect = false;
                        Weponcolleing = null;
                        break;
                    }
                }
                if (canabletocollect)
                {
                    Weponcolleing.filler.fillAmount += Time.deltaTime / 5;
                    if (Weponcolleing.filler.fillAmount == 1)
                    {

                        Weponcolleing.filler.fillAmount = 2;
                        Weapon selectedwepon = allCollectedWepon[0].gameObject.transform.parent.
                            GetChild(id).GetComponent<Weapon>();
                        allCollectedWepon.Add(selectedwepon);
                        if (Object.HasInputAuthority)
                        {
                            gameManager.weponSwitchButton1[allCollectedWepon.Count - 2].transform.GetChild(0).GetComponent<Image>().sprite = Weponcolleing.icon;
                            int countno = allCollectedWepon.Count - 2;
                            gameManager.weponSwitchButton1[allCollectedWepon.Count - 2].onClick.AddListener(() => WeponSwitch(id, countno));

                        }
                        StartCoroutine(Weponcolleing.GetComponent<Reactivate>().reacrivate());
                        Weponcolleing.filler.fillAmount = 0;
                        Weponcolleing.gameObject.SetActive(false);
                        Weponcolleing = null;
                    }
                    //    }
                    //}
                    //else
                    //{
                    //    Weponcolleing.filler.fillAmount = 0;
                    //    Weponcolleing = null;
                    //}

                }
                //if (direction != Vector3.zero)
                //{
                //    Quaternion lookRotation = Quaternion.LookRotation(direction);
                //    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
                //}
            }
            //if (Weponcolleing)
            //{
            //    Weponcolleing.filler.fillAmount += Time.deltaTime / 5f;

            //    if (Weponcolleing.filler.fillAmount == 1)
            //    {

            //        Weponcolleing.filler.fillAmount = 2;
            //        // my_wepon.gameObject.SetActive(false);

            //        // allCollectedWepon.Add(selectedwepon);
            //        my_wepon = my_wepon.gameObject.transform.parent.GetChild(Weponcolleing.id).GetComponent<Weapon>();

            my_wepon.entity = this;
            //        Weponcolleing.GetComponent<Reactivate>().Relocating();
            //        my_wepon.gameObject.SetActive(true);
            //        Weponcolleing.filler.fillAmount = 0;
            //        Weponcolleing = null;
            //    }
            //}


        }

        public void WeponSwitch(int id, int countno)
        {

            RPC_WeponSwitch(countno);

        }


        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_WeponSwitch(int countno)
        {
            Debug.Log("wepon Switching " + countno + 1);
            Weapon temp = allCollectedWepon[0];
            allCollectedWepon[0] = allCollectedWepon[countno + 1];

            allCollectedWepon[countno + 1] = temp;
            //gameManager.weponSwitchButton1[id].transform.GetChild(0).GetComponent<Image>().sprite = temp.icon;
            //allCollectedWepon[id] = temp;
            if (Object.HasInputAuthority)
            {
                gameManager.MainfiregameIcon.sprite = allCollectedWepon[0].icon;
                gameManager.weponSwitchButton1[countno].transform.GetChild(0).GetComponent<Image>().sprite =
                    allCollectedWepon[countno + 1].icon;
            }

            //gameManager.wepqonSwitchButton1[countno].onClick.RemoveAllListeners();
            //gameManager.weponSwitchButton1[countno].onClick.AddListener(() =>
            //WeponSwitch(id, countno));
            allCollectedWepon[0].gameObject.SetActive(true);
            allCollectedWepon[countno + 1].gameObject.SetActive(false);

            entity_animator.SetInteger("WeponID", allCollectedWepon[0].id);
        }
        public void ResetingGun()
        {
            allCollectedWepon[0].gameObject.SetActive(false);
            allCollectedWepon.Clear();

            allCollectedWepon.Add(startingwepon);
            allCollectedWepon[0].gameObject.SetActive(true);
            if (Object.HasInputAuthority)
            {
                for (int i = 0; i < gameManager.weponSwitchButton1.Length; i++)
                {
                    gameManager.weponSwitchButton1[i].onClick.RemoveAllListeners();
                    gameManager.weponSwitchButton1[i].transform.GetChild(0).GetComponent<Image>().sprite = gameManager.emtygun;
                }
                gameManager.MainfiregameIcon.sprite = allCollectedWepon[0].icon;
            }
        }
        public virtual void FixedUpdate()
        {

        }
        private void LateUpdate()
        {

            if (Object && Object.IsValid)
                HealthShow();

        }
        public virtual void OnTriggerEnter(Collider other)
        {

            if (gameManager.GamePlay == false)
            {
                return;
            }
            if (other.CompareTag("Obstacle"))
            {
                Debug.Log("outside Colider " + other.gameObject.name);
                insidecolider = true;
            }
            Reactivate obj = other.GetComponent<Reactivate>();
            if (other.GetComponent<Powerups>() && obj)
            {
                Debug.Log("entered");
                StartCoroutine(obj.reacrivate());
                return;
            }
            if (other.GetComponent<Weapon>())
            {

                Weponcolleing = other.GetComponent<Weapon>();
                return;

            }

            if (other.GetComponent<Entity>() || other.GetComponent<Bullet>())
            {
                return;
            }

            //trigering powerups

        }

        public virtual void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Obstacle"))
            {
                Debug.Log("outside Colider " + other.gameObject.name);
                insidecolider = false;
            }

            if (other.GetComponent<Weapon>() && Weponcolleing)
            {
                Weponcolleing.filler.fillAmount = 0;
                Weponcolleing = null;
                return;
            }
            if (other.GetComponent<Powerups>() && other.GetComponent<Reactivate>())
            {

                return;
            }
            if (other.GetComponent<Entity>() || other.GetComponent<Bullet>())
            {
                return;
            }


        }
        #endregion

        public void ReduceHeath(float damage, Entity gethitfrom)
        {
            if (shild && damage < 100)
                return;

            if (Object.HasStateAuthority)
            {
                CurrentHealth -= damage;
            }

            if (damage < 100)
            {
                Gethitfrom = gethitfrom;


                GameObject indicator = gameManager.Objectpool.GetFromPool("DamageIndicator", this.transform.position, Quaternion.identity, gameManager.damageIndicaterHolder);
                indicator.GetComponent<TextMeshPro>().text = "-" + damage.ToString();
                StartCoroutine(DeactivatingObject(indicator));
            }
            //showing the current Health
            // Show damage indicator
            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;

            }
        }
        public Entity Gethitfrom;

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_ReduceHeath(float damage, Entity gethitfrom)
        {
            if (shild && damage < 100)
                return;

            Debug.Log("Heatl reducing");
            if (Object.HasStateAuthority)
            {
                CurrentHealth -= damage;
            }

            if (damage < 100)
            {
                Gethitfrom = gethitfrom;

                GameObject indicator = gameManager.Objectpool.GetFromPool("DamageIndicator", this.transform.position, Quaternion.identity, gameManager.damageIndicaterHolder);
                indicator.GetComponent<TextMeshPro>().text = "-" + damage.ToString();
                StartCoroutine(DeactivatingObject(indicator));
            }
            //showing the current Health
            // Show damage indicator
            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;

            }
        }

        public void ResetingHealth()
        {
            CurrentHealth = maxHealth;
            HealthShow();
        }
        public bool dead;
        public void HealthShow()
        {
            float healthPercentage = (CurrentHealth / maxHealth) * 100f;
            if (healthPercentage >= 0)
            {
                HealthBarFG.transform.localScale = new Vector3(healthPercentage / 100, HealthBarFG.transform.localScale.y, HealthBarFG.transform.localScale.z);
            }
            else
            {
                HealthBarFG.transform.localScale = new Vector3(0, HealthBarFG.transform.localScale.y, HealthBarFG.transform.localScale.z);
            }
            HealthPerText.text = healthPercentage.ToString("00") + "%";


            if (CurrentHealth <= 0 && !dead)
            {


                dead = true;
                CurrentHealth = 0;
                if (Gethitfrom)
                {
                    Gethitfrom.killCount += 1;

                    ResetingGun();
                    RPC_Death();
                    Gethitfrom = null;
                }
                else
                {
                    ResetingGun();
                    Debug.Log("Died with full health");
                    normalDeath();
                }
                Debug.Log("Over");
                //dead = true;
                //CurrentHealth = -1;
                //BodyVisibility(false);
                //Enemy = null;
                //Healthbarmain.SetActive(false);
                //is_death = true;
                //if (entity_rb) entity_rb.isKinematic = true;
                //if (entity_colider) entity_colider.enabled = false;
                //if (entity_navAi) entity_navAi.enabled = false;
                //StartCoroutine(Respawn());

            }
        }
        public virtual IEnumerator IncreaseHeath(float value)
        {
            yield return new WaitUntil(() => Object.IsValid);


            while (healing && Object.HasInputAuthority)
            {

                if (CurrentHealth < maxHealth)
                {
                    CurrentHealth += value;

                    HealthShow();
                }


                yield return new WaitForSeconds(value);
                CurrentHealth = CurrentHealth > maxHealth ? maxHealth : CurrentHealth;
            }
        }

        // [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public virtual void RPC_Death()
        {

            //   Enemy.ki++;

            BodyVisibility(false);
            Enemy = null;
            Debug.Log("dead");
            Healthbarmain.SetActive(false);
            is_death = true;
            if (entity_rb) entity_rb.isKinematic = true;
            if (entity_colider) entity_colider.enabled = false;
            if (entity_navAi) entity_navAi.enabled = false;

            if (Object.HasStateAuthority)
                gameManager.BotCount();




            //shild off

            // dropingwepon.gameObject.SetActive(true);
            // dropingwepon.transform.GetChild(my_wepon.id).gameObject.SetActive(true);

            //gameManager.BotCount();
            //  StartCoroutine(DeathPartical());
            StartCoroutine(Respawn());
        }
        public virtual void normalDeath()
        {

            BodyVisibility(false);
            Enemy = null;
            Debug.Log("dead");
            Healthbarmain.SetActive(false);
            is_death = true;
            if (entity_rb) entity_rb.isKinematic = true;
            if (entity_colider) entity_colider.enabled = false;
            if (entity_navAi) entity_navAi.enabled = false;

            gameManager.BotCount();
            // dropingwepon.gameObject.SetActive(true);
            // dropingwepon.transform.GetChild(my_wepon.id).gameObject.SetActive(true);
            if (effectloading != null)
                StopCoroutine(effectloading);
            effectloading = FillEffect(1);
            StartCoroutine(effectloading);

            //shild off
            shildeffect.SetActive(false);
            shild = false;

            //speed off
            speedeffect.SetActive(false);

            speedbosting = false;

            //pasthrough effect off
            passthroughEffect.SetActive(false);
            entity_colider.isTrigger = false;
            entity_rb.useGravity = true;

            //invicdibel
            insideGrass = false;
            for (int i = 0; i < allmaterial.Length; i++)
            {

                allmaterial[i].shadowCastingMode = ShadowCastingMode.On;
                SetOpaque(allmaterial[i].material);
            }

            //gameManager.BotCount();
            //  StartCoroutine(DeathPartical());
            StartCoroutine(Respawn());
        }
        IEnumerator Respawn()
        {
            yield return new WaitForSeconds(1);
            gameManager.BotCount();

            yield return new WaitForSeconds(1);
            shildeffect.SetActive(false);
            shild = false;

            //speed off
            speedeffect.SetActive(false);
            moveSpeed = tempspeed;
            speedbosting = false;

            //pasthrough effect off
            passthroughEffect.SetActive(false);
            entity_colider.isTrigger = false;
            entity_rb.useGravity = true;

            //invicdibel
            insideGrass = false;
            for (int i = 0; i < allmaterial.Length; i++)
            {

                allmaterial[i].shadowCastingMode = ShadowCastingMode.On;
                SetOpaque(allmaterial[i].material);
            }
            // StopAllCoroutines();
            if (effectloading != null)
                StopCoroutine(effectloading);
            effectloading = FillEffect(.001f);
            StartCoroutine(effectloading);
            //    dropingwepon.transform.GetChild(my_wepon.id).gameObject.SetActive(false);

            if (Object.HasStateAuthority)
            {
                startingPosition = new Vector3(Random.Range(-3, 4), transform.position.y, Random.Range(-3, 4));
                GetComponent<NetworkTransform>().Teleport(startingPosition, Quaternion.identity);
            }

            yield return new WaitForSeconds(1);
            if (entity_rb) entity_rb.isKinematic = false;
            if (entity_colider) entity_colider.enabled = true;
            if (entity_navAi) entity_navAi.enabled = true;
            CurrentHealth = maxHealth;
            HealthShow();
            Healthbarmain.SetActive(true);

            BodyVisibility(true);
            is_death = false;
            dead = false;
        }



        public virtual void ResetingGame()
        {
            if (powerupsConter)
                powerupsConter.fillAmount = 0;
            entity_animator.SetBool("Shoot", false);
            CurrentHealth = maxHealth;
            //Restart the game
            foreach (var item in allCollectedWepon)
            {
                item.gameObject.SetActive(false);
            }
            allCollectedWepon.Clear();


            allCollectedWepon.Add(startingwepon);

            if (Object.HasInputAuthority)
                killCount = 0;

            is_death = false;
            Healthbarmain.SetActive(true);
            HealthShow();

            entity_rb.linearVelocity = Vector3.zero;
            shildeffect.SetActive(false);
            shild = false;
            if (speedbosting)
            {
                moveSpeed /= 2;
                speedbosting = false;
                speedeffect.SetActive(false);
            }
            passthroughEffect.SetActive(false);
            entity_colider.isTrigger = false;
            entity_rb.useGravity = true;
            for (int i = 0; i < allmaterial.Length; i++)
            {
                SetOpaque(allmaterial[i].material);
            }

            transform.rotation = Quaternion.identity;
            StopAllCoroutines();
            CancelInvoke();
            BodyVisibility(true);
            allCollectedWepon[0].gameObject.SetActive(true);
            this.gameObject.SetActive(true);
            if (entity_rb) entity_rb.isKinematic = false;
            if (entity_colider) entity_colider.enabled = true;
            if (entity_navAi) entity_navAi.enabled = true;
            this.transform.position = new Vector3(Random.Range(-5, 5), this.transform.position.y, Random.Range(-5, 5));
            transform.rotation = Quaternion.identity;//making 000
            StartCoroutine(IncreaseHeath(1));
            gameManager.BotCount();
        }

        // Change a visibility of the body
        public virtual void BodyVisibility(bool visibility)
        {
            for (int i = 0; i < body_parts.Count; i++)
            {
                body_parts[i].gameObject.SetActive(visibility);
            }
        }

        public virtual void BodyVisibility(Color color)
        {
            for (int i = 0; i < body_parts.Count; i++)
            {
                body_parts[i].GetComponentInChildren<MeshRenderer>().material.color = color;
            }
        }

        IEnumerator DeactivatingObject(GameObject objects, float time = 1.5f)
        {
            yield return new WaitForSeconds(time);
            gameManager.Objectpool.ReturnToPool("DamageIndicator", objects);
        }


        public virtual void Shotting()
        {
            if (!shooting)
            {
                shooting = true;
                Bullet spawnedBullets = gameManager.Objectpool.GetFromPool(allCollectedWepon[0].bullets.name,
                        allCollectedWepon[0].FirePoints[0].transform.position,
                    Quaternion.Euler(0, allCollectedWepon[0].FirePoints[0].transform.eulerAngles.y, 0)).GetComponent<Bullet>();
                Debug.Log(spawnedBullets.name + "Shooting");
                spawnedBullets.entity_holder = this.gameObject.GetComponent<Entity>();
                StartCoroutine(waitfornextshoot());
            }


        }
        IEnumerator waitfornextshoot()
        {
            yield return new WaitForSeconds(.5f);
            shooting = false;
        }
        public void HealthBarShake()
        {
            StartCoroutine(ShakeHealthBar());
        }

        private IEnumerator ShakeHealthBar()
        {
            Vector3 originalPos = HealthBarFG.transform.localPosition;

            float duration = 0.5f;
            float elapsed = 0f;
            float magnitude = 5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float offsetX = Random.Range(-magnitude, magnitude);
                float offsetY = Random.Range(-magnitude, magnitude);

                HealthBarFG.transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);
                yield return null;
            }

            HealthBarFG.transform.localPosition = originalPos;
        }
        public IEnumerator RemovingBullets(GameObject shootingpartical)
        {
            yield return new WaitForSeconds(.5f);
            gameManager.Objectpool.ReturnToPool("ShootingPartical", shootingpartical);


        }
        #region Powerups
        public bool insidecolider;
        public Renderer[] allmaterial;
        public Material mat;
        [SerializeField] private GameObject direction, circle;
        // Coroutine to gradually decrease fill amount
        private IEnumerator FillEffect(float duration)
        {
            float timeElapsed = 0;
            powerupsConter.fillAmount = 1; // Start full

            while (timeElapsed < duration)
            {
                powerupsConter.fillAmount = 1 - (timeElapsed / duration);

                timeElapsed += Time.deltaTime;
                yield return null;
            }

            powerupsConter.fillAmount = 0; // Ensure it's empty at the end
        }
        IEnumerator effectloading;
        public IEnumerator ShieldActivate()
        {
            //acriavter shild
            shild = true;

            GameObject indicator = gameManager.Objectpool.GetFromPool("DamageIndicator", this.transform.position, Quaternion.identity, gameManager.damageIndicaterHolder);
            indicator.GetComponent<TextMeshPro>().text = "Shield";
            StartCoroutine(DeactivatingObject(indicator));

            if (!insideGrass)
                shildeffect.SetActive(true);

            //showing effect
            if (powerupsConter)
            {
                if (effectloading != null)
                    StopCoroutine(effectloading);

                effectloading = FillEffect(7);
                StartCoroutine(effectloading);
            }
            yield return new WaitForSeconds(7);
            shildeffect.SetActive(false);
            shild = false;
            //deaactivate shild
        }
        bool speedbosting;
        float tempspeed;
        public IEnumerator SpeedBoost()
        {
            if (!speedbosting)
            {
                speedbosting = true;
                GameObject indicator = gameManager.Objectpool.GetFromPool("DamageIndicator", this.transform.position, Quaternion.identity, gameManager.damageIndicaterHolder);
                indicator.GetComponent<TextMeshPro>().text = "X2";
                StartCoroutine(DeactivatingObject(indicator));

                if (!insideGrass)
                    speedeffect.SetActive(true);
                moveSpeed *= 1.5f;
                if (powerupsConter)
                {
                    if (effectloading != null)
                        StopCoroutine(effectloading);

                    effectloading = FillEffect(7);
                    StartCoroutine(effectloading);
                }
                yield return new WaitForSeconds(7);
                speedeffect.SetActive(false);
                moveSpeed = tempspeed;
                speedbosting = false;
            }
            else
            {

                yield return new WaitForSeconds(.1f);

            }

        }



        public IEnumerator Invisible()
        {
            GameObject indicator = gameManager.Objectpool.GetFromPool("DamageIndicator", this.transform.position, Quaternion.identity, gameManager.damageIndicaterHolder);
            indicator.GetComponent<TextMeshPro>().text = "Mutant";
            StartCoroutine(DeactivatingObject(indicator));
            entity_rb.useGravity = false;
            entity_colider.isTrigger = true;
            if (!insideGrass)
                passthroughEffect.SetActive(true);
            if (powerupsConter)
            {
                if (effectloading != null)
                    StopCoroutine(effectloading);

                effectloading = FillEffect(7);
                StartCoroutine(effectloading);
            }
            yield return new WaitForSeconds(7);
            passthroughEffect.SetActive(false);
            Debug.Log("Before state of insdie grass" + insidecolider);

            yield return new WaitUntil(() => insidecolider == false);
            Debug.Log("after state of while" + insidecolider);
            entity_colider.isTrigger = false;
            entity_rb.useGravity = true;
        }
        // public SkinnedMeshRenderer[] allmaterial;
        public IEnumerator Hide()
        {
            shildeffect.SetActive(false);
            passthroughEffect.SetActive(false);
            speedeffect.SetActive(false);
            insideGrass = true;
            GameObject indicator = gameManager.Objectpool.GetFromPool("DamageIndicator", this.transform.position, Quaternion.identity, gameManager.damageIndicaterHolder);
            StartCoroutine(DeactivatingObject(indicator));
            indicator.GetComponent<TextMeshPro>().text = "Cameo";
            for (int i = 0; i < allmaterial.Length; i++)
            {
                SetTransparent(allmaterial[i].material);
                allmaterial[i].shadowCastingMode = ShadowCastingMode.Off;
                // SetMaterialTransparent(allmaterial[i].material);
                //    yield return StartCoroutine(FadeToTransparent(allmaterial[i].material, 3));
                //need to make it trasparent

            }

            if (powerupsConter)
            {
                if (effectloading != null)
                    StopCoroutine(effectloading);

                effectloading = FillEffect(7);
                StartCoroutine(effectloading);
            }
            yield return new WaitForSeconds(7);



            insideGrass = false;
            for (int i = 0; i < allmaterial.Length; i++)
            {


                // SetMaterialTransparent(allmaterial[i].material);
                //    yield return StartCoroutine(FadeToTransparent(allmaterial[i].material, 3));
                allmaterial[i].shadowCastingMode = ShadowCastingMode.On;
                SetOpaque(allmaterial[i].material);
            }

            if (entity_colider.isTrigger)
                passthroughEffect.SetActive(true);

            if (shild)
                shildeffect.SetActive(true);

            if (speedbosting)
                speedeffect.SetActive(true);

        }
        private IEnumerator FadeToTransparent(Material mat, float duration)
        {
            Color color = mat.color;
            float startAlpha = color.a;
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                color.a = Mathf.Lerp(startAlpha, 0f, time / duration);
                mat.color = color;
                yield return null;
            }
        }
        void SetTransparent(Material targetMaterial)
        {

            if (Object.HasInputAuthority)
            {
                Color color = new Color(targetMaterial.color.r, targetMaterial.color.g, targetMaterial.color.b, .25f);
                targetMaterial.color = color;
            }
            else
            {
                Color color = new Color(targetMaterial.color.r, targetMaterial.color.g, targetMaterial.color.b, 0f);
                Healthbarmain.gameObject.SetActive(false);
                circle.gameObject.SetActive(false);
                direction.gameObject.SetActive(false);
                targetMaterial.color = color;
            }

            targetMaterial.SetFloat("_Surface", 1); // Transparent
            targetMaterial.renderQueue = (int)RenderQueue.Transparent; // Move to Transparent queue
            targetMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            targetMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            targetMaterial.SetFloat("_ZWrite", 0); // Disable ZWrite for proper transparency
            targetMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");


        }

        void SetOpaque(Material targetMaterial)
        {

            if (Object.HasInputAuthority)
            {
                Color color = new Color(targetMaterial.color.r, targetMaterial.color.g, targetMaterial.color.b, 1f);
                targetMaterial.color = color;
            }
            else
            {
                Color color = new Color(targetMaterial.color.r, targetMaterial.color.g, targetMaterial.color.b, 1f);
                Healthbarmain.gameObject.SetActive(true);
                circle.gameObject.SetActive(true);
                direction.gameObject.SetActive(true);
                targetMaterial.color = color;
            }
            targetMaterial.SetFloat("_Surface", 0); // Opaque
            targetMaterial.renderQueue = (int)RenderQueue.Geometry; // Move to Opaque queue
            targetMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            targetMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            targetMaterial.SetFloat("_ZWrite", 1); // Enable ZWrite
            targetMaterial.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        #endregion
        public virtual void GetNeartestEnemy()
        {

            Enemy = null;
            foreach (var item in gameManager.allcharacter)
            {
                if (!item.is_death && item != this && item.gameObject.activeInHierarchy)
                {
                    float distace = Vector3.Distance(this.transform.position, item.transform.position);

                    if (distace < nearestenemydis && distace < shooting_radious && !item.insideGrass)
                    {
                        nearestenemydis = distace;
                        Enemy = item;
                        break;
                    }


                }

            }
            if (Enemy == null)
            {
                nearestenemydis = shooting_radious;

            }

        }
    }
}