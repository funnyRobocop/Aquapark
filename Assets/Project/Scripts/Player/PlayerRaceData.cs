using Fusion;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
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

        /// <summary>Игрок нажал Enter в лобби — готов к старту.</summary>
        [Networked] public NetworkBool IsReady { get; set; }

        [SerializeField] private PlayerGrab _playerGrab;
        [SerializeField] private PlayerView _playerView;
        [SerializeField] private Transform cameraTarget;

        [Inject] private CameraManager _cameraManager;
        [Inject] private VFXManager _vfxManager;

        public string DisplayName =>
            PlayerName.Length > 0 ? PlayerName.ToString() : $"Player {StartingPointIndex + 1}";

        // ─────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────

        public override void Spawned()
        {
            if (!HasStateAuthority)
                return;

            // Игрок может ходить в лобби — не замораживаем.
            // Телепорт на стартовую позицию произойдёт через RPC_AssignStartingPoint.
            CheckpointPosition = transform.position;
            CheckpointRotation = transform.rotation;

            if (InGameManager.Instance != null)
                InGameManager.Instance.RPC_PlayerJoined(Id);
            else
                StartCoroutine(WaitForInGameManager());
        }

        private IEnumerator WaitForInGameManager()
        {
            float timeout = 10f;
            float elapsed = 0f;
            while (InGameManager.Instance == null && elapsed < timeout)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (InGameManager.Instance == null)
            {
                Debug.LogError("[PlayerRaceData] InGameManager not found after 10s.");
                yield break;
            }

            InGameManager.Instance.RPC_PlayerJoined(Id);
        }

        // ─────────────────────────────────────────────
        // Update — Enter для Ready (локально, только свой клиент)
        // ─────────────────────────────────────────────

        private void Update()
        {
            // Только свой клиент обрабатывает Enter
            if (!HasInputAuthority)
                return;

            if (InGameManager.Instance == null ||
                InGameManager.Instance.gameState != InGameManager.GameState.Waiting)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                RPC_SetReady(!IsReady); // тоггл: Enter готов/не готов
            }
        }

        // ─────────────────────────────────────────────
        // FixedUpdateNetwork
        // ─────────────────────────────────────────────

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

        // ─────────────────────────────────────────────
        // RPC: Ready / телепорт
        // ─────────────────────────────────────────────

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_SetReady(NetworkBool ready)
        {
            if (InGameManager.Instance == null ||
                InGameManager.Instance.gameState != InGameManager.GameState.Waiting)
                return;

            IsReady = ready;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_AssignStartingPoint(int index)
        {
            StartingPointIndex = index;
            IsReady = false;
            TeleportToStartingPoint();
            _cameraManager?.FlyingCameraStart();
            StartCoroutine(FlyCameraCrtn());
        }

        private IEnumerator FlyCameraCrtn()
        {
            yield return new WaitForSeconds(3f);

            _cameraManager?.InitForPlayer(cameraTarget);
        }

        /// <summary>
        /// Телепорт на стартовую позицию. Вызывается InGameManager при старте гонки.
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_TeleportToStart()
        {
            TeleportToStartingPoint();
        }

        // ─────────────────────────────────────────────
        // Телепорт
        // ─────────────────────────────────────────────

        public void TeleportToStartingPoint()
        {
            if (!HasStateAuthority) return;

            if (InGameManager.Instance == null || StartingPointIndex < 0)
            {
                Debug.LogWarning($"[PlayerRaceData] Cannot teleport: Manager={InGameManager.Instance != null}, Index={StartingPointIndex}");
                return;
            }

            var points = InGameManager.Instance.startingPoints;
            if (points == null || StartingPointIndex >= points.Length || points[StartingPointIndex] == null)
            {
                Debug.LogError($"[PlayerRaceData] Invalid starting point index {StartingPointIndex}");
                return;
            }

            var point = points[StartingPointIndex];
            TeleportTo(point.SpawnPosition, point.SpawnRotation);

            CheckpointPosition = point.SpawnPosition;
            CheckpointRotation = point.SpawnRotation;
        }

        public void SetCheckpoint(Vector3 pos, Quaternion rot)
        {
            if (!HasStateAuthority) return;
            CheckpointPosition = pos;
            CheckpointRotation = rot;
        }

        public void RespawnAtCheckpoint()
        {
            if (!HasStateAuthority) return;
            _playerGrab?.DropHeldItem();
            TeleportTo(CheckpointPosition, CheckpointRotation);
        }

        private void TeleportTo(Vector3 pos, Quaternion rot)
        {
            var rb = GetComponent<Rigidbody>();
            var nt = GetComponent<NetworkTransform>();

            // Останавливаем и телепортируем
            if (rb != null)
            {
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic     = true;
                rb.position        = pos;
                rb.rotation        = rot;
            }

            if (nt != null) nt.Teleport(pos, rot);
            transform.SetPositionAndRotation(pos, rot);

            // Всегда размораживаем после телепорта:
            // игрок может ходить и в лобби, и в гонке.
            if (rb != null) rb.isKinematic = false;

            _cameraManager?.ResetPlayerCameraPos();
            _vfxManager?.PlaySalutVFX(pos + Vector3.up);
        }

        // ─────────────────────────────────────────────
        // Прочее
        // ─────────────────────────────────────────────

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_ApplyPush(Vector3 force)
        {
            var rb = GetComponent<Rigidbody>();
            if (rb == null) return;
            rb.AddForce(force, ForceMode.Impulse);
            _playerView.PlayStunVFX();
        }
    }
}
