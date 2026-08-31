using UnityEngine;

namespace NonameGame
{
    public class KillZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            var player = other.GetComponentInParent<PlayerRaceData>();
            if (player == null)
                return;

            if (!player.HasStateAuthority)
                return;

            if (InGameManager.Instance != null &&
                InGameManager.Instance.gameState == InGameManager.GameState.ShowResults)
                return;

            player.RespawnAtCheckpoint();
        }
    }
}
