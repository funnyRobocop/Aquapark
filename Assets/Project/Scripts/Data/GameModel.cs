using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Settings;
using VContainer;
using VContainer.Unity;

namespace NonameGame
{
    [System.Serializable]
    public class GameModel : IInitializable
    {
        private readonly SaveService _saveService;
        private readonly SoundManager _soundManager; // Добавляем приватное поле для звука

        public event Action OnDataChanged;
        public event Action<int> OnMoneyChanged;
        public event Action<string> OnPlayerNameChanged;
        public event Action<int, int> OnLevelChanged;

        public string PlayerName;
        public int SelectedSkinId;
        public List<int> PurchasedSkinIdList;
        public int Money;
        public int Points;
        public List<Stats> StatsList;
        public List<AchievementData> Achievements = new();
        public List<string> ClaimedBattlePassRewards = new();

        [Inject] private AchievementsConfig _achievementsConfig;

        public int SessionKickCount { get; set; }
        public int SessionThrowCount { get; set; }
        public int SessionGoldCount { get; set; }
        public int SessionFinishCount { get; set; }

        public SettingsData Settings { get; private set; }

        public int Level => Mathf.FloorToInt(Points / 500f) + 1;

        public int CalculatePoints(int finish, int gold, int kicks, int throws)
        {
            return (finish * 10) + (gold * 100) + kicks + throws;
        }

        // Внедряем SoundManager через конструктор
        public GameModel(SaveService saveService, SoundManager soundManager)
        {
            _saveService = saveService;
            _soundManager = soundManager;
        }

        public void Initialize()
        {
            // 1. Загружаем локальные настройки PlayerPrefs
            Settings = SettingsData.Load();
            ApplyInitialLocale();
            ApplyInitialSoundSettings();

            // 2. Загружаем данные профиля игрока через SaveService
            GameModel savedData = _saveService.Load();

            if (savedData != null)
            {
                // Если сохранение есть — восстанавливаем данные
                PlayerName = savedData.PlayerName;
                SelectedSkinId = savedData.SelectedSkinId;
                PurchasedSkinIdList = savedData.PurchasedSkinIdList ?? new List<int>();
                Money = savedData.Money;
                Points = savedData.Points;
                StatsList = savedData.StatsList ?? new List<Stats>();
                ClaimedBattlePassRewards = savedData.ClaimedBattlePassRewards ?? new List<string>();
                Achievements = savedData.Achievements ?? new List<AchievementData>();
            }
            else
            {
                // Если сохранения НЕТ — задаем начальные дефолтные значения прямо здесь
                PlayerName = "";
                SelectedSkinId = -1;
                PurchasedSkinIdList = new List<int>();
                Money = 1000;
                Points = 0;
                StatsList = new List<Stats>();
                ClaimedBattlePassRewards = new List<string>();
                Achievements = new List<AchievementData>();
            }

            // Инициализация / синхронизация ачивок с конфигом
            if (Achievements.Count == 0)
            {
                if (_achievementsConfig != null)
                {
                    foreach (var def in _achievementsConfig.AchievementsAll)
                    {
                        Achievements.Add(new AchievementData(def.Key, def.Value.Goal));
                    }
                }
            }
            else
            {
                SyncAchievementsWithConfig();
            }
        }

        private async void ApplyInitialLocale()
        {
            await LocalizationSettings.InitializationOperation.Task;

            int localeIndex = Settings.Language;
            if (localeIndex >= 0 && localeIndex < LocalizationSettings.AvailableLocales.Locales.Count)
            {
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeIndex];
            }
        }

        /// <summary>
        /// Передает сохраненные флаги звука и музыки в SoundManager при старте
        /// </summary>
        private void ApplyInitialSoundSettings()
        {
            if (_soundManager != null)
            {
                _soundManager.SetMusicEnabled(Settings.Music);
                _soundManager.SetSoundEnabled(Settings.Sound);
            }
        }

        public bool CanClaimBattlePassReward(string rewardId)
        {
            if (ClaimedBattlePassRewards.Contains(rewardId)) return false;

            var reward = _achievementsConfig.LevelRewards.Find(r => r.Id == rewardId);
            if (reward == null) return false;

            return Points >= reward.RequiredPoints;
        }

        public bool ClaimBattlePassReward(string rewardId, int multiplier = 1)
        {
            if (!CanClaimBattlePassReward(rewardId)) return false;

            var reward = _achievementsConfig.LevelRewards.Find(r => r.Id == rewardId);
            if (reward == null) return false;

            ClaimedBattlePassRewards.Add(rewardId);
            Money += reward.RewardMoney * multiplier;
            OnMoneyChanged?.Invoke(Money);

            OnDataChanged?.Invoke();
            _saveService.Save(this);
            return true;
        }

        private void SyncAchievementsWithConfig()
        {
            if (_achievementsConfig == null) return;

            foreach (var def in _achievementsConfig.AchievementsAll)
            {
                if (!Achievements.Exists(a => a.Id == def.Key))
                {
                    Achievements.Add(new AchievementData(def.Key, def.Value.Goal));
                }
            }
        }

        public void ChangeName(string name)
        {
            PlayerName = name;
            OnPlayerNameChanged?.Invoke(name);
            _saveService.Save(this);
        }

        public void SelectSkin(int skinId)
        {
            SelectedSkinId = skinId;
            OnDataChanged?.Invoke();
            _saveService.Save(this);
        }

        public void PurchaseSkin(int skinId, int price)
        {
            if (Money >= price && !PurchasedSkinIdList.Contains(skinId))
            {
                Money -= price;
                PurchasedSkinIdList.Add(skinId);
                OnMoneyChanged?.Invoke(Money);
                OnDataChanged?.Invoke();
                _saveService.Save(this);
            }
        }

        public void ClaimBattlePassMoney(int rewardAmount)
        {
            Money += rewardAmount;
            OnMoneyChanged?.Invoke(Money);
            _saveService.Save(this);
        }

        public void UpdateStatsAndProgress(Stats newStats)
        {
            if (StatsList == null)
                StatsList = new List<Stats>();

            if (StatsList.Count == 0 || !StatsList.Exists(s => s.Id == newStats.Id))
            {
                StatsList.Add(newStats);
            }
            else
            {
                var existingStats = StatsList.Find(s => s.Id == newStats.Id);
                existingStats.FinishCount += newStats.FinishCount;
                existingStats.GoldCount += newStats.GoldCount;
                existingStats.KickCount += newStats.KickCount;
                existingStats.ThrowCount += newStats.ThrowCount;
            }

            var newPoints = CalculatePoints(newStats.FinishCount, newStats.GoldCount, newStats.KickCount, newStats.ThrowCount);
            Points = Mathf.Clamp(Points + newPoints, 0, _achievementsConfig != null ? _achievementsConfig.TargetPoints : int.MaxValue);

            UpdateAchievementsProgress();

            OnLevelChanged?.Invoke(Points, Level);
            OnDataChanged?.Invoke();
            _saveService.Save(this);
        }

        public void ClearSessionData()
        {
            SessionFinishCount = 0;
            SessionGoldCount = 0;
            SessionKickCount = 0;
            SessionThrowCount = 0;
        }

        public void UpdateAchievementsProgress()
        {
            if (Achievements == null || _achievementsConfig == null) return;

            int totalFinish = StatsList?.Sum(s => s.FinishCount) ?? 0;
            int totalGold = StatsList?.Sum(s => s.GoldCount) ?? 0;
            int totalThrow = StatsList?.Sum(s => s.ThrowCount) ?? 0;
            int totalKick = StatsList?.Sum(s => s.KickCount) ?? 0;

            foreach (var ach in Achievements)
            {
                _achievementsConfig.AchievementsAll.TryGetValue(ach.Id, out AchievementDefinition def);
                if (def == null) continue;

                int value = def.Type switch
                {
                    AchievementType.Finish => totalFinish,
                    AchievementType.Gold => totalGold,
                    AchievementType.Throw => totalThrow,
                    AchievementType.Kick => totalKick,
                    _ => 0
                };

                ach.Current = Mathf.Min(value, ach.Goal);
            }
        }

        public bool ClaimAchievement(string achievementId, int multiplier = 1)
        {
            var ach = Achievements.Find(a => a.Id == achievementId);
            if (ach == null || !ach.CanClaim) return false;

            _achievementsConfig.AchievementsAll.TryGetValue(ach.Id, out AchievementDefinition def);
            if (def == null) return false;

            ach.RewardTaken = true;

            // Перемещаем в конец списка
            Achievements.Remove(ach);
            Achievements.Add(ach);

            Money += def.RewardMoney * multiplier;
            OnMoneyChanged?.Invoke(Money);

            OnDataChanged?.Invoke();
            _saveService.Save(this);
            return true;
        }

        public void ClearStatsAndProgress()
        {
            Points = 0;
            OnLevelChanged?.Invoke(Points, Level);
            StatsList.Clear();
            ClaimedBattlePassRewards.Clear();

            // Сбрасываем только прогресс, награды оставляем полученными
            foreach (var ach in Achievements)
            {
                ach.Current = 0;
                ach.RewardTaken = false;
            }

            OnDataChanged?.Invoke();
            _saveService.Save(this);
        }

        internal bool IsSkinPurchased(int skinId)
        {
            return PurchasedSkinIdList.Contains(skinId);
        }

        [Serializable]
        public class Stats
        {
            public int Id;
            public int FinishCount;
            public int GoldCount;
            public int KickCount;
            public int ThrowCount;

            public Stats(int id, int finishCount, int goldCount, int kickCount, int throwCount)
            {
                Id = id;
                FinishCount = finishCount;
                GoldCount = goldCount;
                KickCount = kickCount;
                ThrowCount = throwCount;
            }
        }

        [Serializable]
        public class AchievementData
        {
            public string Id;
            public int Current;
            public int Goal;
            public bool RewardTaken;

            public AchievementData() { } // для сериализации

            public AchievementData(string id, int goal)
            {
                Id = id;
                Goal = goal;
                Current = 0;
                RewardTaken = false;
            }

            public bool IsCompleted => Current >= Goal;
            public bool CanClaim => IsCompleted && !RewardTaken;
        }
    }
}