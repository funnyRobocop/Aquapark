using UnityEngine;
using VContainer;

namespace NonameGame
{
    public class FinishTrigger : MonoBehaviour
    {
        [Inject] private VFXManager _vfxManager;
        [Inject] private SoundManager _soundManager;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            var player = other.GetComponentInParent<PlayerRaceData>();
            if (player == null || player.HasFinished)
                return;

            if (InGameManager.Instance == null)
                return;

            // RequestFinish сам решает: если мы хост — сразу RegisterFinish,
            // иначе шлёт RPC на хоста. Не нужно проверять HasStateAuthority игрока.
            InGameManager.Instance.RequestFinish(player);

            _vfxManager?.PlaySalutVFX(new Vector3(
                player.transform.position.x,
                player.transform.position.y + 1f,
                player.transform.position.z));

            _soundManager?.PlayPopSound();
        }
    }
}
