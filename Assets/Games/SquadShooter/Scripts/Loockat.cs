using UnityEngine;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class Loockat : MonoBehaviour
    {
        public Transform cameraTransform;
        public Transform target;
        public float ypos;
        void Start()
        {
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main.transform;

            }
            transform.parent = null;
        }

        void Update()
        {
            Vector3 targetPosition = transform.position + cameraTransform.forward;
            Quaternion lookRotation = Quaternion.LookRotation(targetPosition - transform.position);
            transform.rotation = Quaternion.Euler(lookRotation.eulerAngles.x, lookRotation.eulerAngles.y, 0f);

            if (target)
                transform.position = target.position + new Vector3(0, ypos, 0);
        }
    }
}