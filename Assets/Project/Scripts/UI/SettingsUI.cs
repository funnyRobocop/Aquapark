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

        [SerializeField] private TMP_InputField nameInputField;

        void OnEnable()
        {
            nameInputField.text = _gameModel.PlayerName;
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

        public void OnLaguageChange(int value)
        {
            if (value == 0)
            {
                Debug.Log("En");
            }
            else
            {
                Debug.Log("Ru");
            }
        }

        public void OnMusicChange(bool value)
        {
            Debug.Log("Music " + value);
        }

        public void OnSoundFXChange(int value)
        {
            Debug.Log("Sound " + value);
        }

        public void OnQualityChange(int value)
        {
            Debug.Log("Quality " + (Quality) value);
        }
    }
}
