using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace NonameGame
{
    public class Test : MonoBehaviour
    {
        [Inject] private GameModel _gameModel;

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
                _gameModel.UpdateStatsAndProgress(new GameModel.Stats(0, 1, 1, 5, 2));
            }

            if (keyboard.numpad3Key.wasPressedThisFrame)
            {
                _gameModel.UpdateStatsAndProgress( new GameModel.Stats(0, 0, 1, 5, 2));
            }

            if (keyboard.numpad4Key.wasPressedThisFrame)
            {
                _gameModel.UpdateStatsAndProgress( new GameModel.Stats(0, 0, 0, 10, 10));
            }

            if (keyboard.numpad5Key.wasPressedThisFrame)
            {
                _gameModel.ClearStatsAndProgress();
            }
        }
    }
}