using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VContainer;

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

        [Header("Lobby Settings")]
        [SerializeField] private int maxPlayersToAutoStart = 8;      // авто-старт при заполнении
        [SerializeField] private int minPlayersToStartTimer = 2;     // мин. игроков для запуска 30-сек таймера
        [SerializeField] private float lobbyWaitSeconds = 30f;       // макс. ожидание в лобби
        [SerializeField] private float readyCountdownSeconds = 3.1f; // обратный отсчёт когда все готовы

        [Header("Race Settings")]
        [SerializeField] private float raceDurationSeconds = 60f;
        [SerializeField] private float endGameDelay = 3f;
        [SerializeField] private float resultsDelay = 8f;

        [Header("UI")]
        public TextMeshProUGUI gameStateText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI resultsText;
        public CanvasGroup resultsCanvasGroup;

        // ─── Networked ───────────────────────────────────────────────

        [Networked, Capacity(8)]
        public NetworkArray<NetworkBehaviourId> playerList => default;

        [Networked, OnChangedRender(nameof(OnGameStateChanged))]
        public GameState gameState { get; set; }

        [Networked] public TickTimer gameplayTimer    { get; set; }
        [Networked] public TickTimer countdownTimer   { get; set; }  // когда все Ready
        [Networked] public TickTimer lobbyTimer       { get; set; }  // 30-сек ожидание
        [Networked] public NetworkBool LobbyTimerRunning { get; set; }

        [Networked] public int  TotalPlayers    { get; set; }
        [Networked] public int  ReadyPlayers    { get; set; }
        [Networked] public NetworkBool AllPlayersReady { get; set; }
        [Networked] public int  FinishedCount   { get; set; }

        // ─── Local ────────────────────────────────────────────────

        private int?  previousTimerSeconds;
        private int   previousTotalPlayers = -1;
        private int   previousReadyPlayers = -1;
        private bool  showResults;

        [Inject] private LoadingUI _loadingUI;

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

        // ───────────────────────────────────────────────────────
        // Lifecycle
        // ───────────────────────────────────────────────────────

        public override void Spawned()
        {
            Instance = this;
            Cursor.lockState = CursorLockMode.Locked;

            if (startingPoints == null || startingPoints.Length == 0)
                startingPoints = GetComponentsInChildren<StartingPointBehaviour>(true);

            OnGameStateChanged();

            // Назначаем точки игрокам, заспавнившимся до InGameManager
            if (Object.HasStateAuthority)
            {
                foreach (var player in Runner.GetAllBehaviours<PlayerRaceData>())
                {
                    if (player == null || player.StartingPointIndex >= 0)
                        continue;

                    for (int i = 0; i < playerList.Length; i++)
                    {
                        if (Runner.TryFindBehaviour(playerList[i], out PlayerRaceData _))
                            continue;

                        playerList.Set(i, player.Id);
                        player.RPC_AssignStartingPoint(i);
                        break;
                    }
                }
            }
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
                CrazyGames.CrazySDK.Game.HideInviteButton();
        }

        public void PlayerLeft(PlayerRef player)
        {
            if (CrazyGames.CrazySDK.IsInitialized && gameState != GameState.Game)
                CrazyManager.ShowInviteButton();
        }

        // ───────────────────────────────────────────────────────
        // FixedUpdateNetwork — только StateAuthority
        // ───────────────────────────────────────────────────────

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            switch (gameState)
            {
                case GameState.Waiting:   UpdateWaitingState(); break;
                case GameState.Game:
                    if (gameplayTimer.Expired(Runner))
                    {
                        gameState     = GameState.EndGame;
                        gameplayTimer = TickTimer.CreateFromSeconds(Runner, endGameDelay);
                    }
                    break;
                case GameState.EndGame:
                    if (gameplayTimer.Expired(Runner))
                    {
                        gameState     = GameState.ShowResults;
                        gameplayTimer = TickTimer.CreateFromSeconds(Runner, resultsDelay);
                    }
                    break;
            }
        }

        // ───────────────────────────────────────────────────────
        // Waiting logic
        // ───────────────────────────────────────────────────────

        private void UpdateWaitingState()
        {
            TotalPlayers = 0;
            ReadyPlayers = 0;

            for (int i = 0; i < playerList.Length; i++)
            {
                if (!Runner.TryFindBehaviour(playerList[i], out PlayerRaceData player)) continue;
                TotalPlayers++;
                if (player.IsReady) ReadyPlayers++;
            }

            // 1. Авто-старт при maxPlayers
            if (TotalPlayers >= maxPlayersToAutoStart)
            {
                StartRace();
                return;
            }

            // 2. Запускаем 30-сек таймер лобби (1 раз)
            if (TotalPlayers >= minPlayersToStartTimer && !LobbyTimerRunning)
            {
                lobbyTimer       = TickTimer.CreateFromSeconds(Runner, lobbyWaitSeconds);
                LobbyTimerRunning = true;
            }

            // 3. Принудительный старт по таймеру
            if (LobbyTimerRunning && lobbyTimer.Expired(Runner) && TotalPlayers > 0)
            {
                StartRace();
                return;
            }

            // 4. Ранний старт: все нажали Enter (IsReady)
            if (TotalPlayers > 0 && TotalPlayers == ReadyPlayers)
            {
                if (!AllPlayersReady)
                {
                    AllPlayersReady = true;
                    countdownTimer  = TickTimer.CreateFromSeconds(Runner, readyCountdownSeconds);
                }
                if (countdownTimer.Expired(Runner))
                {
                    StartRace();
                    return;
                }
            }
            else
            {
                AllPlayersReady = false;
                countdownTimer  = default;
            }
        }

        // ───────────────────────────────────────────────────────
        // StartRace
        // ───────────────────────────────────────────────────────

        private void StartRace()
        {
            gameState         = GameState.Game;
            gameplayTimer     = TickTimer.CreateFromSeconds(Runner, raceDurationSeconds);
            countdownTimer    = default;
            lobbyTimer        = default;
            LobbyTimerRunning = false;
            AllPlayersReady   = false;
            FinishedCount     = 0;

            for (int i = 0; i < playerList.Length; i++)
            {
                if (!Runner.TryFindBehaviour(playerList[i], out PlayerRaceData player)) continue;

                player.HasFinished = false;
                player.FinishPlace = 0;
                player.IsReady     = false;

                // Телепорт на стартовую позицию.
                // RPC_TeleportToStart → RpcTargets.StateAuthority (сам игрок) —
                // так что в Shared Mode каждый сам делает телепорт своей физики.
                player.RPC_TeleportToStart();
            }
        }

        // ───────────────────────────────────────────────────────
        // RPC: регистрация / финиш
        // ───────────────────────────────────────────────────────

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_PlayerJoined(NetworkBehaviourId newPlayerID)
        {
            if (gameState != GameState.Waiting)
            {
                Debug.Log($"[InGameManager] Race started, ignoring late joiner {newPlayerID}");
                return;
            }

            for (int i = 0; i < playerList.Length; i++)
            {
                if (Runner.TryFindBehaviour(playerList[i], out PlayerRaceData _)) continue;

                playerList.Set(i, newPlayerID);
                if (Runner.TryFindBehaviour(newPlayerID, out PlayerRaceData player))
                    player.RPC_AssignStartingPoint(i);
                return;
            }

            Debug.LogWarning("[InGameManager] No free slot for new player.");
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_RequestFinish(NetworkId playerObjectId)
        {
            if (!Runner.TryFindObject(playerObjectId, out var obj)) return;
            RegisterFinish(obj.GetBehaviour<PlayerRaceData>());
        }

        public void RegisterFinish(PlayerRaceData player)
        {
            if (!Object.HasStateAuthority) return;
            if (gameState != GameState.Game)  return;
            if (player == null || player.HasFinished) return;

            FinishedCount++;
            player.HasFinished = true;
            player.FinishPlace = FinishedCount;

            if (FinishedCount >= TotalPlayers && TotalPlayers > 0)
            {
                gameState     = GameState.EndGame;
                gameplayTimer = TickTimer.CreateFromSeconds(Runner, endGameDelay);
            }
        }

        // ───────────────────────────────────────────────────────
        // Render
        // ───────────────────────────────────────────────────────

        public override void Render()
        {
            switch (gameState)
            {
                case GameState.Waiting: RenderWaitingState(); break;
                case GameState.Game:    RenderGameState();    break;
            }

            if (resultsCanvasGroup != null)
            {
                float speed = ShowResults ? Runner.DeltaTime : -Runner.DeltaTime * 2f;
                resultsCanvasGroup.alpha = Mathf.Clamp01(resultsCanvasGroup.alpha + speed);
            }

            Cursor.lockState = ShowResults ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void RenderWaitingState()
        {
            bool dirty = previousReadyPlayers != ReadyPlayers ||
                         previousTotalPlayers != TotalPlayers;

            if (dirty)
            {
                previousReadyPlayers = ReadyPlayers;
                previousTotalPlayers = TotalPlayers;

                if (gameStateText != null)
                {
                    if (TotalPlayers == 0)
                        gameStateText.text = $"Waiting for players... (max {maxPlayersToAutoStart})";
                    else if (ReadyPlayers < TotalPlayers)
                        gameStateText.text = $"Ready: {ReadyPlayers} / {TotalPlayers}  [← Press Enter to ready up]";
                    else
                        gameStateText.text = "All players ready!";
                }
            }

            if (timerText == null) return;

            // Обратный отсчёт (все нажали Enter)
            if (AllPlayersReady && !countdownTimer.ExpiredOrNotRunning(Runner))
            {
                int s = Mathf.CeilToInt(countdownTimer.RemainingTime(Runner) ?? 0f);
                if (previousTimerSeconds != s)
                {
                    previousTimerSeconds = s;
                    timerText.text = s > 0 ? s.ToString() : string.Empty;
                }
            }
            // 30-сек таймер лобби
            else if (LobbyTimerRunning && !lobbyTimer.ExpiredOrNotRunning(Runner))
            {
                int s = Mathf.CeilToInt(lobbyTimer.RemainingTime(Runner) ?? 0f);
                if (previousTimerSeconds != s)
                {
                    previousTimerSeconds = s;
                    timerText.text = $"Starting in {s}s";
                }
            }
            else if (previousTimerSeconds != null)
            {
                previousTimerSeconds = null;
                timerText.text = string.Empty;
            }
        }

        private void RenderGameState()
        {
            if (timerText == null) return;

            int seconds = Mathf.CeilToInt(gameplayTimer.RemainingTime(Runner) ?? 0f);
            if (previousTimerSeconds == seconds) return;

            previousTimerSeconds = seconds;
            timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }

        // ───────────────────────────────────────────────────────
        // OnGameStateChanged
        // ───────────────────────────────────────────────────────

        private void OnGameStateChanged()
        {
            previousTimerSeconds = null;
            previousTotalPlayers = -1;
            previousReadyPlayers = -1;

            switch (gameState)
            {
                case GameState.Game:
                    if (CrazyGames.CrazySDK.IsInitialized)
                    {
                        CrazyGames.CrazySDK.Game.GameplayStart();
                        CrazyGames.CrazySDK.Game.HideInviteButton();
                    }
                    if (gameStateText != null) gameStateText.text = string.Empty;
                    if (timerText    != null) timerText.text    = string.Empty;
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
                    if (gameStateText != null) gameStateText.text = "Finish!";
                    break;

                case GameState.ShowResults:
                    BuildResults();
                    break;
            }
        }

        // ───────────────────────────────────────────────────────
        // Results
        // ───────────────────────────────────────────────────────

        private void BuildResults()
        {
            var finished    = new List<PlayerRaceData>();
            var notFinished = new List<PlayerRaceData>();

            foreach (var player in Runner.GetAllBehaviours<PlayerRaceData>())
            {
                if (player.HasFinished) finished.Add(player);
                else                    notFinished.Add(player);
            }

            finished.Sort((a, b) => a.FinishPlace.CompareTo(b.FinishPlace));

            var sb = new System.Text.StringBuilder();
            foreach (var p in finished)
                sb.AppendLine($"{p.FinishPlace}. {p.DisplayName}");
            foreach (var p in notFinished)
                sb.AppendLine($"— {p.DisplayName} (Not Finished)");

            if (resultsText != null)
                resultsText.text = sb.ToString();

            var localObj    = Runner.GetPlayerObject(Runner.LocalPlayer);
            var localPlayer = localObj?.GetBehaviour<PlayerRaceData>();
            bool isWinner   = localPlayer != null && localPlayer.HasFinished && localPlayer.FinishPlace == 1;

            if (isWinner && CrazyGames.CrazySDK.IsInitialized)
                CrazyGames.CrazySDK.Game.HappyTime();

            ShowResults = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // ───────────────────────────────────────────────────────
        // Misc
        // ───────────────────────────────────────────────────────

        public void LeaveGame()
        {
            _loadingUI?.Show();
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
