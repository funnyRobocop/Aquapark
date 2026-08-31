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

        [Header("Optional Feedback")]
        [SerializeField] private AudioSource pushAudio; // можно пустым
        [SerializeField] private ParticleSystem pushVfx; // можно пустым

        [Networked] private TickTimer _cooldownTimer { get; set; }

        public bool IsOnCooldown => !_cooldownTimer.ExpiredOrNotRunning(Runner);

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (!GetInput(out NetworkInputData data))
                return;

            if (!data.PushPressed)
                return;

            if (!_cooldownTimer.ExpiredOrNotRunning(Runner))
                return;

            TryPush();
        }

        private void TryPush()
        {
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Collider[] hits = Physics.OverlapSphere(origin, pushRadius, playerMask);
            bool pushedAnyone = false;

            foreach (var hit in hits)
            {
                if (hit.attachedRigidbody != null && hit.attachedRigidbody.gameObject == gameObject)
                    continue;

                var target = hit.GetComponentInParent<PlayerRaceData>();
                if (target == null || target.Object == null)
                    continue;

                if (target.Object == Object)
                    continue;

                // Вектор к цели
                Vector3 toTarget = target.transform.position - transform.position;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude < 0.001f)
                    continue;

                // Проверка конуса перед игроком
                float angle = Vector3.Angle(forward, toTarget.normalized);
                if (angle > pushAngle * 0.5f)
                    continue;

                Vector3 dir = toTarget.normalized;
                dir += Vector3.up * (pushUpForce / Mathf.Max(pushForce, 0.01f));
                dir.Normalize();

                target.RPC_ApplyPush(dir * pushForce);
                pushedAnyone = true;
            }

            _cooldownTimer = TickTimer.CreateFromSeconds(Runner, cooldown);
            
            if (pushedAnyone)
            {
                RPC_PlayPushFeedback();
            }
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
