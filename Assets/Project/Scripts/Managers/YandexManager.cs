using System;
using UnityEngine;
using YG;
using VContainer;

namespace NonameGame
{
    public class YandexManager : MonoBehaviour
    {
        [Inject] private GameModel _gameModel;

        public static YandexManager Instance { get; private set; }

        // Готовность SDK
        public bool IsSDKReady { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void OnEnable()
        {
            YG2.onGetSDKData += OnSDKReady;
        }

        private void OnDisable()
        {
            YG2.onGetSDKData -= OnSDKReady;
        }

        private void Start()
        {
            // Если SDK уже успел инициализироваться до Start
            if (YG2.isSDKEnabled)
                OnSDKReady();
        }

        private void OnSDKReady()
        {
            if (IsSDKReady) return;
            IsSDKReady = true;

            // --- Имя игрока ---
            SetPlayerName();

            Debug.Log($"[YandexManager] SDK Ready. Player: {YG2.player.name}, Auth: {YG2.player.auth}");
        }

        private void SetPlayerName()
        {
            if (string.IsNullOrEmpty(_gameModel.PlayerName) == false)
                return;

            string name = YG2.player.name;

            // "unauthorized" / "anonymous" — типичные значения от плагина
            if (string.IsNullOrEmpty(name) ||
                name == "unauthorized" ||
                name == "anonymous")
            {
                name = "Player" + UnityEngine.Random.Range(0,100);
            }

            _gameModel.ChangeName(name);
        }

        // ===================== РЕКЛАМА =====================

        /// <summary>
        /// Межстраничная реклама
        /// </summary>
        public void ShowInterstitial()
        {
            YG2.InterstitialAdvShow();
        }

        /// <summary>
        /// Реклама за вознаграждение
        /// </summary>
        /// <param name="rewardId">любой свой id (например "double_coins", "extra_life")</param>
        /// <param name="onRewarded">что выдать после успешного просмотра</param>
        public void ShowRewarded(string rewardId, Action onRewarded)
        {
            YG2.RewardedAdvShow(rewardId, onRewarded);
        }

        // Баннеры (Sticky) — на Яндексе работают сами.
        // Этот метод нужен только если хочешь принудительно скрыть/показать.
        /*public void SetStickyBanner(bool active)
        {
            YG2.StickyAdActivity(active);
        }*/

        // ===================== СЕРВЕРНОЕ ВРЕМЯ =====================

        /// <summary>
        /// Возвращает серверное время в миллисекундах (Unix)
        /// </summary>
        public long GetServerTime()
        {
            return YG2.ServerTime();
        }

        // ===================== СОХРАНЕНИЯ =====================

        /// <summary>
        /// Сохранить прогресс в облако
        /// </summary>
        public void SaveProgress()
        {
            YG2.SaveProgress();
        }

        // Данные лежат в YG2.saves
        // Пример: YG2.saves.coins = 100;
    }
}