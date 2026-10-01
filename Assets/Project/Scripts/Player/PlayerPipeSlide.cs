using System.Collections;
using Fusion;
using UnityEngine;

namespace NonameGame
{
    public class PlayerPipeSlide : NetworkBehaviour
    {
        [SerializeField] private PlayerController controller;

        private Rigidbody _rb;
        private NetworkTransform _nt;
        private Coroutine _slideRoutine;
        private bool _isSliding;

        public bool IsSliding => _isSliding;

        public override void Spawned()
        {
            if (controller == null)
                controller = GetComponent<PlayerController>();
            _rb = GetComponent<Rigidbody>();
            _nt = GetComponent<NetworkTransform>();
        }

        public void RequestSlide(Vector3[] path, float duration)
        {
            if (!HasStateAuthority)
                return;
            if (_isSliding)
                return;
            if (path == null || path.Length < 2)
                return;

            duration = Mathf.Clamp(duration, 0.15f, 1f);
            RPC_StartSlide(path, duration);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_StartSlide(Vector3[] path, float duration)
        {
            if (_isSliding)
                return;
            if (path == null || path.Length < 2)
                return;

            if (_slideRoutine != null)
                StopCoroutine(_slideRoutine);

            _slideRoutine = StartCoroutine(SlideLocal(path, duration));
        }

        private IEnumerator SlideLocal(Vector3[] path, float duration)
        {
            _isSliding = true;

            if (controller != null)
                controller.SetStunTimer(duration + 0.15f);

            bool wasKinematic = false;
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                wasKinematic = _rb.isKinematic;
                _rb.isKinematic = true;
            }

            bool ntWasEnabled = false;
            if (_nt != null)
            {
                ntWasEnabled = _nt.enabled;
                _nt.enabled = false;
            }

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(duration, 0.01f);
                float u = Mathf.Clamp01(t);
                float eased = u * u * (3f - 2f * u);

                Vector3 pos = EvaluatePolyline(path, eased);
                Quaternion look = EvaluateLook(path, eased);

                transform.SetPositionAndRotation(pos, look);
                if (_rb != null)
                {
                    _rb.position = pos;
                    _rb.rotation = look;
                }

                yield return null;
            }

            Vector3 end = path[path.Length - 1];
            Vector3 endFwd = path.Length >= 2
                ? (path[path.Length - 1] - path[path.Length - 2]).normalized
                : transform.forward;
            if (endFwd.sqrMagnitude < 0.001f)
                endFwd = Vector3.forward;
            Quaternion endRot = Quaternion.LookRotation(endFwd, Vector3.up);

            transform.SetPositionAndRotation(end, endRot);
            if (_rb != null)
            {
                _rb.position = end;
                _rb.rotation = endRot;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = wasKinematic;
            }

            if (_nt != null)
            {
                _nt.enabled = ntWasEnabled;
                if (HasStateAuthority)
                    _nt.Teleport(end, endRot);
            }

            _isSliding = false;
            _slideRoutine = null;
        }

        private static Vector3 EvaluatePolyline(Vector3[] path, float t)
        {
            t = Mathf.Clamp01(t);
            if (path.Length == 1)
                return path[0];

            float total = 0f;
            for (int i = 0; i < path.Length - 1; i++)
                total += Vector3.Distance(path[i], path[i + 1]);

            if (total < 0.0001f)
                return path[0];

            float target = t * total;
            float acc = 0f;
            for (int i = 0; i < path.Length - 1; i++)
            {
                float seg = Vector3.Distance(path[i], path[i + 1]);
                if (acc + seg >= target || i == path.Length - 2)
                {
                    float localT = seg > 0.0001f ? (target - acc) / seg : 1f;
                    return Vector3.Lerp(path[i], path[i + 1], Mathf.Clamp01(localT));
                }
                acc += seg;
            }

            return path[path.Length - 1];
        }

        private static Quaternion EvaluateLook(Vector3[] path, float t)
        {
            Vector3 a = EvaluatePolyline(path, t);
            Vector3 b = EvaluatePolyline(path, Mathf.Min(1f, t + 0.02f));
            Vector3 dir = b - a;
            if (dir.sqrMagnitude < 0.0001f)
                return Quaternion.identity;
            return Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}