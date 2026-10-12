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

            // Проверка нажатия клавиши Numpad 1 в этот кадр
            if (keyboard.numpad1Key.wasPressedThisFrame)
            {
                _gameModel.ClaimBattlePassMoney(100);
            }
        }
    }
}