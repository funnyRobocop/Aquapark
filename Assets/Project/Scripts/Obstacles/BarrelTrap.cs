
using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace NonameGame
{
    public class BarrelTrap : NetworkBehaviour
    {
        [Header("Настройки движения")]
        [SerializeField] private Transform visual;

        [SerializeField] private float sinkDistance = 3f;

        [Header("Тайминги (секунды)")]
        [SerializeField] private float activationDelay = 2f;
        [SerializeField] private float sinkDuration = 0.5f;
        [SerializeField] private float underwaterDuration = 3f;
        [SerializeField] private float riseDuration = 0.5f;

        [Header("Настройки игрока")]
        [SerializeField] private string playerTag = "Player";

        // Время начала ожидания игрока
        [Networked]
        private float HoldStartTime { get; set; }

        // Время начала движения бочки
        [Networked]
        private float CycleStartTime { get; set; }

        private Vector3 _startLocalPosition;
        private Vector3 _targetLocalPosition;

        // Локальный список коллайдеров игроков
        // Используется только State Authority
        private readonly HashSet<Collider> _playerColliders =
            new HashSet<Collider>();

        private float TotalCycleDuration =>
            sinkDuration + underwaterDuration + riseDuration;

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
            if (visual == null)
                visual = transform;

            _startLocalPosition = visual.localPosition;

            _targetLocalPosition =
                _startLocalPosition + Vector3.down * sinkDistance;

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

            // Первый игрок наступил на бочку
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

            // Все игроки покинули бочку до активации
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

            // Проверяем, сколько игрок стоит на бочке
            if (CycleStartTime < 0f &&
                HoldStartTime >= 0f &&
                _playerColliders.Count > 0)
            {
                if (now - HoldStartTime >= activationDelay)
                {
                    // Запускаем цикл падения
                    CycleStartTime = now;
                    HoldStartTime = -1f;
                }
            }

            // Цикл завершился
            if (CycleStartTime >= 0f &&
                now - CycleStartTime >= TotalCycleDuration)
            {
                CycleStartTime = -1f;

                // Если игрок всё ещё находится на бочке,
                // запускаем новый отсчёт
                if (_playerColliders.Count > 0)
                    HoldStartTime = now;
            }
        }

        public override void Render()
        {
            if (visual == null)
                return;

            float progress = 0f;

            if (CycleStartTime >= 0f)
            {
                float elapsed = NetworkTime - CycleStartTime;

                // 1. Опускаем бочку
                if (elapsed < sinkDuration)
                {
                    float t = elapsed /
                        Mathf.Max(sinkDuration, 0.0001f);

                    progress = Mathf.SmoothStep(0f, 1f, t);
                }
                // 2. Бочка под водой
                else if (elapsed <
                         sinkDuration + underwaterDuration)
                {
                    progress = 1f;
                }
                // 3. Поднимаем бочку
                else if (elapsed < TotalCycleDuration)
                {
                    float t =
                        (elapsed - sinkDuration - underwaterDuration) /
                        Mathf.Max(riseDuration, 0.0001f);

                    progress = 1f - Mathf.SmoothStep(0f, 1f, t);
                }
            }

            visual.localPosition = Vector3.Lerp(
                _startLocalPosition,
                _targetLocalPosition,
                progress
            );
        }
    }
}
