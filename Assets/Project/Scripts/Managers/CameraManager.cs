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
        [SerializeField] private LevelUI _levelUI;

        // ─── Spectator ─────────────────────────────────────────────────────────────────

        private readonly List<PlayerRaceData> _spectateTargets = new();
        private int  _spectateIndex;
        private bool _isSpectating;

        // ──────────────────────────────────────────────────────────────────────
        // Q/E переключение в режиме спектатора
        // ──────────────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!_isSpectating || _spectateTargets.Count == 0) return;

            // При выходе из сессии объекты могут быть уже деспавнены — очищаем список
            int countBefore = _spectateTargets.Count;
            _spectateTargets.RemoveAll(p => !IsValid(p));

            if (_spectateTargets.Count == 0) { StopSpectating(); return; }

            // Индекс вышел за пределы после удаления — сбрасываем
            if (_spectateIndex >= _spectateTargets.Count) _spectateIndex = 0;

            // Если кто-то был удалён — сразу переключаем камеру на текущего в списке
            if (_spectateTargets.Count != countBefore)
            {
                ApplySpectateTarget(_spectateTargets[_spectateIndex].CameraTarget);
                _levelUI?.ShowObjectiveObj(_spectateTargets[_spectateIndex].DisplayName);
            }

            // Авто-переключение: текущий таргет финишировал — найти следующего активного
            var current = CurrentTarget;
            if (IsValid(current) && current.HasFinished)
            {
                var next = FindNextActive(_spectateIndex, +1);
                if (next >= 0)
                {
                    _spectateIndex = next;
                    ApplySpectateTarget(_spectateTargets[_spectateIndex].CameraTarget);
                    _levelUI?.ShowObjectiveObj(_spectateTargets[_spectateIndex].DisplayName);
                }
            }

            // Q/E переключение
            if (_spectateTargets.Count <= 1) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.qKey.wasPressedThisFrame)
                SpectateRelative(-1);
            else if (kb.eKey.wasPressedThisFrame)
                SpectateRelative(+1);
        }

        private PlayerRaceData CurrentTarget =>
            _spectateTargets.Count > 0 ? _spectateTargets[_spectateIndex] : null;

        private static bool IsValid(PlayerRaceData p) =>
            p != null && p.Object != null && p.Object.IsValid;

        /// <summary>
        /// Находит индекс следующего нефинишировавшего игрока в направлении dir.
        /// Возвращает -1 если все финишировали.
        /// </summary>
        private int FindNextActive(int from, int dir)
        {
            int n = _spectateTargets.Count;
            for (int i = 1; i <= n; i++)
            {
                int idx = (from + dir * i + n * n) % n;
                if (IsValid(_spectateTargets[idx]) && !_spectateTargets[idx].HasFinished)
                    return idx;
            }
            return -1;
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
            if (_splineDolly != null)
                _splineDolly.AutomaticDolly.Enabled = false;

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
                    _spectateTargets.Add(p);

            if (_spectateTargets.Count == 0)
            {
                Debug.LogWarning("[CameraManager] BeginSpectating: no valid targets.");
                return;
            }

            if (_splineDolly != null)
                _splineDolly.AutomaticDolly.Enabled = false;

            // Начинаем с первого нефинишировавшего
            _spectateIndex    = FindNextActive(-1, +1);
            if (_spectateIndex < 0) _spectateIndex = 0;

            _isSpectating          = true;
            _playerCamera.Priority = 10;
            ApplySpectateTarget(_spectateTargets[_spectateIndex].CameraTarget);

            _levelUI.ShowObjectiveObj(_spectateTargets[_spectateIndex].DisplayName);
        }

        /// <summary>Выход из режима спектатора (например при Keep Playing).</summary>
        public void StopSpectating()
        {
            _isSpectating = false;
            _spectateTargets.Clear();

            _levelUI.HideObjectiveObj();
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
            int next = FindNextActive(_spectateIndex, delta);
            if (next < 0) return; // все финишировали
            _spectateIndex = next;
            ApplySpectateTarget(_spectateTargets[_spectateIndex].CameraTarget);
        }

        private void ApplySpectateTarget(Transform target)
        {
            if (_playerCamera == null || target == null) return;
            _playerCamera.Target.TrackingTarget = target;
        }
    }
}
