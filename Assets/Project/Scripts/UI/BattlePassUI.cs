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
        [SerializeField] private Image totalProgressSlider; // Image с типажом Filled


        [Header("Weekly Quests UI")]
        [SerializeField] private TextMeshProUGUI throwText;
        [SerializeField] private Image throwProgressSlider;

        [SerializeField] private TextMeshProUGUI kickText;
        [SerializeField] private Image kickProgressSlider;

        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private Image goldProgressSlider;
        [SerializeField] private TextMeshProUGUI finishText;
        [SerializeField] private Image finishProgressSlider;

        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI timerText;

        private GameModel _gameModel;
        private AchievementsConfig _achievementsConfig;

        // VContainer автоматически внедрит зависимость при спавне или инициализации сцены
        [Inject]
        public void Construct(GameModel gameModel, AchievementsConfig achievementsConfig)
        {
            _gameModel = gameModel;
            _achievementsConfig = achievementsConfig;
        }

        private void OnEnable()
        {
            if (_gameModel != null)
            {
                // Подписываемся на изменения данных, чтобы интерфейс обновлялся на лету
                _gameModel.OnDataChanged += RefreshUI;
                _gameModel.OnLevelChanged += HandleLevelChanged;
            }

            RefreshUI();
            HandleLevelChanged(_gameModel.Level);
            UpdateWeeklyTimer();
        }

        private void OnDisable()
        {
            if (_gameModel != null)
            {
                _gameModel.OnDataChanged -= RefreshUI;
                _gameModel.OnLevelChanged -= HandleLevelChanged;
            }
        }

        private void RefreshUI()
        {
            if (_gameModel == null) return;

            // 1. Отображение уровня игрока
            levelText.text = $"УРОВЕНЬ {_gameModel.Level}";

            // 2. Считаем суммы всех параметров из массива StatsList
            int totalGold = _gameModel.StatsList.Sum(s => s.GoldCount);
            int totalFinish = _gameModel.StatsList.Sum(s => s.FinishCount);
            int totalThrows = _gameModel.StatsList.Sum(s => s.ThrowCount);
            int totalKicks = _gameModel.StatsList.Sum(s => s.KickCount);

            // 3. Расчет главного прогресс-бара уровня (ваша будущая формула)
            float totalProgressPercent = CalculateTotalProgress(totalThrows, totalKicks, totalGold, totalFinish);
            totalProgressSlider.fillAmount = Mathf.Clamp01(totalProgressPercent);

            // 4. Обновление недельных квестов
            UpdateQuestTrack(throwText, throwProgressSlider, totalThrows, _achievementsConfig.TargetThrowCount, "Броски");
            UpdateQuestTrack(kickText, kickProgressSlider, totalKicks, _achievementsConfig.TargetKickCount, "Удары");
            UpdateQuestTrack(goldText, goldProgressSlider, totalGold, _achievementsConfig.TargetGoldCount, "Золото");
            UpdateQuestTrack(finishText, finishProgressSlider, totalFinish, _achievementsConfig.TargetFinishCount, "Завершения");
        }

        // Пример вашей формулы прогресса уровня
        private float CalculateTotalProgress(int throws, int kicks, int gold, int finish)
        {
            // Например: каждый бросок дает 10 очков, кик — 15, золото — 100.
            // Для достижения следующего уровня нужно, допустим, 5000 очков (или прогрессия от текущего уровня).
            float currentPoints = (throws * 10) + (kicks * 15) + (gold * 100) + (finish * 50);

            // Настоятельно рекомендуем делать шкалу необходимого опыта динамической
            float requiredPointsForNextLevel = 1000 * _gameModel.Level;

            return currentPoints / requiredPointsForNextLevel;
        }

        // Вспомогательный метод для красивого заполнения баров подзадач
        private void UpdateQuestTrack(TextMeshProUGUI label, Image slider, int current, int target, string questName)
        {
            label.text = $"{questName}: {current} / {target}";
            slider.fillAmount = (float)current / target;
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
            timerText.text = $"До сброса: {timeRemaining.Days}д {timeRemaining.Hours}ч {timeRemaining.Minutes}м";

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

        private void HandleLevelChanged(int newLevel)
        {
            Debug.Log($"Поздравляем! Новый уровень боевого пропуска: {newLevel}");
            // Здесь можно включить красивый UI-эффект вспышки нового уровня
        }
    }
}
