using UnityEngine;

namespace ithappy
{
    public class OscillateRotation : MonoBehaviour
    {
        public Vector3 rotationAxis = Vector3.up;
        public float rotationAngle = 45f;
        public float duration = 2f;
        public bool useRandomDelay = false;
        public float maxRandomDelay = 1f;

        private Quaternion startRotation;
        private float timeElapsed = 0f;
        private bool isReversing = false;
        private float randomDelay = 0f;
        
        // Ссылка на физическое тело
        private Rigidbody _rigidbody;

        void Start()
        {
            startRotation = transform.rotation;
            
            // Автоматически находим Rigidbody
            _rigidbody = GetComponent<Rigidbody>();

            if (useRandomDelay)
            {
                randomDelay = Random.Range(0f, maxRandomDelay);
            }
        }

        void Update()
        {
            if (timeElapsed < randomDelay)
            {
                timeElapsed += Time.deltaTime;
                return;
            }

            float progress = (timeElapsed - randomDelay) / (duration / 2f);
            progress = Mathf.Clamp01(progress);

            progress = EaseInOut(progress);

            float currentAngle = rotationAngle * (isReversing ? (1 - progress) : progress);
            Quaternion currentRotation = startRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);

            if (_rigidbody != null)
            {
                _rigidbody.MoveRotation(currentRotation);
            }
            else
            {
                transform.rotation = currentRotation;
            }

            timeElapsed += Time.deltaTime;

            if (timeElapsed >= duration / 2f + randomDelay)
            {
                timeElapsed = randomDelay;
                isReversing = !isReversing;
            }
        }

        private float EaseInOut(float t)
        {
            return t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
        }
    }
}
