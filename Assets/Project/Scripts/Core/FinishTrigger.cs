using Fusion;
using UnityEngine;
using VContainer;

namespace NonameGame
{
    public class FinishTrigger : MonoBehaviour
    {
        [Inject] private VFXManager _vfxManager;
        
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

            _vfxManager.PlaySalutVFX(new Vector3(player.transform.position.x,
                player.transform.position.y + 1f,
                player.transform.position.z));
        }
    }
}
