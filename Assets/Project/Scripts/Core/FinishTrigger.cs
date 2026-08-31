using Fusion;
using UnityEngine;

namespace NonameGame
{
    public class FinishTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            var player = other.GetComponentInParent<PlayerRaceData>();
            if (player == null)
                return;

            // Регистрируем только если у игрока есть State Authority
            // (в Shared Mode это локальный игрок), а логику места делает Master через RPC/HasStateAuthority менеджера
            if (!player.HasStateAuthority)
                return;

            if (InGameManager.Instance == null)
                return;

            // Просим менеджера засчитать финиш
            InGameManager.Instance.RPC_RequestFinish(player.Object.Id);
        }
    }
}
