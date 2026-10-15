using UnityEngine;
using UnityEngine.InputSystem;

namespace NonameGame
{
    public class Test2 : MonoBehaviour
    {
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.numpad8Key.wasPressedThisFrame)
            {
                var enabled = FindAnyObjectByType<SimulationTimeProvider>().enabled;
                FindAnyObjectByType<SimulationTimeProvider>().enabled = !enabled;
            }
        }
    }
}
