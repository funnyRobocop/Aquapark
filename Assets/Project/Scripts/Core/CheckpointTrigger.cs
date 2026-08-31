using System.Collections.Generic;
using UnityEngine;

namespace NonameGame
{
    public class CheckpointTrigger : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint; // куда ставить игрока (может быть этот же объект)

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
        }
    }
}
