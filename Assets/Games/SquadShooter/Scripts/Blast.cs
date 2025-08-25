using System.Collections;
using UnityEngine;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class Blast : MonoBehaviour
    {
        public int damangeamount;

        private void OnEnable()
        {

            StartCoroutine(waitfordeactivate());


        }

        public IEnumerator waitfordeactivate()
        {
            yield return new WaitForSeconds(.2f);
            transform.parent.gameObject.SetActive(false);
        }
        //private void OnTriggerStay(Collider collision)
        //{

        //    Entity entiy = collision.GetComponent<Entity>();
        //    if (entiy)
        //    {
        //        entiy.ReduceHeath(damangeamount,null);
        //    }      
        //}


    }
}