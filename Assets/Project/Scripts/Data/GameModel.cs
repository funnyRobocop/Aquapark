using System;
using System.Collections.Generic;
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
        public event Action<int> OnLevelChanged;

        // Поля данных
        public int SelectedSkinId;
        public List<int> PurchasedSkinIdList;
        public int Money;
        public int Level;
        public List<Stats> StatsList;

        // Конструктор для VContainer
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
            this.Level = savedData.Level;
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
        public void UpdateStatsAndProgress(int newLevel, Stats newStats)
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
                existingStats.GoldCount += newStats.GoldCount;
                existingStats.FinishCount += newStats.FinishCount;
                existingStats.KickCount += newStats.KickCount;
                existingStats.ThrowCount += newStats.ThrowCount;
            }
            
            if (this.Level != newLevel)
            {
                this.Level = newLevel;
                OnLevelChanged?.Invoke(Level);
            }

            OnDataChanged?.Invoke();
            _saveService.Save(this); // Конец уровня — идеальный момент для сохранения на WebGL
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
        }
    }
}
