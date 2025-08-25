using UnityEngine;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class DeathParticle : MonoBehaviour
    {
        [Space(10)]
        [Header("Main camera")]
        Camera cameraMain; // Main camera object for look there

        private void Start()
        {
            cameraMain = Camera.main; // Assigning the value
        }

        private void Update()
        {
            transform.LookAt(cameraMain.transform.position); // Saw the camera continusoly
        }
    }
}