using UnityEngine;
using VContainer.Unity;

namespace NonameGame
{
    public class SoundManager : MonoBehaviour, IStartable
    {
        [Header("Настройки аудио")]
        public AudioSource audioSource1;
        public AudioSource audioSource2;
        public AudioSource sfxSource;
        [SerializeField] private AudioClip[] popClips;
        [SerializeField] private AudioClip wispClip;

        [Header("Количество повторов")]
        public int clip1RepeatCount = 10;
        public int clip2RepeatCount = 4;

        private int currentRepeat = 0;
        private bool playingFirstClip = true;

        private bool musicEnabled = true;
        private bool soundEnabled = true;

        public bool MusicEnabled => musicEnabled;
        public bool SoundEnabled => soundEnabled;

        // Метод интерфейса IStartable, который вызовет сам VContainer на старте игры
        void IStartable.Start()
        {
            if (audioSource1 == null || audioSource2 == null)
            {
                Debug.LogError("Забыли настроить AudioSource в Инспекторе префаба SoundManager!");
                return;
            }

            audioSource1.loop = false;
            audioSource2.loop = false;

            // Применяем сохранённые настройки
            var settings = SettingsData.Load();
            musicEnabled = settings.Music;
            soundEnabled = settings.Sound;

            // Запускаем музыку только если она включена
            if (musicEnabled)
            {
                audioSource1.Play();
            }
        }

        /// <summary>
        /// Включить / выключить фоновую музыку.
        /// </summary>
        public void SetMusicEnabled(bool enabled)
        {
            if (musicEnabled == enabled) return;

            musicEnabled = enabled;

            if (enabled)
            {
                // Возобновляем с текущего трека
                AudioSource activeSource = playingFirstClip ? audioSource1 : audioSource2;
                if (!activeSource.isPlaying)
                {
                    activeSource.Play();
                }
            }
            else
            {
                audioSource1.Stop();
                audioSource2.Stop();
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
            if (!soundEnabled) return;
            if (popClips == null || popClips.Length == 0) return;

            int randomIndex = Random.Range(0, popClips.Length);
            sfxSource.PlayOneShot(popClips[randomIndex]);
        }

        public void PlayWispSound()
        {
            if (!soundEnabled) return;
            if (wispClip == null) return;

            sfxSource.PlayOneShot(wispClip);
        }

        void Update()
        {
            // Музыка выключена — ничего не делаем
            if (!musicEnabled) return;

            // Берем тот источник, который должен играть прямо сейчас
            AudioSource activeSource = playingFirstClip ? audioSource1 : audioSource2;

            // Как только активный сэмпл ПОЛНОСТЬЮ закончил играть
            if (!activeSource.isPlaying)
            {
                currentRepeat++;

                if (playingFirstClip && currentRepeat >= clip1RepeatCount)
                {
                    playingFirstClip = false;
                    currentRepeat = 0;
                }
                else if (!playingFirstClip && currentRepeat >= clip2RepeatCount)
                {
                    playingFirstClip = true;
                    currentRepeat = 0;
                }

                AudioSource nextSource = playingFirstClip ? audioSource1 : audioSource2;
                nextSource.Play();
            }
        }
    }
}