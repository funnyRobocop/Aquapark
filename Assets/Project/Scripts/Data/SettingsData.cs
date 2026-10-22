using UnityEngine;
using UnityEngine.Rendering;

namespace NonameGame
{
    public enum QualityType { Low, Medium, High }

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

        // Используем новые динамические значения по умолчанию
        public int Language { get; set; } = DefaultLanguage;
        public int Region { get; set; } = 0;
        public int Quality { get; set; } = DefaultQuality;

        /// <summary>
        /// Язык по умолчанию на основе системы: 0 = En, 1 = Ru
        /// </summary>
        public static int DefaultLanguage
        {
            get
            {
                // Проверяем, является ли системный язык русским, белорусским или украинским (опционально)
                if (Application.systemLanguage == SystemLanguage.Russian ||
                    Application.systemLanguage == SystemLanguage.Belarusian ||
                    Application.systemLanguage == SystemLanguage.Ukrainian)
                {
                    return 1; // Ru
                }

                return 0; // En для всех остальных языков
            }
        }

        public static int DefaultQuality
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.WebGPU)
                {
                    return (int)QualityType.High; // 2
                }
                else
                {
                    return (int)QualityType.Medium; // 1
                }
#else
                return (int)QualityType.Medium; // 1 по умолчанию для остальных платформ
#endif
            }
        }

        public static SettingsData Load()
        {
            var data = new SettingsData
            {
                Music = PlayerPrefs.GetInt(KeyMusic, 1) == 1,
                Sound = PlayerPrefs.GetInt(KeySound, 1) == 1,
                Notification = PlayerPrefs.GetInt(KeyNotification, 1) == 1,
                Language = PlayerPrefs.GetInt(KeyLanguage, DefaultLanguage),
                Region = PlayerPrefs.GetInt(KeyRegion, 0),
                Quality = PlayerPrefs.GetInt(KeyQuality, DefaultQuality)
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
