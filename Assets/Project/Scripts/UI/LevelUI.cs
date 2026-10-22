using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace NonameGame
{
    public class LevelUI : MonoBehaviour
    {
        public GameObject objectiveObj;
        public GameObject escObj;

        [SerializeField] private TextMeshProUGUI countdownText;

        // Время отсчета в секундах
        [SerializeField] private int startSeconds = 10;

        public void ShowObjectiveObj(bool value)
        {
            objectiveObj.SetActive(value);
        }

        public void ShowEscObj(bool value)
        {
            escObj.SetActive(value);
        }

        public void ShowKeepPlayingTimer()
        {
            StartCoroutine(StartCountdownRoutine());
        }

        private IEnumerator StartCountdownRoutine()
        {
            int currentTime = startSeconds;

            while (currentTime > 0)
            {
                // Обновляем текст на экране
                countdownText.text = currentTime.ToString();

                // Ждем ровно одну секунду
                yield return new WaitForSeconds(1f);

                // Уменьшаем счетчик
                currentTime--;
            }

            // Что происходит, когда время вышло
            countdownText.text = "0";
        }

        void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}
