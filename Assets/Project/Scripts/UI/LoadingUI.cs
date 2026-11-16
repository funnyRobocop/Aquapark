using UnityEngine.UI;
using UnityEngine;
using System.Collections;

namespace NonameGame
{
    public class LoadingUI : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject[] _hints;
        [SerializeField] private Slider _slider;

        [Header("Настройки времени")]
        [SerializeField] private float minShowTime = 1f;
        [SerializeField] private float fadeInDuration = 0.25f;
        [SerializeField] private float fadeOutDuration = 0.3f;

        private float _showTimestamp;
        private Coroutine _transitionCoroutine;

        void Awake()
        {
            canvas.enabled = false;
            canvasGroup.alpha = 0f;

            foreach (var item in _hints)
                item.SetActive(false);
        }

        public void Show(bool needHints = false)
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }

            // Сразу блокируем клики, включаем канвас и сбрасываем прогресс
            canvasGroup.blocksRaycasts = true;
            canvas.enabled = true;
            _slider.value = 0f;

            if (_hints != null && _hints.Length > 0)
            {
                foreach (var item in _hints)
                    item.SetActive(false);

                if (needHints)
                {
                    var rnd = Random.Range(0, _hints.Length);
                    _hints[rnd].SetActive(true);
                }
            }

            _showTimestamp = Time.unscaledTime;
            _transitionCoroutine = StartCoroutine(FadeInRoutine());
        }

        private IEnumerator FadeInRoutine()
        {
            float startAlpha = canvas.enabled ? canvasGroup.alpha : 0f;
            // Если канвас был выключен, начинаем с полной прозрачности
            if (Mathf.Approximately(startAlpha, 1f))
                startAlpha = 0f;

            canvasGroup.alpha = startAlpha;

            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / fadeInDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            _transitionCoroutine = null;
        }

        public void Hide()
        {
            float elapsedTime = Time.unscaledTime - _showTimestamp;
            float remainingTime = Mathf.Max(0f, minShowTime - elapsedTime);

            // Останавливаем текущую анимацию (например, если еще идет fade-in)
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }

            _transitionCoroutine = StartCoroutine(HideRoutine(remainingTime));
        }

        private IEnumerator HideRoutine(float delay)
        {
            // 1. Ждем остаток от minShowTime (заодно за это время завершится fade-in, если он короткий)
            if (delay > 0f)
            {
                // Плавно доводим альфу до 1 во время ожидания, если Hide вызвали во время fade-in
                float waitElapsed = 0f;
                float currentAlpha = canvasGroup.alpha;
                while (waitElapsed < delay)
                {
                    waitElapsed += Time.unscaledDeltaTime;
                    if (canvasGroup.alpha < 1f)
                    {
                        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.unscaledDeltaTime / fadeInDuration);
                    }
                    yield return null;
                }
            }

            // 2. Разрешаем клики сквозь интерфейс перед затуханием
            canvasGroup.blocksRaycasts = false;

            // 3. Плавное затухание от текущей прозрачности до 0
            float startAlpha = canvasGroup.alpha;
            float fadeElapsed = 0f;

            while (fadeElapsed < fadeOutDuration)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, fadeElapsed / fadeOutDuration);
                yield return null;
            }

            // 4. Финальное отключение
            canvasGroup.alpha = 0f;
            canvas.enabled = false;
            _transitionCoroutine = null;
        }

        public void SetProgress(float value) // 0..1
        {
            _slider.value = value;
        }
    }
}
