using System.Collections;
using UnityEngine;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class Bullet : MonoBehaviour
    {
        private Rigidbody rb;
        private Collider colider;
        [Space(10)]
        [Header("Bot manager")]
        public Entity entity_holder; // Shooter bot
        public bool small_bullets;

        public ObjectPoolManager objectPolling;
        [Space(10)]
        [Header("Paricle systems")]
        [SerializeField] private ParticleSystem wallHitParticle; // Particle for playing when hit wall
        [SerializeField] private ParticleSystem playerHitParicle; // Particle for playing when hit player/bot
        [SerializeField] private ParticleSystem projectile;

        [SerializeField] private ParticleSystem flash;

        [Space(10)]
        [Header("Damage for player/bot")]
        public int damageAmount; // Damage to player or bot
        public float bulletSpeed;

        public AudioSource hitaudio;
        public AudioClip obsticlehit, playerhit;
        bool ended;


        // Called on activation of object
        void OnEnable()
        {
            ended = false;
            projectile.gameObject.SetActive(true);
            colider.enabled = true;
            for (int i = 0; i < transform.childCount - 2; i++)
            {
                transform.GetChild(i).gameObject.SetActive(true);
            }

            StartCoroutine(OffBullet());
        }
        IEnumerator OffBullet()
        {
            projectile.gameObject.SetActive(false);
            wallHitParticle.gameObject.SetActive(false);
            wallHitParticle.Stop();
            flash.Play();
            yield return new WaitForSeconds(.1f);
            if (!ended)
            {
                //flash.gameObject.SetActive(false);
                projectile.gameObject.SetActive(true);
                projectile.Play();
                Debug.Log("Firing");

                Vector3 direction = transform.forward;

                rb.linearVelocity = direction * bulletSpeed;
                yield return new WaitForSeconds(2f);
                ended = true;
                rb.linearVelocity = Vector3.zero;
                entity_holder.gameManager.Objectpool.ReturnToPool(entity_holder.allCollectedWepon[0].bullets.name, this.gameObject);
            }
        }

        //public IEnumerator Fireing()
        //{
        //    Debug.Log("Firin11g");

        //}
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            colider = GetComponent<Collider>();
        }


        private void OnTriggerEnter(Collider collision)
        {
            if (entity_holder == null ||
                collision.gameObject == entity_holder.gameObject ||
                collision.transform.GetComponent<Bullet>() ||
                collision.gameObject.name == "Magic circle" ||
                collision.GetComponent<Grass>() || collision.gameObject.name.Contains("water")
                || collision.GetComponent<Reactivate>())
                return;

            this.transform.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            Vector3 pos = this.transform.position;
            ended = true;
            Entity player = collision.GetComponent<Entity>();
            if (player && collision.gameObject != entity_holder)
            {
                playerHitParicle.transform.position = pos;

                // hitaudio.PlayOneShot(playerhit);
                StartCoroutine(GoParentAfterParticle(playerHitParicle));
                if (player.Object.HasStateAuthority)
                {
                    player.RPC_ReduceHeath(damageAmount, entity_holder);
                }
                //player hit
            }
            else if (collision.gameObject != entity_holder)
            {
                //wall hit

                wallHitParticle.transform.position = pos;
                // hitaudio.PlayOneShot(obsticlehit);
                StartCoroutine(GoParentAfterParticle(wallHitParticle));
            }







        }

        private void OnTriggerExit(Collider collision)
        {
            if (entity_holder == null ||
                collision.gameObject == entity_holder.gameObject ||
                collision.transform.GetComponent<Bullet>() ||
                collision.gameObject.name == "Magic circle" || collision.GetComponent<Reactivate>() ||
                collision.gameObject.name.Contains("water") || collision.gameObject.name.Contains("wepon"))
                return;

            this.transform.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            Vector3 pos = this.transform.position;

            if (collision.GetComponent<Entity>() && collision.gameObject != entity_holder)
            {
                //playerHitParicle.transform.position = pos;

                // hitaudio.PlayOneShot(playerhit);
                //  StartCoroutine(GoParentAfterParticle(playerHitParicle));
                /// collision.GetComponent<Entity>().ReduceHeath(damageAmount, entity_holder);
                //player hit
            }
            else if (collision.gameObject != entity_holder)
            {
                ///wall hit
                // Debug.Log("object hitting");
                //wallHitParticle.transform.position = pos;
                // hitaudio.PlayOneShot(obsticlehit);
                // StartCoroutine(GoParentAfterParticle(wallHitParticle));
            }




            Debug.Log("Deactivating bullets");
        }

        // Playing particle when hit anything
        IEnumerator GoParentAfterParticle(ParticleSystem particleType)
        {


            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
            particleType.gameObject.SetActive(true);
            particleType.Play();
            //need to deactivate eeveertything
            rb.linearVelocity = Vector3.zero;
            projectile.gameObject.SetActive(false);
            colider.enabled = false;

            // hitaudio.Play();
            yield return new WaitForSeconds(2f);
            entity_holder.gameManager.Objectpool.ReturnToPool(entity_holder.allCollectedWepon[0].bullets.name, this.gameObject);
            // gameManager.Objectpool.ReturnToPool(Bullets.name, bulletObj);
        }
    }
}