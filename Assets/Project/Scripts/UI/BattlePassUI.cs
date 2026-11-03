using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using YG;

namespace NonameGame
{
    public class BattlePassUI : MonoBehaviour
    {
        [Header("Main Progress")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Slider totalProgressSlider;

        [Header("Achievements List")]
        [SerializeField] private Transform achievementsContainer;
        [SerializeField] private AchievementItemUI achievementItemPrefab;

        [Header("Battle Pass Level Rewards (уже раскиданы в UI)")]
        [SerializeField] private List<BattlePassRewardButton> levelRewardButtons = new();

        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI timerText;

        [SerializeField] private List<GameObject> levelNumList;

        [SerializeField] private RewardUI rewardPanel;

        private GameModel _gameModel;
        private AchievementsConfig _achievementsConfig;


        [Inject]
        public void Construct(GameModel gameModel, AchievementsConfig achievementsConfig)
        {
            _gameModel = gameModel;
            _achievementsConfig = achievementsConfig;
        }

        private void Awake()
        {
            if (_gameModel != null)
            {
                _gameModel.OnDataChanged += RefreshUI;
                _gameModel.OnLevelChanged += HandleLevelChanged;
            }

            // Подписываем кнопки
            foreach (var btn in levelRewardButtons)
            {
                if (btn.button != null)
                    btn.button.onClick.AddListener(() => OnLevelRewardClaimed(btn.rewardId));
            }
        }

        private void OnDestroy()
        {
            if (_gameModel != null)
            {
                _gameModel.OnDataChanged -= RefreshUI;
                _gameModel.OnLevelChanged -= HandleLevelChanged;
            }
        }

        private void OnEnable()
        {
            RefreshUI();
            if (_gameModel != null)
                HandleLevelChanged(_gameModel.Points, _gameModel.Level);
            UpdateWeeklyTimer();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void RefreshUI()
        {
            if (_gameModel == null || _achievementsConfig == null) return;

            // Очищаем контейнер
            foreach (Transform child in achievementsContainer)
                Destroy(child.gameObject);

            // Сортировка: сначала можно забрать → незавершённые → уже забранные
            var sorted = _gameModel.Achievements
                .OrderBy(a => a.RewardTaken)          // false (можно/ещё нет) выше
                .ThenByDescending(a => a.CanClaim)    // CanClaim = true выше
                .ThenBy(a => a.Id)
                .ToList();

            foreach (var ach in _gameModel.Achievements)
            {
                _achievementsConfig.AchievementsAll.TryGetValue(ach.Id, out AchievementDefinition def);
                if (def == null) continue;

                var item = Instantiate(achievementItemPrefab, achievementsContainer);
                item.Setup(_gameModel, ach, def, OnClaimClicked);
            }

            RefreshLevelRewardButtons();
            UpdateTotalProgressTrack();
        }

        private void RefreshLevelRewardButtons()
        {
            foreach (var btnData in levelRewardButtons)
            {
                if (btnData.root == null) continue;

                bool canClaim = _gameModel.CanClaimBattlePassReward(btnData.rewardId);
                bool alreadyClaimed = _gameModel.ClaimedBattlePassRewards.Contains(btnData.rewardId);

                // Если уже забрали — полностью скрываем
                if (alreadyClaimed || !canClaim)
                {
                    btnData.root.SetActive(false);
                    continue;
                }

                // Показываем объект
                btnData.root.SetActive(true);

                // Кнопка активна только когда можно забрать
                if (btnData.button != null)
                    btnData.button.interactable = canClaim;
            }
        }

        private void UpdateTotalProgressTrack()
        {
            if (_gameModel == null || _achievementsConfig == null) return;

            levelText.text = $"{_gameModel.Points}";
            float totalProgressPercent = _gameModel.Points / (float)_achievementsConfig.TargetPoints;
            totalProgressSlider.value = Mathf.Clamp01(totalProgressPercent);
        }

        private void UpdateWeeklyTimer()
        {
            // Серверное время Яндекса
            long serverTimeMs = YG2.ServerTime();
            if (serverTimeMs <= 0)
                serverTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Переводим в московское время (UTC+3)
            DateTime utcNow = DateTimeOffset.FromUnixTimeMilliseconds(serverTimeMs).UtcDateTime;
            DateTime moscowNow = utcNow.AddHours(3);

            // Находим следующий понедельник 00:00 по Москве
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)moscowNow.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0 && moscowNow.TimeOfDay > TimeSpan.Zero)
            {
                // Если сегодня понедельник, но уже не 00:00 — берём следующий
                daysUntilMonday = 7;
            }

            DateTime nextResetMoscow = moscowNow.Date.AddDays(daysUntilMonday);
            TimeSpan timeRemaining = nextResetMoscow - moscowNow;

            // Если вдруг ушли в минус — сбрасываем
            if (timeRemaining.TotalSeconds <= 0)
            {
                ResetWeeklyStats();
                // После сброса пересчитываем на следующую неделю
                nextResetMoscow = moscowNow.Date.AddDays(7);
                timeRemaining = nextResetMoscow - moscowNow;
            }

            // Отображение
            int lang = _gameModel.Settings.Language == 0 ? 0 : 1;
            if (lang == 1)
                timerText.text = $"осталось {timeRemaining.Days}д {timeRemaining.Hours:D2}ч {timeRemaining.Minutes:D2}м";
            else
                timerText.text = $"{timeRemaining.Days}d {timeRemaining.Hours:D2}h {timeRemaining.Minutes:D2}m remaining";

            // Подсветка текущего уровня
            foreach (var item in levelNumList)
                item.SetActive(false);

            int levelIndex = _gameModel.Level - 1;
            if (levelIndex >= 0 && levelIndex < levelNumList.Count)
                levelNumList[levelIndex].SetActive(true);
        }

        private void ResetWeeklyStats()
        {
            _gameModel.ClearStatsAndProgress();
            RefreshUI();
        }

        private void OnClaimClicked(string achievementId)
        {
            rewardPanel.Show(() => { if (_gameModel.ClaimAchievement(achievementId)) RefreshUI(); },
                () => { if (_gameModel.ClaimAchievement(achievementId, 2)) RefreshUI(); },
                _achievementsConfig.AchievementsAll[achievementId].RewardMoney);            
        }
        
        private void OnLevelRewardClaimed(string rewardId)
        {
            rewardPanel.Show(() => { if (_gameModel.ClaimBattlePassReward(rewardId)) RefreshUI(); },
                () => { if (_gameModel.ClaimBattlePassReward(rewardId, 2)) RefreshUI(); },
                 _achievementsConfig.LevelRewards.Find(f => f.Id == rewardId).RewardMoney);
        }

        private void HandleLevelChanged(int points, int newLevel)
        {
        }

        // Вспомогательный класс для инспектора
        [Serializable]
        public class BattlePassRewardButton
        {
            public string rewardId;          // должен совпадать с Id в конфиге ("bp_500" и т.д.)
            public GameObject root;          // весь объект кнопки/награды
            public Button button;            // сама кнопка (можно оставить null, если root = кнопка)
        }
    }
}
