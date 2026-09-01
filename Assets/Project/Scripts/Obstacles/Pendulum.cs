using UnityEngine;


namespace NonameGame
{
    public class Pendulum : MonoBehaviour
    {
        [SerializeField] private float maxAngle = 45f;
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private float timeOffset = 0f;
        [SerializeField] private Vector3 rotationAxis = Vector3.forward;

        private Quaternion _startRotation;

        private void Awake()
        {
            _startRotation = transform.rotation;
        }

        private void FixedUpdate()
        {
            float t = SimulationTimeProvider.Instance != null
                ? SimulationTimeProvider.Time
                : Time.time;

            float angle = Mathf.Sin((t + timeOffset) * speed) * maxAngle;
            transform.rotation = _startRotation * Quaternion.AngleAxis(angle, rotationAxis.normalized);
        }
    }
}
