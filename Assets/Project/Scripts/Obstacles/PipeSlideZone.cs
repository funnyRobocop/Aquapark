using Fusion;
using UnityEngine;

namespace NonameGame
{
    public class PipeSlideZone : MonoBehaviour
    {
        [Header("Spline (мировые точки по порядку сверху вниз)")]
        [SerializeField] private Transform[] splinePoints;

        [Header("Timing")]
        [SerializeField][Range(0.15f, 1f)] private float duration = 0.85f;

        [Header("Optional")]
        [SerializeField] private bool drawGizmos = true;

        public Vector3[] GetPathWorldPositions()
        {
            if (splinePoints == null || splinePoints.Length < 2)
                return null;

            var path = new Vector3[splinePoints.Length];
            for (int i = 0; i < splinePoints.Length; i++)
            {
                path[i] = splinePoints[i] != null
                    ? splinePoints[i].position
                    : transform.position;
            }
            return path;
        }

        public float Duration => duration;

        private void OnTriggerEnter(Collider other)
        {
            var slide = other.GetComponentInParent<PlayerPipeSlide>();
            if (slide == null)
                return;

            if (!slide.HasStateAuthority)
                return;

            if (slide.IsSliding)
                return;

            Vector3[] path = GetPathWorldPositions();
            if (path == null || path.Length < 2)
            {
                Debug.LogWarning($"[PipeSlideZone] {name}: нужно минимум 2 точки сплайна", this);
                return;
            }

            slide.RequestSlide(path, duration);
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || splinePoints == null || splinePoints.Length < 2)
                return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < splinePoints.Length - 1; i++)
            {
                if (splinePoints[i] == null || splinePoints[i + 1] == null)
                    continue;
                Gizmos.DrawLine(splinePoints[i].position, splinePoints[i + 1].position);
                Gizmos.DrawSphere(splinePoints[i].position, 0.12f);
            }

            if (splinePoints[splinePoints.Length - 1] != null)
                Gizmos.DrawSphere(splinePoints[splinePoints.Length - 1].position, 0.12f);
        }
    }
}