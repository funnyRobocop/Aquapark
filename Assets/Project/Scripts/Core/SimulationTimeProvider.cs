using Fusion;
using UnityEngine;


namespace NonameGame
{
    public class SimulationTimeProvider : NetworkBehaviour
    {
        public static SimulationTimeProvider Instance { get; private set; }
        public static float Time { get; private set; }

        public override void Spawned()
        {
            Instance = this;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this)
                Instance = null;
        }

        public override void FixedUpdateNetwork()
        {
            Time = (float)Runner.SimulationTime;
        }
    }
}