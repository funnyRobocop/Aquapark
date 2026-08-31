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

        [Networked] public Vector3 CheckpointPosition { get; set; }
        [Networked] public Quaternion CheckpointRotation { get; set; }

        public string DisplayName =>
            PlayerName.Length > 0 ? PlayerName.ToString() : $"Player {StartingPointIndex + 1}";

        public override void Spawned()
        {
            if (!HasStateAuthority)
                return;

            // Просим у менеджера свободную стартовую точку
            if (InGameManager.Instance != null)
                InGameManager.Instance.RPC_PlayerJoined(Id);

            // Временный чекпоинт, пока точка не назначена
            CheckpointPosition = transform.position;
            CheckpointRotation = transform.rotation;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            UpdateOnStartPoint();
        }

        private void UpdateOnStartPoint()
        {
            if (InGameManager.Instance == null || StartingPointIndex < 0)
            {
                OnStartPoint = false;
                return;
            }

            // Готовность только в Waiting
            if (InGameManager.Instance.gameState != InGameManager.GameState.Waiting)
            {
                OnStartPoint = false;
                return;
            }

            var points = InGameManager.Instance.startingPoints;
            if (points == null || StartingPointIndex >= points.Length || points[StartingPointIndex] == null)
            {
                OnStartPoint = false;
                return;
            }

            OnStartPoint = points[StartingPointIndex].IsPlayerInRange(transform.position);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_AssignStartingPoint(int index)
        {
            StartingPointIndex = index;
            TeleportToStartingPoint();
        }

        public void TeleportToStartingPoint()
        {
            if (!HasStateAuthority)
                return;

            if (InGameManager.Instance == null || StartingPointIndex < 0)
                return;

            var points = InGameManager.Instance.startingPoints;
            if (points == null || StartingPointIndex >= points.Length || points[StartingPointIndex] == null)
                return;

            var point = points[StartingPointIndex];
            TeleportTo(point.SpawnPosition, point.SpawnRotation);

            CheckpointPosition = point.SpawnPosition;
            CheckpointRotation = point.SpawnRotation;
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

            TeleportTo(CheckpointPosition, CheckpointRotation);
        }

        private void TeleportTo(Vector3 pos, Quaternion rot)
        {
            var rb = GetComponent<Rigidbody>();
            var nt = GetComponent<NetworkTransform>();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (nt != null)
            {
                nt.Teleport(pos, rot);
            }
            else if (rb != null)
            {
                rb.position = pos;
                rb.rotation = rot;
            }
            else
            {
                transform.SetPositionAndRotation(pos, rot);
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_ApplyPush(Vector3 force)
        {
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
                return;

            rb.AddForce(force, ForceMode.Impulse);
        }
    }
}
