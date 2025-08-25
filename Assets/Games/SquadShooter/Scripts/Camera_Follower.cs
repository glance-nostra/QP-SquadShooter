using UnityEngine;
using TMPro;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class Camera_Follower : MonoBehaviour
    {
        public Transform player;
        public Vector3 offset;
        public float minX, maxX, minY, maxY;
        public float maxSpeed = 10f;
        public float slowDownDistance = 3f;

        private Vector3 targetPosition;
        private Vector3 shakeOffset = Vector3.zero;
        private bool isShaking = false;

        [Header("Camera Shake")]
        public float shakeDuration = 0.25f;
        public float shakeMagnitude = 0.1f;

        public TextMeshProUGUI XValue, YValue, ZValue;

        public void SetTarget(Transform target)
        {
            player = target;
        }

        public void Fire()
        {
            // StartCoroutine(Shake()); // Optionally trigger shake
        }

        public void changeX(float value)
        {
            offset.x = value;
            XValue.text = value.ToString("F2");
        }

        public void changeY(float value)
        {
            offset.y = value;
            YValue.text = value.ToString("F2");
        }

        public void changeZ(float value)
        {
            offset.z = value;
            ZValue.text = value.ToString("F2");
        }

        void LateUpdate()
        {
            if (player == null) return;

            targetPosition = player.position + offset;
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.z = Mathf.Clamp(targetPosition.z, minY, maxY);

            float speed = Mathf.Lerp(0, maxSpeed, 1 - Mathf.InverseLerp(minX + slowDownDistance, maxX - slowDownDistance, targetPosition.x));
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            if (isShaking)
            {
                transform.position += shakeOffset;
            }
        }
    }
}