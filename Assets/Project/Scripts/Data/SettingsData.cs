using UnityEngine;

namespace NonameGame
{
    /// <summary>
    /// Данные настроек игры. Сохраняются и загружаются через PlayerPrefs.
    /// </summary>
    public class SettingsData
    {
        private const string KeyMusic = "Settings_Music";
        private const string KeySound = "Settings_Sound";
        private const string KeyNotification = "Settings_Notification";
        private const string KeyLanguage = "Settings_Language";
        private const string KeyRegion = "Settings_Region";
        private const string KeyQuality = "Settings_Quality";

        public bool Music { get; set; } = true;
        public bool Sound { get; set; } = true;
        public bool Notification { get; set; } = true;
        public int Language { get; set; } = 0;
        public int Region { get; set; } = 0;
        public int Quality { get; set; } = 1; // Medium by default

        public static SettingsData Load()
        {
            var data = new SettingsData
            {
                Music = PlayerPrefs.GetInt(KeyMusic, 1) == 1,
                Sound = PlayerPrefs.GetInt(KeySound, 1) == 1,
                Notification = PlayerPrefs.GetInt(KeyNotification, 1) == 1,
                Language = PlayerPrefs.GetInt(KeyLanguage, 0),
                Region = PlayerPrefs.GetInt(KeyRegion, 0),
                Quality = PlayerPrefs.GetInt(KeyQuality, 1)
            };
            return data;
        }

        public void Save()
        {
            PlayerPrefs.SetInt(KeyMusic, Music ? 1 : 0);
            PlayerPrefs.SetInt(KeySound, Sound ? 1 : 0);
            PlayerPrefs.SetInt(KeyNotification, Notification ? 1 : 0);
            PlayerPrefs.SetInt(KeyLanguage, Language);
            PlayerPrefs.SetInt(KeyRegion, Region);
            PlayerPrefs.SetInt(KeyQuality, Quality);
            PlayerPrefs.Save();
        }
    }
}