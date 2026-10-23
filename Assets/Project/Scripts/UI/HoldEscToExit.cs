using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


namespace NonameGame
{
    public class HoldEscToExit : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float holdDuration = 5f;          // Время удержания в секундах
        [SerializeField] private Image fillImage;                  // Image с Fill Amount (круг)

        [SerializeField] private GameObject rootView;

        [Header("Optional")]
        [SerializeField] private bool resetOnRelease = true;       // Сбрасывать прогресс, если отпустили раньше

        // Событие, которое ты можешь подписать в инспекторе или из кода
        public event Action OnExitTriggered;

        private float holdTimer = 0f;
        private bool isHolding = false;
        private bool hasTriggered = false;

        private void Update()
        {
            // Проверяем, нажата ли клавиша Escape (новая Input System)
            bool isEscPressed = Keyboard.current != null && Keyboard.current.escapeKey.isPressed;

            if (isEscPressed)
            {
                if (!isHolding)
                {
                    isHolding = true;
                    hasTriggered = false;
                    rootView.SetActive(true);
                }

                holdTimer += Time.unscaledDeltaTime; // unscaled, чтобы работало даже на паузе

                // Заполняем Image
                if (fillImage != null)
                {
                    fillImage.fillAmount = Mathf.Clamp01(holdTimer / holdDuration);
                }

                // Достигли нужного времени
                if (holdTimer >= holdDuration && !hasTriggered)
                {
                    hasTriggered = true;
                    TriggerExit();
                }
            }
            else
            {
                // Отпустили кнопку
                if (isHolding && resetOnRelease)
                {
                    ResetProgress();
                    rootView.SetActive(false);
                }

                isHolding = false;
            }
        }

        private void TriggerExit()
        {
            OnExit();

            OnExitTriggered?.Invoke();
        }

        private void OnExit()
        {
            Debug.Log("Выход из игры / меню");

            InGameManager.Instance?.LeaveGame();
        }

        public void ResetProgress()
        {
            holdTimer = 0f;
            hasTriggered = false;

            if (fillImage != null)
                fillImage.fillAmount = 0f;
        }

        public void ForceReset()
        {
            ResetProgress();
            isHolding = false;
        }
    }
}

