
using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace NonameGame
{
    public class BarrelTrap : NetworkBehaviour
    {
        [Header("Объекты")]
        [SerializeField] private Transform platform;
        [SerializeField] private Transform visual;

        [Header("Настройки движения")]
        [SerializeField] private float sinkDistance = 3f;

        [Header("Тайминги (секунды)")]
        [SerializeField] private float activationDelay = 2f;
        [SerializeField] private float shakeDuration = 0.3f;
        [SerializeField] private float sinkDuration = 0.5f;
        [SerializeField] private float underwaterDuration = 3f;
        [SerializeField] private float riseDuration = 0.5f;

        [Header("Настройки тряски")]
        [SerializeField] private float shakeAmountX = 0.04f;
        [SerializeField] private float shakeAmountZ = 0.04f;
        [SerializeField] private float shakeFrequency = 18f;

        [Header("Настройки игрока")]
        [SerializeField] private string playerTag = "Player";

        [Networked]
        private float HoldStartTime { get; set; }

        [Networked]
        private float CycleStartTime { get; set; }

        private Vector3 _platformStartPosition;
        private Vector3 _platformTargetPosition;
        private Vector3 _visualStartPosition;

        private readonly HashSet<Collider> _playerColliders =
            new HashSet<Collider>();

        private float TotalCycleDuration =>
            shakeDuration + sinkDuration +
            underwaterDuration + riseDuration;

        private float NetworkTime
        {
            get
            {
                if (SimulationTimeProvider.Instance != null)
                    return SimulationTimeProvider.Time;

                return Runner.SimulationTime;
            }
        }

        public override void Spawned()
        {
            if (platform == null)
                platform = transform;

            _platformStartPosition = platform.localPosition;

            _platformTargetPosition =
                _platformStartPosition + Vector3.down * sinkDistance;

            if (visual != null)
                _visualStartPosition = visual.localPosition;

            if (HasStateAuthority)
            {
                HoldStartTime = -1f;
                CycleStartTime = -1f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!HasStateAuthority)
                return;

            if (!IsPlayer(other))
                return;

            if (!_playerColliders.Add(other))
                return;

            if (_playerColliders.Count == 1 &&
                CycleStartTime < 0f)
            {
                HoldStartTime = NetworkTime;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!HasStateAuthority)
                return;

            _playerColliders.Remove(other);

            if (_playerColliders.Count == 0 &&
                CycleStartTime < 0f)
            {
                HoldStartTime = -1f;
            }
        }

        private bool IsPlayer(Collider other)
        {
            Transform current = other.transform;

            while (current != null)
            {
                if (current.CompareTag(playerTag))
                    return true;

                current = current.parent;
            }

            return false;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            float now = NetworkTime;

            // Ожидание игрока
            if (CycleStartTime < 0f &&
                HoldStartTime >= 0f &&
                _playerColliders.Count > 0)
            {
                if (now - HoldStartTime >= activationDelay)
                {
                    CycleStartTime = now;
                    HoldStartTime = -1f;
                }
            }

            // Завершение полного цикла
            if (CycleStartTime >= 0f &&
                now - CycleStartTime >= TotalCycleDuration)
            {
                CycleStartTime = -1f;

                if (_playerColliders.Count > 0)
                    HoldStartTime = now;
            }
        }

        public override void Render()
        {
            float elapsed = CycleStartTime >= 0f
                ? NetworkTime - CycleStartTime
                : -1f;

            float sinkProgress = 0f;

            // 1. Тряска перед падением
            if (visual != null)
            {
                Vector3 shakeOffset = Vector3.zero;

                if (elapsed >= 0f && elapsed < shakeDuration)
                {
                    float t = elapsed;

                    // Плавное усиление тряски
                    float envelope = Mathf.SmoothStep(
                        0f, 1f,
                        Mathf.Clamp01(t / 0.08f)
                    );

                    float waveX = Mathf.Sin(
                        t * shakeFrequency * Mathf.PI * 2f
                    );

                    float waveZ = Mathf.Sin(
                        t * shakeFrequency * Mathf.PI * 2f + 1.5f
                    );

                    shakeOffset.x =
                        waveX * shakeAmountX * envelope;

                    shakeOffset.z =
                        waveZ * shakeAmountZ * envelope;
                }

                visual.localPosition =
                    _visualStartPosition + shakeOffset;
            }

            // 2. Опускание платформы
            if (elapsed >= shakeDuration &&
                elapsed < shakeDuration + sinkDuration)
            {
                float t = (elapsed - shakeDuration) /
                    Mathf.Max(sinkDuration, 0.0001f);

                sinkProgress = Mathf.SmoothStep(0f, 1f, t);
            }
            // 3. Платформа под водой
            else if (elapsed >= shakeDuration + sinkDuration &&
                     elapsed < shakeDuration + sinkDuration +
                     underwaterDuration)
            {
                sinkProgress = 1f;
            }
            // 4. Возвращение платформы
            else if (elapsed >= shakeDuration + sinkDuration +
                     underwaterDuration &&
                     elapsed < TotalCycleDuration)
            {
                float t = (elapsed - shakeDuration -
                    sinkDuration - underwaterDuration) /
                    Mathf.Max(riseDuration, 0.0001f);

                sinkProgress = 1f - Mathf.SmoothStep(0f, 1f, t);
            }

            // Применяем движение к платформе с коллайдером
            if (platform != null)
            {
                platform.localPosition = Vector3.Lerp(
                    _platformStartPosition,
                    _platformTargetPosition,
                    sinkProgress
                );
            }
        }
    }
}
