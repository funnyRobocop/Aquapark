using UnityEngine;


namespace NonameGame
{
    public class Mover : MonoBehaviour
    {
        [Header("Настройки перемещения")]
        [SerializeField] private Vector3 moveOffset = new Vector3(0f, 0f, 3f);
        [SerializeField] private float speed = 0.5f;
        [SerializeField] private float timeOffset = 0f;

        private Vector3 _startWorldPos;
        private Rigidbody _rb;

        private void Awake()
        {
            _startWorldPos = transform.position;
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

            Vector3 targetPos = _startWorldPos + moveOffset * smooth;

            if (_rb != null)
                _rb.MovePosition(targetPos);
            else
                transform.position = targetPos;
        }
    }
}
