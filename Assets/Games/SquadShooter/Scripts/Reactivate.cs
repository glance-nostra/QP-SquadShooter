using UnityEngine;
using System.Collections;
using Fusion;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class Reactivate : NetworkBehaviour
    {
        public GameController manager;
        public Vector3 minimum, maximum;
        bool insidesafezone;
        [SerializeField] private GameObject[] hiding_object;
        private NetworkTransform nettrasform;
        public bool wepon;
        public bool deactive;
        [SerializeField] private Transform[] spawinpoint;
        private float spawnTime;


        private void Start()
        {
            spawnTime = Time.time;

            nettrasform = GetComponent<NetworkTransform>();
            manager = GameController.instace;
        }
        public void activateObhect(bool active)
        {
            foreach (var item in hiding_object)
            {
                item.SetActive(active);
            }
        }
        public IEnumerator reacrivate()
        {
            deactive = true;

            activateObhect(false);
            if (Object && Object.HasStateAuthority)
            {

                nettrasform.Teleport(spawinpoint[Random.Range(0, spawinpoint.Length)].position);

            }
            yield return new WaitForSeconds(10);


            this.gameObject.SetActive(true);
            if (Object && Object.HasStateAuthority)
            {
                deactive = false;
                hasTeleported = false;
            }
            spawnTime = Time.time;
            activateObhect(true);
        }

        public bool hasTeleported;
        private void Update()
        {
            if (!Object || !Object.HasStateAuthority)
                return;
            //if (!insidesafezone && Object && Object.HasStateAuthority)
            //{
            //    activateObhect(false);

            //    nettrasform.Teleport(spawinpoint[Random.Range(0, spawinpoint.Length)].position);
            //    //  nettrasform.Teleport(new Vector3(Random.Range(minimum.x, maximum.x), transform.position.y, Random.Range(minimum.z, maximum.z)));

            //}
            //else
            //{

            //}
            if (!insidesafezone && !hasTeleported)
            {
                hasTeleported = true;
                Debug.Log("Flickeing");
                activateObhect(false);
                StartCoroutine(reacrivate());
            }
        }
        //public void Relocating()
        //{
        //    spawnTime = Time.time;
        //    activateObhect(false);
        //    if (Object && Object.HasStateAuthority)
        //    {
        //        nettrasform.Teleport(spawinpoint[Random.Range(0, spawinpoint.Length)].position);
        //    }
        //    insidesafezone = false;

        //}
        private void OnTriggerEnter(Collider other)
        {
            if (!Object)
                return;

            //if (other.GetComponent<Reactivate>())
            //{
            //    if (spawnTime > other.GetComponent<Reactivate>().spawnTime)
            //    {
            //        StartCoroutine(reacrivate());

            //    }
            //}
            //if (other.CompareTag("Obstacle"))
            //{
            //    activateObhect(false);
            //    insidesafezone = false;
            //    nettrasform.Teleport(new Vector3(Random.Range(minimum.x, maximum.x), transform.position.y, Random.Range(minimum.z, maximum.z)));

            //}

            if (other.GetComponent<SafeZone>() && !deactive)
            {
                insidesafezone = true;
                spawnTime = Time.time;


            }






        }
        public void OnPlayerLeft(PlayerRef player)
        {
            // If the player who left had authority over this object...
            if (Object && Object.StateAuthority == player)
            {
                Debug.Log($"Player {player} left. Reassigning authority.");

                // Pick another player (e.g., the first active one)
                foreach (var p in Runner.ActivePlayers)
                {
                    if (p != player)
                    {
                        Object.RequestStateAuthority();
                        Debug.Log($"Assigned StateAuthority to {p}");
                        break;
                    }
                }
            }
        }
        private void OnTriggerStay(Collider other)
        {
            if (!Object || !Object.HasStateAuthority)
                return;
            if (other.GetComponent<SafeZone>() && !deactive)
            {
                insidesafezone = true;

                //activateObhect(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!Object || !Object.HasStateAuthority)
                return;
            if (other.GetComponent<SafeZone>())
            {
                insidesafezone = false;
                activateObhect(false);
                if (Object.HasInputAuthority)
                    nettrasform.Teleport(spawinpoint[Random.Range(0, spawinpoint.Length)].position);

            }
        }
    }
}