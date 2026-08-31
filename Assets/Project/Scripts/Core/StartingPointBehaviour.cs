using UnityEngine;


namespace NonameGame
{
    public class StartingPointBehaviour : MonoBehaviour
    {
        [Header("Ready Check")]
        [SerializeField] private float readyRadius = 1.5f;
        [SerializeField] private Vector3 readyOffset = Vector3.zero;

        public Vector3 SpawnPosition => transform.position + Vector3.up * 0.1f;
        public Quaternion SpawnRotation => transform.rotation;

        public bool IsPlayerInRange(Vector3 playerPosition)
        {
            Vector3 center = transform.position + readyOffset;
            return Vector3.Distance(playerPosition, center) <= readyRadius;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + readyOffset, readyRadius);
        }
    }
}
