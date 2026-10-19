using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NonameGame
{
    [RequireComponent(typeof(Button))]
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Settings")]
        [SerializeField] private Image progressCircle;
        [SerializeField] private float holdDuration = 1.0f;

        private Button _button;
        private bool _isHolding = false;
        private float _currentHoldTime = 0f;

        public event Action OnHoldComplete;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (progressCircle != null) progressCircle.fillAmount = 0f;
        }

        private void Update()
        {
            if (_isHolding && _button.interactable)
            {
                _currentHoldTime += Time.deltaTime;

                if (progressCircle != null)
                {
                    progressCircle.fillAmount = Mathf.Clamp01(_currentHoldTime / holdDuration);
                }

                if (_currentHoldTime >= holdDuration)
                {
                    ResetHold();
                    OnHoldComplete?.Invoke(); // Вызываем событие успешного удержания
                }
            }
            else
            {
                if (progressCircle != null && progressCircle.fillAmount > 0f)
                {
                    // Плавно сбрасываем прогресс, как в Fall Guys
                    progressCircle.fillAmount = Mathf.MoveTowards(progressCircle.fillAmount, 0f, Time.deltaTime * 3f);
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button.interactable)
            {
                _isHolding = true;
                _currentHoldTime = 0f;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetHold();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ResetHold(); // Сброс, если игрок увёл курсор/палец с кнопки
        }

        public void ResetHold()
        {
            _isHolding = false;
            _currentHoldTime = 0f;
        }

        private void OnDisable()
        {
            ResetHold();
            if (progressCircle != null) progressCircle.fillAmount = 0f;
        }
    }
}
