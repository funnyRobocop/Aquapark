using UnityEngine;

namespace NonameGame
{
    public class Mover : MonoBehaviour
    {
        [Header("Настройки перемещения")]
        [SerializeField] private Vector3 moveOffset = new Vector3(0f, 0f, 3f);
        [SerializeField] private float speed = 0.5f;
        [SerializeField] private float timeOffset = 0f;

        private Vector3 _startLocalPos;
        private Vector3 _prevLocalPos;
        private Vector3 _currentLocalPos;
        private Vector3 _visualBaseLocalPos;
        private Rigidbody _rb;

        private void Awake()
        {
            _startLocalPos = transform.localPosition;
            _prevLocalPos = _startLocalPos;
            _currentLocalPos = _startLocalPos;

            _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                _rb.isKinematic = true;
                _rb.useGravity = false;
            }
        }

        private void FixedUpdate()
        {
            float t = SimulationTimeProvider.Instance != null
                ? SimulationTimeProvider.Time
                : Time.time;

            float pingPong = Mathf.PingPong((t + timeOffset) * speed, 1f);
            float smooth = Mathf.SmoothStep(0f, 1f, pingPong);

            Vector3 nextLocal = _startLocalPos + moveOffset * smooth;

            _prevLocalPos = _currentLocalPos;
            _currentLocalPos = nextLocal;

            ApplyPosition(_currentLocalPos);
        }

        private void ApplyPosition(Vector3 localPos)
        {
            if (_rb != null)
            {
                Vector3 worldPos = transform.parent != null
                    ? transform.parent.TransformPoint(localPos)
                    : localPos;

                _rb.MovePosition(worldPos);
            }
        }
    }
}
