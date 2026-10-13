using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace NonameGame
{
    [System.Serializable]
    public class GameModel : IInitializable
    {
        // Ссылку на SaveService контейнер внедрит автоматически через конструктор
        private readonly SaveService _saveService;

        // События для UI, чтобы обновлять экраны при изменении
        public event Action OnDataChanged;
        public event Action<int> OnMoneyChanged;
        public event Action<int, int> OnLevelChanged;

        // Поля данных
        public int SelectedSkinId;
        public List<int> PurchasedSkinIdList;
        public int Money;
        public int Points;
        public List<Stats> StatsList;
        [Inject] private AchievementsConfig _achievementsConfig;

        public int Level => Mathf.CeilToInt(Points / 1000f);

        public int CalculatePoints(int throws, int kicks, int gold, int finish)
        {
            return (throws * 5) + (kicks * 5) + (gold * 100) + (finish * 50);
        }

        public GameModel(SaveService saveService)
        {
            _saveService = saveService;
        }

        // Вызовется автоматически ОДИН раз при старте игры
        public void Initialize()
        {
            GameModel savedData = _saveService.Load();

            // Переносим загруженные данные в этот экземпляр
            this.SelectedSkinId = savedData.SelectedSkinId;
            this.PurchasedSkinIdList = savedData.PurchasedSkinIdList ?? new List<int>();
            this.Money = savedData.Money;
            this.Points = savedData.Points;
            this.StatsList = savedData.StatsList ?? new List<Stats>();
        }

        // --- МЕТОДЫ ИЗМЕНЕНИЯ ДАННЫХ (Вызываются из UI / Геймплея) ---

        public void SelectSkin(int skinId)
        {
            SelectedSkinId = skinId;
            OnDataChanged?.Invoke();
            _saveService.Save(this); // Выбор скина нечастый — сохраняем сразу
        }

        public void PurchaseSkin(int skinId, int price)
        {
            if (Money >= price && !PurchasedSkinIdList.Contains(skinId))
            {
                Money -= price;
                PurchasedSkinIdList.Add(skinId);
                OnMoneyChanged?.Invoke(Money);
                OnDataChanged?.Invoke();
                _saveService.Save(this); // Покупка редкая — сохраняем сразу
            }
        }

        public void ClaimBattlePassMoney(int rewardAmount)
        {
            Money += rewardAmount;
            OnMoneyChanged?.Invoke(Money);
            _saveService.Save(this); // Забрали награду в UI — сохраняем сразу
        }

        // Вызывается в конце уровня: принимает накопленную локально за уровень статистику
        public void UpdateStatsAndProgress(Stats newStats)
        {
            if (StatsList == null)
                StatsList = new List<Stats>();

            if (StatsList.Count == 0 || !StatsList.Exists(s => s.Id == newStats.Id))
            {
                // Если уровень ещё не игрался, добавляем новый элемент в список
                StatsList.Add(newStats);
            }
            else
            {
                // Если уровень уже игрался, обновляем существующий элемент
                var existingStats = StatsList.Find(s => s.Id == newStats.Id);
                existingStats.GoldCount = Mathf.Clamp(
                    existingStats.GoldCount + newStats.GoldCount, 0, _achievementsConfig.TargetGoldCount);
                existingStats.FinishCount = Mathf.Clamp(
                    existingStats.FinishCount + newStats.FinishCount, 0, _achievementsConfig.TargetFinishCount);
                existingStats.KickCount = Mathf.Clamp(
                    existingStats.KickCount + newStats.KickCount, 0, _achievementsConfig.TargetKickCount);
                existingStats.ThrowCount = Mathf.Clamp(
                    existingStats.ThrowCount + newStats.ThrowCount, 0, _achievementsConfig.TargetThrowCount);
            }

            var newPoints = CalculatePoints(newStats.ThrowCount, newStats.KickCount, newStats.GoldCount, newStats.FinishCount);

            Points = Mathf.Clamp(Points + newPoints, 0, _achievementsConfig.TargetPoints);
            OnLevelChanged?.Invoke(Points, Level);

            OnDataChanged?.Invoke();
            _saveService.Save(this); // Конец уровня — идеальный момент для сохранения на WebGL
        }

        public void ClearStatsAndProgress()
        {
            this.Points = 0;
            OnLevelChanged?.Invoke(Points, Level);
            StatsList.Clear();
            OnDataChanged?.Invoke();
            _saveService.Save(this);
        }

        internal bool IsSkinPurchased(int skinId)
        {
            return PurchasedSkinIdList.Contains(skinId);
        }

        [System.Serializable]
        public class Stats
        {
            public int Id;
            public int GoldCount;
            public int FinishCount;
            public int KickCount;
            public int ThrowCount;

            public Stats(int id, int goldCount, int finishCount, int kickCount, int throwCount)
            {
                Id = id;
                GoldCount = goldCount;
                FinishCount = finishCount;
                KickCount = kickCount;
                ThrowCount = throwCount;
            }
        }
    }
}
