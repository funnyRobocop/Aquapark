using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
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
        [SerializeField] private Button playButton;
        [SerializeField] private ShopUI shopUI;
        [SerializeField] private SettingsUI settingsUI;
        [SerializeField] private BattlePassUI battlePassUI;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI playerName;
        [SerializeField] private FusionNetworkManager fusionNetworkManager;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Camera camera;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject tutorArrow;

        private GameModel _gameModel;
        private AchievementsConfig _achievementsConfig;
        [Inject] private IPlayerSkinLoader _playerSkinLoader;
        [Inject] private SkinPreviewUI _skinPreviewUI;
        [Inject] private SoundManager _soundManager;

        public Canvas Canvas { get { return canvas; } }
        public Camera Camera { get { return camera; } }
        public CanvasGroup CanvasGroup { get { return canvasGroup; } }

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
            playButton.onClick.AddListener(OnPlayButtonClicked);

            if (_gameModel != null)
            {
                _gameModel.OnLevelChanged += UpdateLevelText;
                UpdateLevelText(_gameModel.Points, _gameModel.Level);
                _gameModel.OnPlayerNameChanged += UpdatePlayerNameText;
                UpdatePlayerNameText(_gameModel.PlayerName);
            }
        }

        void OnEnable()
        {
            if (_gameModel.SelectedSkinId < 0)
            {
                OnShopButtonClicked();
                tutorArrow?.SetActive(true);
                return;
            }
            SpawnPlayerMesh();
        }

        public void SpawnPlayerMesh()
        {
            _skinPreviewUI.SpawnPlayerMesh(_playerSkinLoader.PlayerSkinAll[(PlayerSkin)_gameModel.SelectedSkinId]);
        }

        public void Resume()
        {
            _skinPreviewUI.Show(-325f, 250f);
            tutorArrow?.SetActive(false);
        }

        private void UpdatePlayerNameText(string name)
        {
            playerName.text = name;
        }

        private void UpdateLevelText(int points, int level)
        {
            levelText.text = level.ToString();
        }

        private void OnDestroy()
        {
            settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
            shopButton.onClick.RemoveListener(OnShopButtonClicked);
            battlePassButton.onClick.RemoveListener(OnBattlePassButtonClicked);
            playButton.onClick.RemoveListener(OnPlayButtonClicked);

            if (_gameModel != null)
            {
                _gameModel.OnLevelChanged -= UpdateLevelText;
                _gameModel.OnPlayerNameChanged -= UpdatePlayerNameText;
            }
        }

        private void OnBattlePassButtonClicked()
        {
            _skinPreviewUI.Hide();
            battlePassUI.Show();
        }

        private void OnSettingsButtonClicked()
        {
            _skinPreviewUI.Hide();
            settingsUI.Show();
        }

        private void OnShopButtonClicked()
        {
            _skinPreviewUI.Hide();
            shopUI.Show();
        }

        private void OnPlayButtonClicked()
        {
            _skinPreviewUI.Hide();
            fusionNetworkManager.OnQuickJoinPressed();
            _soundManager.StopMusic();
        }
    }
}
