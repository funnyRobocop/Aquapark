using TMPro;
using UnityEngine;
using VContainer;

namespace NonameGame
{
    public class MoneyPanelUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI moneyText;

        [Inject] private GameModel _gameModel;

        private void Awake()
        {
            if (_gameModel != null)
            {
                _gameModel.OnMoneyChanged += UpdateMoneyText;
                UpdateMoneyText(_gameModel.Money);
            }
        }

        private void OnDestroy()
        {
            if (_gameModel != null)
            {
                _gameModel.OnMoneyChanged -= UpdateMoneyText;
            }
        }

        private void UpdateMoneyText(int money)
        {
            moneyText.text = money.ToString() + "$";
        }
    }
}
