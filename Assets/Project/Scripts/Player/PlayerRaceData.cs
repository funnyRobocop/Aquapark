using Fusion;
using System.Collections;
using UnityEngine;
using VContainer;

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

        [SerializeField] private PlayerGrab _playerGrab;
        [SerializeField] private PlayerView _playerView;

        [Inject] private CameraManager _cameraManager;
        [Inject] private VFXManager _vfxManager;

        public string DisplayName =>
            PlayerName.Length > 0 ? PlayerName.ToString() : $"Player {StartingPointIndex + 1}";

        public override void Spawned()
        {
            if (!HasStateAuthority)
                return;

            // Замораживаем физику, пока не телепортируемся на стартовую точку.
            // Иначе игрок падает сквозь пол пока ждёт загрузки InGameManager.
            FreezePhysics(true);

            CheckpointPosition = transform.position;
            CheckpointRotation = transform.rotation;

            if (InGameManager.Instance != null)
            {
                // InGameManager уже готов — сразу регистрируемся
                InGameManager.Instance.RPC_PlayerJoined(Id);
            }
            else
            {
                // Аддитивная сцена ещё не загрузила InGameManager — ждём
                StartCoroutine(WaitForInGameManager());
            }
        }

        // Включает/выключает физику. isKinematic=true — игрок висит на месте.
        private void FreezePhysics(bool freeze)
        {
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
                rb.isKinematic = freeze;
        }

        private IEnumerator WaitForInGameManager()
        {
            // Ждём появления InGameManager (сцена level грузится аддитивно)
            float timeout = 10f;
            float elapsed = 0f;
            while (InGameManager.Instance == null && elapsed < timeout)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (InGameManager.Instance == null)
            {
                Debug.LogError("[PlayerRaceData] InGameManager not found after 10s. " +
                    "Check that InGameManager is on the additive level scene.");
                yield break;
            }

            InGameManager.Instance.RPC_PlayerJoined(Id);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            // Гарантируем kinematic только пока ждём старта
            if (InGameManager.Instance != null &&
                InGameManager.Instance.gameState == InGameManager.GameState.Waiting)
            {
                var rb = GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                    rb.isKinematic = true;
            }

            UpdateOnStartPoint();
        }

        private void UpdateOnStartPoint()
        {
            if (InGameManager.Instance == null || StartingPointIndex < 0)
            {
                OnStartPoint = false;
                return;
            }

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
            {
                Debug.LogWarning($"[PlayerRaceData] Cannot teleport: Instance={InGameManager.Instance != null}, Index={StartingPointIndex}");
                return;
            }

            var points = InGameManager.Instance.startingPoints;
            if (points == null || StartingPointIndex >= points.Length || points[StartingPointIndex] == null)
            {
                Debug.LogError($"[PlayerRaceData] Invalid starting point index {StartingPointIndex}. Points count = {points?.Length ?? 0}");
                return;
            }

            var point = points[StartingPointIndex];
            Debug.Log($"[PlayerRaceData] Teleporting to starting point {StartingPointIndex} at {point.SpawnPosition}");
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

            _playerGrab?.DropHeldItem();
            TeleportTo(CheckpointPosition, CheckpointRotation);
        }

        private void TeleportTo(Vector3 pos, Quaternion rot)
        {
            var rb = GetComponent<Rigidbody>();
            var nt = GetComponent<NetworkTransform>();

            // 1. Останавливаем физику и делаем kinematic для чистого телепорта
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.position = pos;
                rb.rotation = rot;
            }

            // 2. Телепорт сетевого и локального transform
            if (nt != null)
                nt.Teleport(pos, rot);

            transform.SetPositionAndRotation(pos, rot);

            // 3. Решаем, должен ли игрок оставаться kinematic
            //    Только в Waiting — стоим на старте. Во время Game/EndGame — бегаем.
            bool keepKinematic = InGameManager.Instance != null &&
                                 InGameManager.Instance.gameState == InGameManager.GameState.Waiting;

            if (rb != null)
                rb.isKinematic = keepKinematic;

            _cameraManager?.ResetPlayerCameraPos();
            _vfxManager?.PlaySalutVFX(pos + Vector3.up);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_ApplyPush(Vector3 force)
        {
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
                return;

            rb.AddForce(force, ForceMode.Impulse);

            _playerView.PlayStunVFX();
        }
    }
}
