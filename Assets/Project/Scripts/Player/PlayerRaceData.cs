using Fusion;
using UnityEngine;

namespace NonameGame
{
    public class PlayerRaceData : NetworkBehaviour
    {
        [Networked] public int StartingPointIndex { get; set; } = -1;
        [Networked] public NetworkBool OnStartPoint { get; set; }
        [Networked] public NetworkBool HasFinished { get; set; }
        [Networked] public int FinishPlace { get; set; }
        [Networked] public NetworkString<_32> PlayerName { get; set; }

        // Чекпоинт
        [Networked] public Vector3 CheckpointPosition { get; set; }
        [Networked] public Quaternion CheckpointRotation { get; set; }

        public string DisplayName =>
            PlayerName.Length > 0 ? PlayerName.ToString() : $"Player {StartingPointIndex + 1}";

        public override void Spawned()
        {
            // Стартовый чекпоинт = место спавна
            if (HasStateAuthority)
            {
                CheckpointPosition = transform.position;
                CheckpointRotation = transform.rotation;
            }
        }

        public void SetCheckpoint(Vector3 pos, Quaternion rot)
        {
            if (!HasStateAuthority)
                return;

            CheckpointPosition = pos;
            CheckpointRotation = rot;
        }

        public void RespawnAtCheckpoint()
        {
            if (!HasStateAuthority)
                return;

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = CheckpointPosition;
                rb.rotation = CheckpointRotation;
            }
            else
            {
                transform.SetPositionAndRotation(CheckpointPosition, CheckpointRotation);
            }
        }
    }
}
