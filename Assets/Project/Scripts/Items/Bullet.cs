using Fusion;
using UnityEngine;

namespace NonameGame
{
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 3f;
        [SerializeField] private float hitForce = 14f;
        [SerializeField] private float hitUpForce = 2.5f;

        private Rigidbody _rb;
        private Collider _col;
        private PlayerWeapon _ownerPool;
        private bool _active;
        private float _spawnTime;
        private PlayerRef _ownerPlayer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<Collider>();
        }

        public void Init(PlayerWeapon pool)
        {
            _ownerPool = pool;
            gameObject.SetActive(false);
        }

        public void Fire(Vector3 position, Quaternion rotation, Vector3 velocity, PlayerRef owner)
        {
            _ownerPlayer = owner;
            _active = true;
            _spawnTime = Time.time;

            transform.SetPositionAndRotation(position, rotation);
            gameObject.SetActive(true);

            if (_col != null)
                _col.enabled = true;

            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.linearVelocity = velocity;
                _rb.angularVelocity = Vector3.zero;
            }
        }

        private void Update()
        {
            if (!_active)
                return;

            if (Time.time - _spawnTime >= lifeTime)
                ReturnToPool();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_active)
                return;

            var race = collision.collider.GetComponentInParent<PlayerRaceData>();
            if (race != null)
            {
                if (race.Object != null && race.Object.InputAuthority == _ownerPlayer)
                    return;

                Vector3 dir = race.transform.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.001f)
                    dir = transform.forward;
                dir.Normalize();
                dir += Vector3.up * (hitUpForce / Mathf.Max(hitForce, 0.01f));
                dir.Normalize();

                race.RPC_ApplyPush(dir * hitForce);
                ReturnToPool();
                return;
            }

            ReturnToPool();
        }

        private void ReturnToPool()
        {
            if (!_active)
                return;

            _active = false;

            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }

            if (_col != null)
                _col.enabled = false;

            gameObject.SetActive(false);

            if (_ownerPool != null)
                _ownerPool.ReturnBullet(this);
        }
    }
}