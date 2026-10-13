using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace NonameGame
{
    public class BattlePassUI : MonoBehaviour
    {
        [Header("Main Progress")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Slider totalProgressSlider;


        [Header("Weekly Quests UI")]
        [SerializeField] private TextMeshProUGUI throwText;
        [SerializeField] private Slider throwProgressSlider;

        [SerializeField] private TextMeshProUGUI kickText;
        [SerializeField] private Slider kickProgressSlider;

        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private Slider goldProgressSlider;
        [SerializeField] private TextMeshProUGUI finishText;
        [SerializeField] private Slider finishProgressSlider;

        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI timerText;

        [SerializeField] private Button closeButton;

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
            closeButton.onClick.AddListener(Hide);

            if (_gameModel != null)
            {
                // Подписываемся на изменения данных, чтобы интерфейс обновлялся на лету
                _gameModel.OnDataChanged += RefreshUI;
                _gameModel.OnLevelChanged += HandleLevelChanged;
            }
        }

        private void OnDestroy()
        {
            closeButton.onClick.RemoveListener(Hide);

            if (_gameModel != null)
            {
                _gameModel.OnDataChanged -= RefreshUI;
                _gameModel.OnLevelChanged -= HandleLevelChanged;
            }
        }

        private void OnEnable()
        {
            RefreshUI();
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
            if (_gameModel == null) return;

            // 1. Отображение уровня игрока
            levelText.text = $"{_gameModel.Points}";

            // 2. Считаем суммы всех параметров из массива StatsList
            int totalGold = _gameModel.StatsList.Sum(s => s.GoldCount);
            int totalFinish = _gameModel.StatsList.Sum(s => s.FinishCount);
            int totalThrows = _gameModel.StatsList.Sum(s => s.ThrowCount);
            int totalKicks = _gameModel.StatsList.Sum(s => s.KickCount);

            // 3. Расчет главного прогресс-бара уровня (ваша будущая формула)
            float totalProgressPercent = _gameModel.Points / (float)_achievementsConfig.TargetPoints;
            totalProgressSlider.value = Mathf.Clamp01(totalProgressPercent);

            // 4. Обновление недельных квестов
            UpdateQuestTrack(throwText, throwProgressSlider, totalThrows, _achievementsConfig.TargetThrowCount);
            UpdateQuestTrack(kickText, kickProgressSlider, totalKicks, _achievementsConfig.TargetKickCount);
            UpdateQuestTrack(goldText, goldProgressSlider, totalGold, _achievementsConfig.TargetGoldCount);
            UpdateQuestTrack(finishText, finishProgressSlider, totalFinish, _achievementsConfig.TargetFinishCount);
        }

        // Вспомогательный метод для красивого заполнения баров подзадач
        private void UpdateQuestTrack(TextMeshProUGUI label, Slider slider, int current, int target)
        {
            label.text = $"{current} / {target}";
            slider.value = Mathf.Clamp01(current / (float)target);
            Debug.Log(slider.value);
        }

        // Расчет и вывод времени до конца недели
        private void UpdateWeeklyTimer()
        {
            DateTime now = DateTime.UtcNow;

            // Вычисляем следующий понедельник 00:00:00
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0 && now.Hour >= 0) daysUntilMonday = 7; // Если сегодня уже понедельник, берем следующий

            DateTime nextResetTime = now.Date.AddDays(daysUntilMonday);

            TimeSpan timeRemaining = nextResetTime - now;

            // Форматируем строку: Дней:Часов:Минут (секунды по желанию)
            timerText.text = $"{timeRemaining.Days}d {timeRemaining.Hours}h {timeRemaining.Minutes}m";

            // Если таймер истек в ноль (прошла неделя) — отправляем запрос на очистку
            if (timeRemaining.TotalSeconds <= 0)
            {
                ResetWeeklyStats();
            }
        }

        private void ResetWeeklyStats()
        {
            // Очищаем локальный список или обнуляем показатели в GameModel
            _gameModel.StatsList.Clear();
            // Сохраняем пустой прогресс
            RefreshUI();
        }

        private void HandleLevelChanged(int points, int newLevel)
        {
            Debug.Log($"Поздравляем! Новый уровень боевого пропуска: {newLevel}");
            // Здесь можно включить красивый UI-эффект вспышки нового уровня
        }
    }
}
