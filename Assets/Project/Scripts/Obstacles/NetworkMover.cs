using UnityEngine;
using Fusion;

namespace NonameGame
{
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkMover : NetworkBehaviour
    {
        [Header("Настройки перемещения")]
        [SerializeField] private Vector3 moveOffset;
        [SerializeField] private float speed;
        [SerializeField] private float timeOffset;

        private Vector3 _startWorldPos;
        private Rigidbody _rigidbody;

        public override void Spawned()
        {
            _startWorldPos = transform.position;        
            _rigidbody = GetComponent<Rigidbody>();
        }

        public override void FixedUpdateNetwork()
        {
            if (_rigidbody == null) return;

            if (Runner.IsServer)
            {
                float syncedTime = Runner.SimulationTime + timeOffset;

                float pingPong = Mathf.PingPong(syncedTime * speed, 1f);
                float smoothPingPong = Mathf.SmoothStep(0f, 1f, pingPong);

                Vector3 targetWorldPosition = _startWorldPos + (moveOffset * smoothPingPong);
                
                _rigidbody.MovePosition(targetWorldPosition);
            }
        }
    }
}