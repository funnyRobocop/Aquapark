using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NonameGame
{
    public class CameraManager : MonoBehaviour
    {
        [SerializeField] private CinemachineSplineDolly   _splineDolly;
        [SerializeField] private CinemachineCamera        _playerCamera;
        [SerializeField] private CinemachineOrbitalFollow _playerCameraFollow;
        [SerializeField] private CinemachineCamera[]      _allRagdollCameras;

        // ─── Spectator ─────────────────────────────────────────────────────────────────

        private readonly List<Transform> _spectateTargets = new();
        private int  _spectateIndex;
        private bool _isSpectating;

        // ──────────────────────────────────────────────────────────────────────
        // Q/E переключение в режиме спектатора
        // ──────────────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!_isSpectating || _spectateTargets.Count <= 1) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.qKey.wasPressedThisFrame)
                SpectateRelative(-1);
            else if (kb.eKey.wasPressedThisFrame)
                SpectateRelative(+1);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────────────────

        public void ResetPlayerCameraPos()
        {
            if (_playerCameraFollow != null)
                _playerCameraFollow.HorizontalAxis.Value = 180f;
        }

        public void FlyingCameraStart()
        {
            if (_splineDolly == null) return;
            _splineDolly.CameraPosition              = 0;
            _splineDolly.AutomaticDolly.Enabled       = true;
        }

        /// <summary>Передаём камеру на локального игрока и останавливаем авто-долли.</summary>
        public void InitForPlayer(Transform playerTransform)
        {
            if (_playerCamera != null)
                _playerCamera.Target.TrackingTarget = playerTransform;

            _playerCamera.Priority = 10;
            StopSpectating();
        }

        // ──────────────────────────────────────────────────────────────────────
        // Spectator
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Запускает режим спектатора: камера следит за первым игроком в списке.
        /// Q — предыдущий, E — следующий.
        /// </summary>
        public void BeginSpectating(IList<PlayerRaceData> players)
        {
            _spectateTargets.Clear();
            foreach (var p in players)
                if (p != null && p.CameraTarget != null)
                    _spectateTargets.Add(p.CameraTarget);

            if (_spectateTargets.Count == 0)
            {
                Debug.LogWarning("[CameraManager] BeginSpectating: no valid targets.");
                return;
            }

            _spectateIndex    = 0;
            _isSpectating     = true;
            _playerCamera.Priority = 10;
            ApplySpectateTarget(_spectateTargets[0]);
        }

        /// <summary>Выход из режима спектатора (например при Keep Playing).</summary>
        public void StopSpectating()
        {
            _isSpectating = false;
            _spectateTargets.Clear();
        }

        // ──────────────────────────────────────────────────────────────────────
        // Ragdoll cameras (без изменений)
        // ──────────────────────────────────────────────────────────────────────

        public void InitForRagdoll(Transform ragdollTransform)
        {
            foreach (var cam in _allRagdollCameras)
                cam.Target.LookAtTarget = ragdollTransform;
        }

        public void SwitchOnRagdollCamera(CinemachineCamera activeCamera)
        {
            SwitchOffAllRagdollCameras();
            if (activeCamera != null) activeCamera.Priority = 20;
        }

        public void SwitchOnRagdollCamera(int cameraIndex)
        {
            SwitchOffAllRagdollCameras();
            if (cameraIndex >= 0 && cameraIndex < _allRagdollCameras.Length)
                _allRagdollCameras[cameraIndex].Priority = 20;
        }

        public void SwitchOffAllRagdollCameras()
        {
            foreach (var cam in _allRagdollCameras)
                if (cam != null) cam.Priority = 0;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────

        private void SpectateRelative(int delta)
        {
            if (_spectateTargets.Count == 0) return;
            _spectateIndex = (_spectateIndex + delta + _spectateTargets.Count) % _spectateTargets.Count;
            ApplySpectateTarget(_spectateTargets[_spectateIndex]);
        }

        private void ApplySpectateTarget(Transform target)
        {
            if (_playerCamera == null || target == null) return;
            _playerCamera.Target.TrackingTarget = target;
        }
    }
}
