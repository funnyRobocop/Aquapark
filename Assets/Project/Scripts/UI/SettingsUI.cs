using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace NonameGame
{
    public class SettingsUI : MonoBehaviour
    {
        public enum Quality { Low, Medium, High }

        [Inject] private GameModel _gameModel;
        [Inject] private SoundManager _soundManager;

        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private FusionNetworkManager fusionNetworkManager;

        // Опционально: ссылки на UI-элементы, чтобы синхронизировать состояние при открытии
        [SerializeField] private GameObject musicOn;
        [SerializeField] private GameObject musicOff;
        [SerializeField] private GameObject soundOn;
        [SerializeField] private GameObject soundOff;
        [SerializeField] private GameObject notificationOn;
        [SerializeField] private GameObject notificationOff;
        [SerializeField] private GameObject qualityLowOn;
        [SerializeField] private GameObject qualityLowOff;
        [SerializeField] private GameObject qualityMediumOn;
        [SerializeField] private GameObject qualityMediumOff;
        [SerializeField] private GameObject qualityHighOn;
        [SerializeField] private GameObject qualityHighOff;

        [SerializeField] private TMP_Dropdown languageDropdown;
        [SerializeField] private TMP_Dropdown regionDropdown;

        private SettingsData _settings;

        void OnEnable()
        {
            nameInputField.text = _gameModel.PlayerName;

            _settings = SettingsData.Load();
            ApplySettingsToUI();
        }

        private void ApplySettingsToUI()
        {
            musicOn.SetActive(_settings.Music);
            musicOff.SetActive(!_settings.Music);
            soundOn.SetActive(_settings.Sound);
            soundOff.SetActive(!_settings.Sound);
            notificationOn.SetActive(_settings.Notification);
            notificationOff.SetActive(!_settings.Notification);
            
            qualityLowOn.SetActive(_settings.Quality == (int)Quality.Low);
            qualityLowOff.SetActive(_settings.Quality != (int)Quality.Low);
            qualityMediumOn.SetActive(_settings.Quality == (int)Quality.Medium);
            qualityMediumOff.SetActive(_settings.Quality != (int)Quality.Medium);
            qualityHighOn.SetActive(_settings.Quality == (int)Quality.High);
            qualityHighOff.SetActive(_settings.Quality != (int)Quality.High);

            if (languageDropdown != null)
                languageDropdown.SetValueWithoutNotify(_settings.Language);

            if (regionDropdown != null)
                regionDropdown.SetValueWithoutNotify(_settings.Region);
        }

        // Метод, который мы вызовем при нажатии на кнопку
        public void SavePlayerName()
        {
            // Считываем текст из поля ввода и удаляем лишние пробелы по краям
            string enteredName = nameInputField.text.Trim();

            // Проверяем, не оставил ли игрок поле пустым
            if (!string.IsNullOrEmpty(enteredName))
            {
                _gameModel.ChangeName(enteredName);
            }
            else
            {
                Debug.LogWarning("Поле ввода пустое! Пожалуйста, введите имя.");
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void OnChangeLanguage(int value)
        {
            if (_settings == null)
                _settings = SettingsData.Load();

            _settings.Language = value;
            _settings.Save();
            // Здесь можно добавить смену локализации, если она есть
        }

        public void OnChangeRegion(int value)
        {
            if (_settings == null)
                _settings = SettingsData.Load();

            _settings.Region = value;
            _settings.Save();

            fusionNetworkManager.OnChangeRegion(value);
        }

        public void OnChangeMusic(bool value)
        {
            if (_settings == null)
                _settings = SettingsData.Load();

            _settings.Music = value;
            _settings.Save();
            _soundManager.SetMusicEnabled(value);
        }

        public void OnChangeSoundFX(bool value)
        {
            if (_settings == null)
                _settings = SettingsData.Load();

            _settings.Sound = value;
            _settings.Save();
            _soundManager.SetSoundEnabled(value);
        }

        public void OnChangeNotification(bool value)
        {
            if (_settings == null)
                _settings = SettingsData.Load();

            _settings.Notification = value;
            _settings.Save();
        }

        public void OnChangeQuality(int value)
        {
            if (_settings == null)
                _settings = SettingsData.Load();

            _settings.Quality = value;
            _settings.Save();
            // Здесь можно применить QualitySettings.SetQualityLevel(value);
        }
    }
}