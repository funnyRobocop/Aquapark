using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace NonameGame
{
    public class SettingsUI : MonoBehaviour
    {

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
    }
}
