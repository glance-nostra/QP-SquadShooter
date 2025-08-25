using UnityEngine;
using System.Collections;

namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class Health_Indocator : MonoBehaviour
    {
        bool isStartGoingUp; // Fing that it should start going up or not

        // Called on activation of object
        private void OnEnable()
        {
            // transform.localPosition = Vector3.zero; // Give position to 0
            isStartGoingUp = true; // Start going up
        }

        private void Update()
        {
            // Find that it should going up or not
            if (isStartGoingUp)
            {
                transform.position += Vector3.up * 1 * Time.deltaTime;// Move the text upward.
                                                                      //   transform.LookAt(Camera.main.transform.position); // Saw the camera
            }
        }

        // Called on deactivation of object
        private void OnDisable()
        {
            isStartGoingUp = false; // Stop going up
                                    // transform.localPosition = Vector3.zero; // Give position to 0
        }
    }
}