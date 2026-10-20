using UnityEngine;
using VContainer.Unity; // Обязательно добавляем этот namespace

namespace NonameGame
{
    public class SoundManager : MonoBehaviour, IStartable
    {
        [Header("Настройки аудио")]
        public AudioSource audioSource1;
        public AudioSource audioSource2;

        [Header("Количество повторов")]
        public int clip1RepeatCount = 10;
        public int clip2RepeatCount = 4;

        private int currentRepeat = 0;
        private bool playingFirstClip = true;

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

            // Запускаем музыку в самый первый миг жизни игры
            audioSource1.Play();
        }

        void Update()
        {
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
