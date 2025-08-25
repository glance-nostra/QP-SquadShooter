using UnityEngine;
using Fusion;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class SafeZone : NetworkBehaviour
    {
        public float speedreduceing;
        public Vector3 startingsclae;
        public bool start;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {


        }
        private void FixedUpdate()
        {

            if (!start)
                return;
            if (Object.HasStateAuthority)
            {
                transform.localScale -= new Vector3(speedreduceing * Time.deltaTime, 0, speedreduceing * Time.deltaTime);
            }
        }
        private void OnTriggerExit(Collider other)
        {

            if (other.GetComponent<Entity>())
            {
                Debug.Log("Heath Reducing");
                other.GetComponent<Entity>().RPC_ReduceHeath(1000, null);
            }



        }
    }
}