using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace NonameGame
{
    public class PlayerRaceData : NetworkBehaviour
    {
        [Networked] public int           StartingPointIndex { get; set; } = -1;
        [Networked] public NetworkBool   OnStartPoint       { get; set; }
        [Networked] public int           FinishPlace        { get; set; }
        [Networked] public NetworkString<_32> PlayerName    { get; set; }
        [Networked] public Vector3       CheckpointPosition { get; set; }
        [Networked] public Quaternion    CheckpointRotation { get; set; }

        /// <summary>Игрок нажал Enter — готов к старту.</summary>
        [Networked, OnChangedRender(nameof(OnIsReadyChanged))]
        public NetworkBool IsReady { get; set; }

        /// <summary>Игрок финишировал — скрыть и переключить камеру.</summary>
        // HasFinished теперь напрямую триггерит OnChangedRender на всех клиентах

        /// <summary>Управляет видимостью аватара для всех клиентов.</summary>
        [Networked, OnChangedRender(nameof(OnHasFinishedChanged))]
        public NetworkBool HasFinished { get; set; }

        [Networked, OnChangedRender(nameof(OnIsVisibleChanged))]
        public NetworkBool IsVisible { get; set; } = true;

        [SerializeField] private PlayerGrab  _playerGrab;
        [SerializeField] private PlayerView  _playerView;
        [SerializeField] private Transform   _cameraTarget;      // Follow-target для CinemachineCamera

        /// <summary>Follow-target для Cinemachine, доступен снаружи.</summary>
        public Transform CameraTarget => _cameraTarget;

        [Inject] private CameraManager _cameraManager;
        [Inject] private VFXManager    _vfxManager;

        // Кэш Renderer-ов для быстрого вкл/выкл видимости
        private Renderer[] _renderers;

        public string DisplayName =>
            PlayerName.Length > 0 ? PlayerName.ToString() : $"Player {StartingPointIndex + 1}";

        // ─────────────────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        public override void Spawned()
        {
            if (!HasStateAuthority) return;

            CheckpointPosition = transform.position;
            CheckpointRotation = transform.rotation;
            IsVisible          = true;

            if (InGameManager.Instance != null)
            {
                // Опоздавший — сразу в режим спектатора если гонка уже идёт
                if (HasInputAuthority &&
                    InGameManager.Instance.gameState != InGameManager.GameState.Waiting)
                {
                    StartCoroutine(SetupLateJoinerSpectator());
                    return;
                }

                InGameManager.Instance.RPC_PlayerJoined(Id);
            }
            else
            {
                StartCoroutine(WaitForInGameManager());
            }
        }

        private IEnumerator WaitForInGameManager()
        {
            float timeout = 10f, elapsed = 0f;
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

            if (HasInputAuthority &&
                InGameManager.Instance.gameState != InGameManager.GameState.Waiting)
            {
                StartCoroutine(SetupLateJoinerSpectator());
                yield break;
            }

            InGameManager.Instance.RPC_PlayerJoined(Id);
        }

        /// <summary>Опоздавший игрок: ожидаем кадр и входим в режим наблюдателя.</summary>
        private IEnumerator SetupLateJoinerSpectator()
        {
            yield return null; // ожидаем кадр инициализации

            // Скрываем своёго игрока (он не участвует в гонке)
            SetRenderersEnabled(false);

            InGameManager.Instance?.BeginSpectatingForLateJoiner(this);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Update — Enter для Ready
        // ─────────────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!HasInputAuthority) return;
            if (InGameManager.Instance == null ||
                InGameManager.Instance.gameState != InGameManager.GameState.Waiting) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                RPC_SetReady(!IsReady);
        }

        // ─────────────────────────────────────────────────────────────────────
        // FixedUpdateNetwork
        // ─────────────────────────────────────────────────────────────────────

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            UpdateOnStartPoint();
        }

        private void UpdateOnStartPoint()
        {
            if (InGameManager.Instance == null || StartingPointIndex < 0 ||
                InGameManager.Instance.gameState != InGameManager.GameState.Waiting)
            {
                OnStartPoint = false;
                return;
            }

            var pts = InGameManager.Instance.startingPoints;
            if (pts == null || StartingPointIndex >= pts.Length || pts[StartingPointIndex] == null)
            {
                OnStartPoint = false;
                return;
            }

            OnStartPoint = pts[StartingPointIndex].IsPlayerInRange(transform.position);
        }

        // ─────────────────────────────────────────────────────────────────────
        // OnChangedRender колбэки (работают на всех клиентах)
        // ─────────────────────────────────────────────────────────────────────

        private void OnIsReadyChanged()
        {
            // Обновляем плашку Ready в LevelInfoContent (на всех клиентах)
            InGameManager.Instance?.OnPlayerReadyChanged(Id, DisplayName, IsReady);
        }

        private void OnHasFinishedChanged()
        {
            if (!HasFinished) return; // срабатываем только при true (не при сбросе в false)

            // Плашка "is finished" для всех
            InGameManager.Instance?.OnPlayerFinishedUI(DisplayName);

            // Локальный игрок — переключаем камеру
            if (HasInputAuthority)
                InGameManager.Instance?.OnLocalPlayerFinished(this);
        }

        private void OnIsVisibleChanged()
        {
            SetRenderersEnabled(IsVisible);
        }

        // ─────────────────────────────────────────────────────────────────────
        // RPC
        // ─────────────────────────────────────────────────────────────────────

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_SetReady(NetworkBool ready)
        {
            if (InGameManager.Instance == null ||
                InGameManager.Instance.gameState != InGameManager.GameState.Waiting) return;
            IsReady = ready;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_AssignStartingPoint(int index)
        {
            StartingPointIndex = index;
            IsReady            = false;
            IsVisible          = true;
            TeleportToStartingPoint();
            _cameraManager?.FlyingCameraStart();
            StartCoroutine(FlyCameraCrtn());
        }

        private IEnumerator FlyCameraCrtn()
        {
            yield return new WaitForSeconds(3f);
            _cameraManager?.InitForPlayer(_cameraTarget);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_TeleportToStart()
        {
            TeleportToStartingPoint();
        }

        /// <summary>
        /// Вызывается InGameManager-ом: игрок сам себя помечает как финишировавшего.
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_SetFinished(int place)
        {
            HasFinished = true;
            FinishPlace = place;
            IsVisible   = false;
        }

        /// <summary>Reset перед новым раундом (KeepPlaying / StartRace).</summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_ResetForNewRound()
        {
            HasFinished = false;
            FinishPlace = 0;
            IsVisible   = true;
            IsReady     = false;
            TeleportToStartingPoint();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_ApplyPush(Vector3 force)
        {
            var rb = GetComponent<Rigidbody>();
            if (rb == null) return;
            rb.AddForce(force, ForceMode.Impulse);
            _playerView.PlayStunVFX();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Телепорт
        // ─────────────────────────────────────────────────────────────────────

        public void TeleportToStartingPoint()
        {
            if (!HasStateAuthority) return;

            if (InGameManager.Instance == null || StartingPointIndex < 0)
            {
                Debug.LogWarning($"[PlayerRaceData] Cannot teleport: Manager={InGameManager.Instance != null}, Index={StartingPointIndex}");
                return;
            }

            var pts = InGameManager.Instance.startingPoints;
            if (pts == null || StartingPointIndex >= pts.Length || pts[StartingPointIndex] == null)
            {
                Debug.LogError($"[PlayerRaceData] Invalid starting point index {StartingPointIndex}");
                return;
            }

            var pt = pts[StartingPointIndex];
            TeleportTo(pt.SpawnPosition, pt.SpawnRotation);

            CheckpointPosition = pt.SpawnPosition;
            CheckpointRotation = pt.SpawnRotation;
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

            if (rb != null) rb.isKinematic = false;

            _cameraManager?.ResetPlayerCameraPos();
            _vfxManager?.PlaySalutVFX(pos + Vector3.up);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Видимость
        // ─────────────────────────────────────────────────────────────────────

        private void SetRenderersEnabled(bool visible)
        {
            if (_renderers == null)
                _renderers = GetComponentsInChildren<Renderer>(true);
            if (_renderers == null) return;
            foreach (var r in _renderers)
                if (r != null) r.enabled = visible;

            //GetComponent<Collider>().enabled = visible;
        }
    }
}
