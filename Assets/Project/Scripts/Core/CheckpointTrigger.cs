using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace NonameGame
{
    public class CheckpointTrigger : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint; // куда ставить игрока (может быть этот же объект)

        [Inject] private VFXManager _vfxManager;
        
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            var player = other.GetComponentInParent<PlayerRaceData>();
            if (player == null)
                return;

            if (!player.HasStateAuthority)
                return;

            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

            player.SetCheckpoint(pos, rot);

            _vfxManager.PlaySalutVFX(new Vector3(player.transform.position.x,
                player.transform.position.y + 1f,
                player.transform.position.z));
        }
    }
}
