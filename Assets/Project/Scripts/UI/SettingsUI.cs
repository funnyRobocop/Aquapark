using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings; // Добавлено пространство имен локализации
using UnityEngine.UI;
using VContainer;

namespace NonameGame
{
    public class SettingsUI : MonoBehaviour
    {
        [Inject] private GameModel _gameModel;
        [Inject] private SoundManager _soundManager;

        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private FusionNetworkManager fusionNetworkManager;

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
        private Coroutine _setLocaleCoroutine;

        void OnEnable()
        {
            nameInputField.text = _gameModel.PlayerName;
            _settings = _gameModel.Settings;

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

            qualityLowOn.SetActive(_settings.Quality == (int)QualityType.Low);
            qualityLowOff.SetActive(_settings.Quality != (int)QualityType.Low);
            qualityMediumOn.SetActive(_settings.Quality == (int)QualityType.Medium);
            qualityMediumOff.SetActive(_settings.Quality != (int)QualityType.Medium);
            qualityHighOn.SetActive(_settings.Quality == (int)QualityType.High);
            qualityHighOff.SetActive(_settings.Quality != (int)QualityType.High);

            if (languageDropdown != null)
                languageDropdown.SetValueWithoutNotify(_settings.Language);

            if (regionDropdown != null)
                regionDropdown.SetValueWithoutNotify(_settings.Region);
        }

        public void SavePlayerName()
        {
            string enteredName = nameInputField.text.Trim();

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

            SetLocale(value);
        }

        private void SetLocale(int localeIndex)
        {
            if (_setLocaleCoroutine != null)
                StopCoroutine(_setLocaleCoroutine);

            _setLocaleCoroutine = StartCoroutine(SetLocaleRoutine(localeIndex));
        }

        private IEnumerator SetLocaleRoutine(int localeIndex)
        {
            yield return LocalizationSettings.InitializationOperation;

            if (localeIndex >= 0 && localeIndex < LocalizationSettings.AvailableLocales.Locales.Count)
            {
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeIndex];
            }
            else
            {
                Debug.LogError($"Индекс локали {localeIndex} выходит за рамки доступных в LocalizationSettings!");
            }
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
        }
    }
}
