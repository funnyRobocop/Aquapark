using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace NonameGame
{
    public class InGameManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
    {
        public static InGameManager Instance { get; private set; }

        public enum GameState
        {
            Waiting = 0,
            Game = 1,
            EndGame = 2,
            ShowResults = 3,
        }

        [Header("Start Points")]
        public StartingPointBehaviour[] startingPoints;

        [Networked, Capacity(8)]
        public NetworkArray<NetworkBehaviourId> playerList => default;

        [Networked, OnChangedRender(nameof(OnGameStateChanged))]
        public GameState gameState { get; set; }

        [Networked] public TickTimer gameplayTimer { get; set; }
        [Networked] public TickTimer countdownTimer { get; set; }

        [Networked] public int TotalPlayers { get; set; }
        [Networked] public int ReadyPlayers { get; set; }
        [Networked] public NetworkBool AllPlayersReady { get; set; }

        [Networked] public int FinishedCount { get; set; } // сколько уже финишировало

        [Header("Race Settings")]
        [SerializeField] private float raceDurationSeconds = 60f;
        [SerializeField] private float countdownSeconds = 3.1f;
        [SerializeField] private float endGameDelay = 3f;
        [SerializeField] private float resultsDelay = 8f;

        [Header("UI")]
        public TextMeshProUGUI gameStateText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI resultsText;
        public CanvasGroup resultsCanvasGroup;

        private int? previousTimerSeconds;
        private int previousTotalPlayers;
        private int previousReadyPlayers;
        private bool showResults;

        public bool ShowResults
        {
            get => showResults;
            set
            {
                showResults = value;
                if (resultsCanvasGroup != null)
                    resultsCanvasGroup.interactable = value;
            }
        }

        public override void Spawned()
        {
            Instance = this;
            Cursor.lockState = CursorLockMode.Locked;
            OnGameStateChanged();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            Instance = null;
            Cursor.lockState = CursorLockMode.None;
        }

        public void PlayerJoined(PlayerRef player)
        {
            if (CrazyGames.CrazySDK.IsInitialized &&
                Runner.SessionInfo.PlayerCount == Runner.SessionInfo.MaxPlayers)
            {
                CrazyGames.CrazySDK.Game.HideInviteButton();
            }
        }

        public void PlayerLeft(PlayerRef player)
        {
            if (CrazyGames.CrazySDK.IsInitialized && gameState != GameState.Game)
                CrazyManager.ShowInviteButton();
        }

        public override void FixedUpdateNetwork()
        {
            // Логику состояний ведёт только State Authority (Master Client)
            if (!Object.HasStateAuthority)
                return;

            switch (gameState)
            {
                case GameState.Waiting:
                    UpdateWaitingState();
                    break;

                case GameState.Game:
                    if (gameplayTimer.Expired(Runner))
                    {
                        gameState = GameState.EndGame;
                        gameplayTimer = TickTimer.CreateFromSeconds(Runner, endGameDelay);
                    }
                    break;

                case GameState.EndGame:
                    if (gameplayTimer.Expired(Runner))
                    {
                        gameState = GameState.ShowResults;
                        gameplayTimer = TickTimer.CreateFromSeconds(Runner, resultsDelay);
                    }
                    break;

                case GameState.ShowResults:
                    if (gameplayTimer.Expired(Runner))
                    {
                        // После результатов можно вернуть в Waiting или оставить экран
                        // gameState = GameState.Waiting;
                    }
                    break;
            }
        }

        private void UpdateWaitingState()
        {
            ReadyPlayers = 0;
            TotalPlayers = 0;

            for (int i = 0; i < playerList.Length; i++)
            {
                if (!Runner.TryFindBehaviour(playerList[i], out PlayerRaceData player))
                    continue;

                TotalPlayers++;
                if (player.OnStartPoint)
                    ReadyPlayers++;
            }

            if (TotalPlayers > 0 && TotalPlayers == ReadyPlayers)
            {
                if (!AllPlayersReady)
                {
                    AllPlayersReady = true;
                    countdownTimer = TickTimer.CreateFromSeconds(Runner, countdownSeconds);
                }

                if (countdownTimer.Expired(Runner))
                {
                    StartRace();
                }
            }
            else
            {
                AllPlayersReady = false;
                countdownTimer = default;
            }
        }

        private void StartRace()
        {
            gameState = GameState.Game;
            gameplayTimer = TickTimer.CreateFromSeconds(Runner, raceDurationSeconds);
            countdownTimer = default;
            AllPlayersReady = false;
            FinishedCount = 0;

            // Сброс финиша у всех
            for (int i = 0; i < playerList.Length; i++)
            {
                if (!Runner.TryFindBehaviour(playerList[i], out PlayerRaceData player))
                    continue;

                player.HasFinished = false;
                player.FinishPlace = 0;
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_RequestFinish(NetworkId playerObjectId)
        {
            if (!Runner.TryFindObject(playerObjectId, out var obj))
                return;

            var player = obj.GetBehaviour<PlayerRaceData>();
            RegisterFinish(player);
        }

        /// <summary>
        /// Вызывать из финишного триггера (только на State Authority игрока или через RPC).
        /// </summary>
        public void RegisterFinish(PlayerRaceData player)
        {
            if (!Object.HasStateAuthority)
                return;

            if (gameState != GameState.Game)
                return;

            if (player == null || player.HasFinished)
                return;

            FinishedCount++;
            player.HasFinished = true;
            player.FinishPlace = FinishedCount;

            // Если финишировали все — можно сразу заканчивать
            if (FinishedCount >= TotalPlayers && TotalPlayers > 0)
            {
                gameState = GameState.EndGame;
                gameplayTimer = TickTimer.CreateFromSeconds(Runner, endGameDelay);
            }
        }

        public override void Render()
        {
            switch (gameState)
            {
                case GameState.Waiting:
                    RenderWaitingState();
                    break;
                case GameState.Game:
                    RenderGameState();
                    break;
            }

            if (resultsCanvasGroup != null)
            {
                float target = ShowResults ? 1f : 0f;
                float speed = ShowResults ? Runner.DeltaTime : -Runner.DeltaTime * 2f;
                resultsCanvasGroup.alpha = Mathf.Clamp01(resultsCanvasGroup.alpha + speed);
            }

            Cursor.lockState = ShowResults ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void RenderWaitingState()
        {
            if (previousReadyPlayers != ReadyPlayers || previousTotalPlayers != TotalPlayers)
            {
                previousReadyPlayers = ReadyPlayers;
                previousTotalPlayers = TotalPlayers;

                if (gameStateText != null)
                {
                    if (ReadyPlayers != TotalPlayers)
                        gameStateText.text = $"Stand on the starting positions\nReady: {ReadyPlayers} / {TotalPlayers}";
                    else
                        gameStateText.text = "All players are ready!";
                }

                if (timerText != null)
                    timerText.text = string.Empty;
            }

            if (!AllPlayersReady || timerText == null)
                return;

            float? remaining = countdownTimer.RemainingTime(Runner);
            int seconds = remaining.HasValue ? Mathf.CeilToInt(remaining.Value) : 0;

            if (previousTimerSeconds != seconds)
            {
                previousTimerSeconds = seconds;
                timerText.text = seconds > 0 ? seconds.ToString() : "";
            }
        }

        private void RenderGameState()
        {
            if (timerText == null)
                return;

            float? remaining = gameplayTimer.RemainingTime(Runner);
            float t = remaining ?? 0f;
            int seconds = Mathf.CeilToInt(t);

            if (previousTimerSeconds != seconds)
            {
                previousTimerSeconds = seconds;
                int min = seconds / 60;
                int sec = seconds % 60;
                timerText.text = $"{min:00}:{sec:00}";
            }
        }

        private void OnGameStateChanged()
        {
            switch (gameState)
            {
                case GameState.Game:
                    if (CrazyGames.CrazySDK.IsInitialized)
                    {
                        CrazyGames.CrazySDK.Game.GameplayStart();
                        CrazyGames.CrazySDK.Game.HideInviteButton();
                    }
                    if (gameStateText != null)
                        gameStateText.text = string.Empty;
                    ShowResults = false;
                    break;

                case GameState.Waiting:
                    if (CrazyGames.CrazySDK.IsInitialized)
                    {
                        if (Runner.SessionInfo.PlayerCount < Runner.SessionInfo.MaxPlayers)
                            CrazyManager.ShowInviteButton();
                        CrazyGames.CrazySDK.Game.GameplayStop();
                    }
                    ShowResults = false;
                    break;

                case GameState.EndGame:
                    if (gameStateText != null)
                        gameStateText.text = "Finish!";
                    break;

                case GameState.ShowResults:
                    BuildResults();
                    break;
            }
        }

        private void BuildResults()
        {
            var finished = new List<PlayerRaceData>();
            var notFinished = new List<PlayerRaceData>();

            foreach (var player in Runner.GetAllBehaviours<PlayerRaceData>())
            {
                if (player.HasFinished)
                    finished.Add(player);
                else
                    notFinished.Add(player);
            }

            finished.Sort((a, b) => a.FinishPlace.CompareTo(b.FinishPlace));

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < finished.Count; i++)
                sb.AppendLine($"{finished[i].FinishPlace}. {finished[i].DisplayName}");

            for (int i = 0; i < notFinished.Count; i++)
                sb.AppendLine($"— {notFinished[i].DisplayName} (не финишировал)");

            if (resultsText != null)
                resultsText.text = sb.ToString();

            // Победа локального игрока (1 место)
            var localObj = Runner.GetPlayerObject(Runner.LocalPlayer);
            var localPlayer = localObj != null ? localObj.GetBehaviour<PlayerRaceData>() : null;

            bool isWinner = localPlayer != null && localPlayer.HasFinished && localPlayer.FinishPlace == 1;

            if (isWinner && CrazyGames.CrazySDK.IsInitialized)
                CrazyGames.CrazySDK.Game.HappyTime();

            ShowResults = true;
            Cursor.lockState = CursorLockMode.None;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_PlayerJoined(NetworkBehaviourId newPlayerID)
        {
            for (int i = 0; i < playerList.Length; i++)
            {
                if (Runner.TryFindBehaviour(playerList[i], out PlayerRaceData existing))
                    continue;

                playerList.Set(i, newPlayerID);

                if (Runner.TryFindBehaviour(newPlayerID, out PlayerRaceData player))
                {
                    player.RPC_AssignStartingPoint(i);
                }
                return;
            }
        }

        public void LeaveGame()
        {
            if (LoadingScreenBehaviour.Instance != null)
                LoadingScreenBehaviour.Instance.Show("Returning To Main Menu");

            if (CrazyGames.CrazySDK.IsInitialized)
                CrazyGames.CrazySDK.Game.HideInviteButton();

            Runner.Shutdown();
        }

        void OnValidate()
        {
            startingPoints = GetComponentsInChildren<StartingPointBehaviour>();
        }
    }
}
