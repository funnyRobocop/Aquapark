using UnityEngine;
using VContainer.Unity;

namespace NonameGame
{
    public class SoundManager : MonoBehaviour, IStartable
    {
        [Header("Настройки аудио")]
        public AudioSource audioSource; // Оставили один источник для фоновой музыки
        public AudioSource sfxSource;
        [SerializeField] private AudioClip[] popClips;
        [SerializeField] private AudioClip wispClip;

        private bool musicEnabled = true;
        private bool soundEnabled = true;

        public bool MusicEnabled => musicEnabled;
        public bool SoundEnabled => soundEnabled;

        // Метод интерфейса IStartable
        void IStartable.Start()
        {
            return;
            if (audioSource == null)
            {
                Debug.LogError("Забыли настроить AudioSource в Инспекторе префаба SoundManager!");
                return;
            }

            // Включаем зацикливание средствами самого Unity
            audioSource.loop = true;

            // Запускаем музыку только если она была включена при инициализации GameModel
            if (musicEnabled)
            {
                audioSource.Play();
            }
        }

        public void StopMusic()
        {
            return;
            if (musicEnabled)
            {
                audioSource.Stop();
            }
        }
        public void PlayMusic()
        {
            return;
            if (musicEnabled)
            {
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
        }

        /// <summary>
        /// Включить / выключить фоновую музыку.
        /// </summary>
        public void SetMusicEnabled(bool enabled)
        {
            return;
            if (musicEnabled == enabled) return;

            musicEnabled = enabled;

            if (enabled)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
            }
            else
            {
                audioSource.Stop();
            }
        }

        /// <summary>
        /// Включить / выключить звуковые эффекты.
        /// </summary>
        public void SetSoundEnabled(bool enabled)
        {
            soundEnabled = enabled;
        }

        public void PlayPopSound()
        {
            return;
            if (!soundEnabled) return;
            if (popClips == null || popClips.Length == 0) return;

            int randomIndex = Random.Range(0, popClips.Length);
            sfxSource.PlayOneShot(popClips[randomIndex]);
        }

        public void PlayWispSound()
        {
            return; //todo пока не найду звук нормальный
            if (!soundEnabled) return;
            if (wispClip == null) return;

            sfxSource.PlayOneShot(wispClip);
        }
    }
}