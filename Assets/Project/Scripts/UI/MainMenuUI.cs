using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace NonameGame
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button battlePassButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private ShopUI shopUI;
        [SerializeField] private SettingsUI settingsUI;
        [SerializeField] private BattlePassUI battlePassUI;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI moneyText;

        private GameModel _gameModel;
        private AchievementsConfig _achievementsConfig;

        [Inject]
        public void Construct(GameModel gameModel, AchievementsConfig achievementsConfig)
        {
            _gameModel = gameModel;
            _achievementsConfig = achievementsConfig;
        }
        
        private void Awake()
        {
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            shopButton.onClick.AddListener(OnShopButtonClicked);
            battlePassButton.onClick.AddListener(OnBattlePassButtonClicked);

            if (_gameModel != null)
            {
                _gameModel.OnLevelChanged += UpdateLevelText;
                UpdateLevelText(_gameModel.Level);
                _gameModel.OnMoneyChanged += UpdateMoneyText;
                UpdateMoneyText(_gameModel.Money);  
            }
        }

        private void UpdateMoneyText(int money)
        {
            moneyText.text = money.ToString();
        }

        private void UpdateLevelText(int level)
        {
            levelText.text = level.ToString();
        }

        private void OnDestroy()
        {
            settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
            shopButton.onClick.RemoveListener(OnShopButtonClicked);
            battlePassButton.onClick.RemoveListener(OnBattlePassButtonClicked);

            if (_gameModel != null)
            {
                _gameModel.OnLevelChanged -= UpdateLevelText;
                _gameModel.OnMoneyChanged -= UpdateMoneyText;
            }
        }

        private void OnBattlePassButtonClicked()
        {
            battlePassUI.Show();
        }

        private void OnSettingsButtonClicked()
        {
            settingsUI.Show();
        }

        private void OnShopButtonClicked()
        {
            shopUI.Show();
        }
    }
}
