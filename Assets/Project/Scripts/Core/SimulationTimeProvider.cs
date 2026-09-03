using Fusion;
using UnityEngine;


namespace NonameGame
{
    public class SimulationTimeProvider : MonoBehaviour
    {
        public static SimulationTimeProvider Instance { get; private set; }
        public static float Time { get; private set; }

        private NetworkRunner _runner;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void FixedUpdate()
        {
            if (_runner == null || !_runner.IsRunning)
            {
                _runner = NetworkRunner.GetRunnerForGameObject(gameObject);
                if (_runner == null)
                    _runner = FindAnyObjectByType<NetworkRunner>();
            }

            if (_runner != null && _runner.IsRunning)
                Time = (float)_runner.SimulationTime;
            else
                Time = UnityEngine.Time.time;
        }
    }
}