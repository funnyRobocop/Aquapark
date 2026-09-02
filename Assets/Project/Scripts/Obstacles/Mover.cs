using UnityEngine;


namespace NonameGame
{
    public class Mover : MonoBehaviour
    {
        [Header("Настройки перемещения")]
        [SerializeField] private Vector3 moveOffset = new Vector3(0f, 0f, 3f);
        [SerializeField] private float speed = 0.5f;
        [SerializeField] private float timeOffset = 0f;

        [Header("Visual (опционально)")]
        [Tooltip("Если задан — сглаживается только меш, коллайдер остаётся на корне")]
        [SerializeField] private Transform visualMesh;

        private Vector3 _startWorldPos;
        private Vector3 _prevPos;
        private Vector3 _currentPos;
        private Rigidbody _rb;

        private void Awake()
        {
            _startWorldPos = transform.position;
            _prevPos = _startWorldPos;
            _currentPos = _startWorldPos;

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

            Vector3 nextPos = _startWorldPos + moveOffset * smooth;

            _prevPos = _currentPos;
            _currentPos = nextPos;

            // Логика / коллайдер — на фиксированном тике
            if (_rb != null)
                _rb.MovePosition(_currentPos);
            else
                transform.position = _currentPos;

            // Если visual вынесен отдельно, корень уже на _currentPos
            // меш сгладим в LateUpdate
            if (visualMesh == null)
            {
                // если меша нет, сглаживание корня сделаем в LateUpdate
            }
        }

        private void LateUpdate()
        {
            float alpha = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
            alpha = Mathf.Clamp01(alpha);

            Vector3 visualPos = Vector3.Lerp(_prevPos, _currentPos, alpha);

            if (visualMesh != null)
            {
                visualMesh.position = visualPos;
            }
            else
            {
                // Сглаживаем весь объект (удобно для платформ)
                // Коллайдер в FixedUpdate уже на _currentPos, визуально чуть интерполируем
                transform.position = visualPos;
            }
        }
    }
}
