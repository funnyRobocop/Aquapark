using Fusion;
using UnityEngine;


namespace NonameGame
{
    public class PlayerPush : NetworkBehaviour
    {
        [Header("Push Settings")]
        [SerializeField] private float pushRadius = 1.7f;
        [SerializeField] private float pushForce = 18f;
        [SerializeField] private float pushUpForce = 3f;
        [SerializeField] private float cooldown = 0.5f;
        [SerializeField] private float pushAngle = 90f; // конус перед игроком, градусы
        [SerializeField] private LayerMask playerMask;
        [SerializeField] private LayerMask itemMask;

        private PlayerGrab playerGrab;

        [Header("References")]
        [SerializeField] private PlayerView _view;

        [Header("Optional Feedback")]
        [SerializeField] private AudioSource pushAudio; // можно пустым
        [SerializeField] private ParticleSystem pushVfx; // можно пустым

        [Networked] private TickTimer _cooldownTimer { get; set; }

        public bool IsOnCooldown => !_cooldownTimer.ExpiredOrNotRunning(Runner);

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            Debug.Log($"Pushing item1");
            if (!GetInput(out NetworkInputData data))
                return;

            Debug.Log($"Pushing item2");
            if (!data.PushPressed)
                return;

            Debug.Log($"Pushing item 3");
            if (!_cooldownTimer.ExpiredOrNotRunning(Runner))
                return;

            Debug.Log($"Pushing item 4");
            if (playerGrab != null && playerGrab.IsHolding)
                return;

            TryPush();
        }

        public override void Spawned()
        {
            playerGrab = GetComponent<PlayerGrab>();
        }

        private void TryPush()
        {
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            bool pushedAnyone = false;

            // --- Игроки ---
            Collider[] playerHits = Physics.OverlapSphere(origin, pushRadius, playerMask);
            foreach (var hit in playerHits)
            {
                if (hit.attachedRigidbody != null && hit.attachedRigidbody.gameObject == gameObject)
                    continue;

                var target = hit.GetComponent<PlayerRaceData>();
                if (target == null || target.Object == null)
                    continue;

                if (target.Object == Object)
                    continue;

                if (!IsInPushCone(forward, target.transform.position, out Vector3 dir))
                    continue;

                target.RPC_ApplyPush(dir * pushForce);
                pushedAnyone = true;
            }

            // --- Предметы ---
            Collider[] itemHits = Physics.OverlapSphere(origin, pushRadius, itemMask);
            foreach (var hit in itemHits)
            {
                Debug.Log($"Pushing item {hit.name}");
                var item = hit.GetComponent<ThrowableItem>();
                if (item == null || item.Object == null)
                    continue;

                if (item.IsHeld)
                    continue;

                if (!IsInPushCone(forward, item.transform.position, out Vector3 dir))
                    continue;

Debug.Log($"Pushing item {item.name} with dir {dir} and force {pushForce}");
                item.RPC_ApplyPush(dir * pushForce);
                pushedAnyone = true;
            }

            _cooldownTimer = TickTimer.CreateFromSeconds(Runner, cooldown);

            if (_view != null)
                _view.PlayPush();

            if (pushedAnyone)
                RPC_PlayPushFeedback();
        }

        private bool IsInPushCone(Vector3 forward, Vector3 targetPos, out Vector3 dir)
        {
            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.001f)
            {
                dir = forward;
                return false;
            }

            float angle = Vector3.Angle(forward, toTarget.normalized);
            if (angle > pushAngle * 0.5f)
            {
                dir = default;
                return false;
            }

            dir = toTarget.normalized;
            dir += Vector3.up * (pushUpForce / Mathf.Max(pushForce, 0.01f));
            dir.Normalize();
            return true;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayPushFeedback()
        {
            if (pushAudio != null)
                pushAudio.Play();

            if (pushVfx != null)
                pushVfx.Play();
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, pushRadius);

            // Визуализация конуса
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Quaternion left = Quaternion.Euler(0f, -pushAngle * 0.5f, 0f);
            Quaternion right = Quaternion.Euler(0f, pushAngle * 0.5f, 0f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + left * forward * pushRadius);
            Gizmos.DrawLine(origin, origin + right * forward * pushRadius);
        }
    }
}
