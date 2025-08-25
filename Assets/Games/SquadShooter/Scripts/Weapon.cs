using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class Weapon : MonoBehaviour
    {
        /// <summary>
        /// Unique identifier for this weapon
        /// </summary>
        /// 
        public Sprite icon;
        public int id = 0;  // Made private with SerializeField
        public Image filler;
        /// <summary>
        /// Audio source component for weapon sound effects
        /// </summary>
        [SerializeField] private AudioSource weaponAudio;  // Renamed to follow naming convention

        [SerializeField] private AudioClip blastSound;

        public List<Transform> FirePoints = new List<Transform>();  // Renamed for consistency

        [SerializeField] private GameObject rangeIndicator;

        [SerializeField] private bool canPlayMultipleTimes;

        public GameObject bullets;  // Renamed for clarity
        public Entity entity;  // Fixed typo "enity" -> "entity"
        public Player_Movement playermoemet;
        public GameController gameManager;
        public float firerate;  // Fixed typo "firerate" -> "fireRate"

        private float nextFireTime;  // Added to implement fire rate

        private void Start()
        {

            // Validate essential references
            if (gameManager == null || bullets == null)
            {
                gameManager = GameController.instace;

                //Debug.LogError($"{gameObject.name}: Required references missing (GameManager or Bullet Prefab)");
                //enabled = false;
                //return;
            }

            // Initialize object pool
            gameManager.Objectpool.CreatePool(bullets.name, bullets, 50, gameManager.BulletsHolder);

            // Validate audio source
            if (weaponAudio == null)
            {
                weaponAudio = GetComponent<AudioSource>();
                if (weaponAudio == null)
                {
                    Debug.LogWarning($"{gameObject.name}: No AudioSource found");
                }
            }

        }

        private void Update()
        {
            //if (entity?.Enemy != null)  // Added null-conditional operator
            //{
            //    Vector3 targetPosition = new Vector3(
            //        entity.Enemy.transform.position.x,
            //        transform.position.y,
            //        entity.Enemy.transform.position.z
            //    ) + Vector3.forward;
            //    foreach (Transform firePoint in FirePoints)  // Changed to foreach for better readability
            //    {
            //     //   firePoint.LookAt(targetPosition);
            //    }
            //}
        }

        // Added firing method
        private void Fire()
        {
            foreach (Transform firePoint in FirePoints)
            {

                GameObject bullet = gameManager.Objectpool.GetFromPool(bullets.name, firePoint.position, firePoint.rotation, gameManager.BulletsHolder);
                if (bullet != null)
                {
                    bullet.transform.SetPositionAndRotation(firePoint.position, firePoint.rotation);
                    bullet.SetActive(true);

                    if (weaponAudio != null && blastSound != null)
                    {
                        if (canPlayMultipleTimes)
                        {
                            weaponAudio.PlayOneShot(blastSound);
                        }
                        else if (!weaponAudio.isPlaying)
                        {
                            weaponAudio.PlayOneShot(blastSound);
                        }
                    }
                }
            }
        }

        // Optional: Visualize fire points in editor
        private void OnDrawGizmos()
        {

            Gizmos.color = Color.red;

            foreach (Transform firePoint in FirePoints)
            {
                if (firePoint != null)
                {
                    Gizmos.DrawSphere(firePoint.position, 0.1f);
                }
            }
        }
    }
}