using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using YG;

namespace NonameGame
{
    public class Test : MonoBehaviour
    {
        [Inject] private GameModel _gameModel;

        FusionNetworkManager _fusionNetworkManager;

        void Awake()
        {
            _fusionNetworkManager = FindObjectOfType<FusionNetworkManager>();
        }

        // Update is called once per frame
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.numpad1Key.wasPressedThisFrame)
            {
                _gameModel.ClaimBattlePassMoney(100);
            }
            
            if (keyboard.numpad2Key.wasPressedThisFrame)
            {
                _gameModel.UpdateStatsAndProgress(new GameModel.Stats(0, 1, 1, 5, 5));
            }

            if (keyboard.numpad3Key.wasPressedThisFrame)
            {
                _gameModel.UpdateStatsAndProgress(new GameModel.Stats(0, 0, 1, 5, 5));
            }

            if (keyboard.numpad4Key.wasPressedThisFrame)
            {
                _gameModel.UpdateStatsAndProgress(new GameModel.Stats(0, 0, 0, 5, 5));
            }

            if (keyboard.numpad5Key.wasPressedThisFrame)
            {
                _gameModel.ClearStatsAndProgress();
            }

            if (keyboard.numpad6Key.wasPressedThisFrame)
            {
                YG2.saves = new SavesYG();
                YG2.SaveProgress();
            }

            if (keyboard.numpad7Key.wasPressedThisFrame)
            {
                _fusionNetworkManager.LevelIndex = 1;
            }

            if (keyboard.numpad8Key.wasPressedThisFrame)
            {
                _fusionNetworkManager.LevelIndex = 2;
            }

            if (keyboard.numpad9Key.wasPressedThisFrame)
            {
                _fusionNetworkManager.LevelIndex = 3;
            }
        }
    }
}