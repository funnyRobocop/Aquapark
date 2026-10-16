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
            Game    = 1,
            EndGame = 2,
            ShowResults = 3,
        }

        [Header("Start Points")]
        public StartingPointBehaviour[] startingPoints;

        [Header("Lobby Settings")]
        [SerializeField] private int   maxPlayersToAutoStart  = 8;
        [SerializeField] private int   minPlayersToStartTimer = 2;
        [SerializeField] private float lobbyWaitSeconds       = 30f;
        [SerializeField] private float readyCountdownSeconds  = 3.1f;

        [Header("Race Settings")]
        [SerializeField] private float raceDurationSeconds    = 300f;
        [SerializeField] private float afterFirstFinishSeconds = 60f;   // таймер после первого финиша
        [SerializeField] private float endGameDelay           = 0f;
        [SerializeField] private float resultsDelay           = 10f;

        [Header("UI: Level Info Feed")]
        public Transform  playerLevelInfoContent;              // VerticalLayoutGroup
        public GameObject playerLevelInfoPrefab;              // префаб плашки

        [Header("UI: HUD")]
        public TextMeshProUGUI gameStateText;
        public TextMeshProUGUI timerText;

        [Header("UI: Results")]
        public TextMeshProUGUI resultsText;
        public CanvasGroup     resultsCanvasGroup;

        // ─── Networked ───────────────────────────────────────────────────────

        [Networked, Capacity(8)]
        public NetworkArray<NetworkBehaviourId> playerList => default;

        [Networked, OnChangedRender(nameof(OnGameStateChanged))]
        public GameState gameState { get; set; }

        [Networked] public TickTimer     gameplayTimer     { get; set; }
        [Networked] public TickTimer     countdownTimer    { get; set; }
        [Networked] public TickTimer     lobbyTimer        { get; set; }
        [Networked] public NetworkBool   LobbyTimerRunning { get; set; }

        [Networked] public int           TotalPlayers      { get; set; }
        [Networked] public int           ReadyPlayers      { get; set; }
        [Networked] public NetworkBool   AllPlayersReady   { get; set; }
        [Networked] public int           FinishedCount     { get; set; }
        [Networked] public NetworkBool   FirstFinished     { get; set; } // сработал первый финиш

        // ─── Local (only this client) ─────────────────────────────────────────

        private int?  _prevTimerSec;
        private int   _prevTotalPlayers = -1;
        private int   _prevReadyPlayers = -1;
        private bool  _showResults;

        // Карточки готовности (id → GO) — удаляются при снятии Ready
        private readonly Dictionary<NetworkBehaviourId, GameObject> _readyCards = new();

        // Локальный трекер финишировавших (StateAuthority only).
        // player.HasFinished теперь ставится через RPC с задержкой — 
        // нельзя использовать его как guard от дубль на хосте.
        private readonly HashSet<NetworkBehaviourId> _finishedSet = new();

        //[Inject] private LoadingUI    _loadingUI;
        [SerializeField] private CameraManager _cameraManager;
        [Inject] private IPlayerSkinLoader   _skinLoader;   // для выгрузки addressables

        public bool ShowResults
        {
            get => _showResults;
            set
            {
                _showResults = value;
                if (resultsCanvasGroup != null)
                    resultsCanvasGroup.interactable = value;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        public override void Spawned()
        {
            Instance = this;
            Cursor.lockState = CursorLockMode.Locked;

            if (startingPoints == null || startingPoints.Length == 0)
                startingPoints = GetComponentsInChildren<StartingPointBehaviour>(true);

            OnGameStateChanged();

            if (Object.HasStateAuthority)
            {
                foreach (var player in Runner.GetAllBehaviours<PlayerRaceData>())
                {
                    if (player == null || player.StartingPointIndex >= 0) continue;

                    for (int i = 0; i < playerList.Length; i++)
                    {
                        if (Runner.TryFindBehaviour(playerList[i], out PlayerRaceData _)) continue;
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

        // ─────────────────────────────────────────────────────────────────────
        // FixedUpdateNetwork
        // ─────────────────────────────────────────────────────────────────────

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            switch (gameState)
            {
                case GameState.Waiting: UpdateWaitingState(); break;

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

        // ─────────────────────────────────────────────────────────────────────
        // Waiting
        // ─────────────────────────────────────────────────────────────────────

        private void UpdateWaitingState()
        {
            TotalPlayers = 0;
            ReadyPlayers = 0;

            for (int i = 0; i < playerList.Length; i++)
            {
                if (!Runner.TryFindBehaviour(playerList[i], out PlayerRaceData p)) continue;
                TotalPlayers++;
                if (p.IsReady) ReadyPlayers++;
            }

            if (TotalPlayers >= maxPlayersToAutoStart) { StartRace(); return; }

            if (TotalPlayers >= minPlayersToStartTimer && !LobbyTimerRunning)
            {
                lobbyTimer = TickTimer.CreateFromSeconds(Runner, lobbyWaitSeconds);
                LobbyTimerRunning = true;
            }

            if (LobbyTimerRunning && lobbyTimer.Expired(Runner) && TotalPlayers > 0)
            {
                StartRace();
                return;
            }

            if (TotalPlayers > 0 && TotalPlayers == ReadyPlayers)
            {
                if (!AllPlayersReady)
                {
                    AllPlayersReady = true;
                    countdownTimer = TickTimer.CreateFromSeconds(Runner, readyCountdownSeconds);
                }
                if (countdownTimer.Expired(Runner)) { StartRace(); return; }
            }
            else
            {
                AllPlayersReady = false;
                countdownTimer = default;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // StartRace
        // ─────────────────────────────────────────────────────────────────────
        
        private void StartRace()
        {
            gameState         = GameState.Game;
            gameplayTimer     = TickTimer.CreateFromSeconds(Runner, raceDurationSeconds);
            countdownTimer    = default;
            lobbyTimer        = default;
            LobbyTimerRunning = false;
            AllPlayersReady   = false;
            FinishedCount     = 0;
            FirstFinished     = false;
            _finishedSet.Clear();

            for (int i = 0; i < playerList.Length; i++)
            {
                if (!Runner.TryFindBehaviour(playerList[i], out PlayerRaceData p)) continue;
                p.RPC_ResetForNewRound();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // RPC: join
        // ─────────────────────────────────────────────────────────────────────

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_PlayerJoined(NetworkBehaviourId newPlayerID)
        {
            if (gameState != GameState.Waiting)
            {
                Debug.Log($"[InGameManager] Race in progress — late joiner {newPlayerID} is spectator.");
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

        // ─────────────────────────────────────────────────────────────────────
        // RPC: finish
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Вызывай этот метод из FinishZone — он сам разберётся с authority.
        /// Не важно, на каком клиенте вызван.
        /// </summary>
        public void RequestFinish(PlayerRaceData player)
        {
            if (player == null || player.HasFinished) return;

            if (Object.HasStateAuthority)
                RegisterFinish(player);
            else
                RPC_RequestFinish(player.Object.Id);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_RequestFinish(NetworkId playerObjectId)
        {
            if (!Runner.TryFindObject(playerObjectId, out var obj)) return;
            RegisterFinish(obj.GetBehaviour<PlayerRaceData>());
        }

        public void RegisterFinish(PlayerRaceData player)
        {
            if (!Object.HasStateAuthority)       return;
            if (gameState != GameState.Game)      return;
            if (player == null)                   return;
            if (!_finishedSet.Add(player.Id))     return; // дубль — уже зарегистрирован

            FinishedCount++;
            // Игрок сам себя помечает через RPC —
            // нельзя писать напрямую в чужой NetworkBehaviour в Shared Mode
            player.RPC_SetFinished(FinishedCount);

            // Первый финиш — перезапускаем таймер на 1 минуту
            if (!FirstFinished)
            {
                FirstFinished = true;
                gameplayTimer = TickTimer.CreateFromSeconds(Runner, afterFirstFinishSeconds);
            }

            // Все финишировали
            if (FinishedCount >= TotalPlayers && TotalPlayers > 0)
            {
                gameState     = GameState.EndGame;
                gameplayTimer = TickTimer.CreateFromSeconds(Runner, endGameDelay);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // UI: Level Info Feed  (вызывается из PlayerRaceData через OnChangedRender)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Обновляет плашку "игрок X is ready". Вызывается на всех клиентах.</summary>
        public void OnPlayerReadyChanged(NetworkBehaviourId id, string name, bool ready)
        {
            if (playerLevelInfoContent == null || playerLevelInfoPrefab == null) return;

            if (ready)
            {
                if (!_readyCards.ContainsKey(id))
                {
                    var go = Instantiate(playerLevelInfoPrefab, playerLevelInfoContent);
                    go.GetComponent<PlayerLevelInfo>()?.Show($"Player {name} is ready");
                    _readyCards[id] = go;
                }
            }
            else
            {
                if (_readyCards.TryGetValue(id, out var go))
                {
                    Destroy(go);
                    _readyCards.Remove(id);
                }
            }
        }

        /// <summary>Спавнить плашку "игрок X is finished". Вызывается на всех клиентах.</summary>
        public void OnPlayerFinishedUI(string name)
        {
            if (playerLevelInfoContent == null || playerLevelInfoPrefab == null) return;
            var go = Instantiate(playerLevelInfoPrefab, playerLevelInfoContent);
            go.GetComponent<PlayerLevelInfo>()?.Show($"Player {name} is finished");
        }

        /// <summary>После Keep Playing возвращает камеру на локального иг��ока.</summary>
        private void ReinitLocalPlayerCamera()
        {
            if (_cameraManager == null || Runner == null) return;

            var localObj = Runner.GetPlayerObject(Runner.LocalPlayer);
            var localPlayer = localObj?.GetBehaviour<PlayerRaceData>();
            if (localPlayer != null && localPlayer.CameraTarget != null)
                _cameraManager.InitForPlayer(localPlayer.CameraTarget);
        }

        private void ClearAllMessages()
        {
            foreach (var card in _readyCards.Values)
                if (card != null) Destroy(card);
            _readyCards.Clear();

            if (playerLevelInfoContent == null) return;
            foreach (Transform child in playerLevelInfoContent)
                Destroy(child.gameObject);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Camera / Spectator (вызывается из PlayerRaceData.OnHasFinishedChanged)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Локальный игрок финишировал — переключаем камеру на одного из активных.
        /// </summary>
        public void OnLocalPlayerFinished(PlayerRaceData finishedPlayer)
        {
            var targets = new List<PlayerRaceData>();
            foreach (var p in Runner.GetAllBehaviours<PlayerRaceData>())
            {
                if (p == finishedPlayer)           continue;
                if (p.StartingPointIndex < 0)      continue; // опоздавший спектатор
                if (p.HasFinished)                 continue; // уже финишировал
                targets.Add(p);
            }

            if (targets.Count == 0)
            {
                // Все финишировали — наб��юдаем за первым местом
                foreach (var p in Runner.GetAllBehaviours<PlayerRaceData>())
                    if (p.StartingPointIndex >= 0 && p != finishedPlayer)
                        targets.Add(p);
            }

            _cameraManager?.BeginSpectating(targets);
        }

        /// <summary>Опоздавший игрок входит в режим наблюдателя.</summary>
        public void BeginSpectatingForLateJoiner(PlayerRaceData spectator)
        {
            var targets = new List<PlayerRaceData>();
            foreach (var p in Runner.GetAllBehaviours<PlayerRaceData>())
            {
                if (p == spectator)           continue;
                if (p.StartingPointIndex < 0) continue;
                targets.Add(p);
            }
            _cameraManager?.BeginSpectating(targets);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Results buttons
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>UI: кнопка "Leave Game".</summary>
        public void LeaveGame()
        {
            //_loadingUI?.Show();
            if (CrazyGames.CrazySDK.IsInitialized)
                CrazyGames.CrazySDK.Game.HideInviteButton();

            // Выгрузка addressable скинов
            _skinLoader?.ReleaseAll();

            Runner.Shutdown();
        }

        /// <summary>UI: кнопка "Keep Playing" — сброс в Waiting без выхода из сессии.</summary>
        public void KeepPlaying()
        {
            RPC_RequestKeepPlaying();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_RequestKeepPlaying()
        {
            if (gameState != GameState.ShowResults) return;
            ResetToWaiting();
        }

        private void ResetToWaiting()
        {
            // Сбрасываем состояние игроков
            for (int i = 0; i < playerList.Length; i++)
            {
                if (!Runner.TryFindBehaviour(playerList[i], out PlayerRaceData p)) continue;
                // RPC_ResetForNewRound сразу сбрасывает HasFinished/FinishPlace/IsReady/IsVisible
                // И телепортирует на старт — один RPC вместо двух
                p.RPC_ResetForNewRound();
            }

            gameState         = GameState.Waiting;
            gameplayTimer     = default;
            countdownTimer    = default;
            lobbyTimer        = default;
            LobbyTimerRunning = false;
            AllPlayersReady   = false;
            FinishedCount     = 0;
            TotalPlayers      = 0;
            ReadyPlayers      = 0;
            FirstFinished     = false;
            _finishedSet.Clear();

            // Дорегистрировать опоздавших, которых не было в playerList
            RegisterPendingPlayers();
        }

        /// <summary>
        /// После Keep Playing: добавляет в playerList всех PlayerRaceData,
        /// которые подключились во время гонки и не были добавлены.
        /// </summary>
        private void RegisterPendingPlayers()
        {
            foreach (var player in Runner.GetAllBehaviours<PlayerRaceData>())
            {
                if (player == null) continue;

                // Уже в списке?
                bool inList = false;
                for (int i = 0; i < playerList.Length; i++)
                    if (playerList[i] == player.Id) { inList = true; break; }
                if (inList) continue;

                // Найти свободный слот и добавить
                for (int i = 0; i < playerList.Length; i++)
                {
                    if (Runner.TryFindBehaviour(playerList[i], out PlayerRaceData _)) continue;

                    playerList.Set(i, player.Id);
                    player.RPC_AssignStartingPoint(i); // телепорт + камера
                    break;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Render
        // ─────────────────────────────────────────────────────────────────────

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
            bool dirty = _prevReadyPlayers != ReadyPlayers || _prevTotalPlayers != TotalPlayers;
            if (dirty)
            {
                _prevReadyPlayers = ReadyPlayers;
                _prevTotalPlayers = TotalPlayers;

                if (gameStateText != null)
                {
                    if (TotalPlayers == 0)
                        gameStateText.text = $"Waiting for players... (max {maxPlayersToAutoStart})";
                    else if (ReadyPlayers < TotalPlayers)
                        gameStateText.text = $"Ready: {ReadyPlayers} / {TotalPlayers}  [Press Enter to ready up]";
                    else
                        gameStateText.text = "All players ready!";
                }
            }

            if (timerText == null) return;

            if (AllPlayersReady && !countdownTimer.ExpiredOrNotRunning(Runner))
            {
                int s = Mathf.CeilToInt(countdownTimer.RemainingTime(Runner) ?? 0f);
                if (_prevTimerSec != s) { _prevTimerSec = s; timerText.text = s > 0 ? s.ToString() : string.Empty; }
            }
            else if (LobbyTimerRunning && !lobbyTimer.ExpiredOrNotRunning(Runner))
            {
                int s = Mathf.CeilToInt(lobbyTimer.RemainingTime(Runner) ?? 0f);
                if (_prevTimerSec != s) { _prevTimerSec = s; timerText.text = $"Starting in {s}s"; }
            }
            else if (_prevTimerSec != null)
            {
                _prevTimerSec = null;
                timerText.text = string.Empty;
            }
        }

        private void RenderGameState()
        {
            if (timerText == null) return;
            int s = Mathf.CeilToInt(gameplayTimer.RemainingTime(Runner) ?? 0f);
            if (_prevTimerSec == s) return;
            _prevTimerSec = s;
            timerText.text = $"{s / 60:00}:{s % 60:00}";
        }

        // ─────────────────────────────────────────────────────────────────────
        // OnGameStateChanged (OnChangedRender)
        // ─────────────────────────────────────────────────────────────────────

        private void OnGameStateChanged()
        {
            _prevTimerSec     = null;
            _prevTotalPlayers = -1;
            _prevReadyPlayers = -1;

            switch (gameState)
            {
                case GameState.Game:
                    ClearAllMessages();                         // убираем все плашки Ready
                    _cameraManager?.StopSpectating();           // сброс спектатора

                    if (CrazyGames.CrazySDK.IsInitialized)
                    {
                        CrazyGames.CrazySDK.Game.GameplayStart();
                        CrazyGames.CrazySDK.Game.HideInviteButton();
                    }
                    if (gameStateText != null) gameStateText.text = string.Empty;
                    if (timerText     != null) timerText.text     = string.Empty;
                    ShowResults = false;
                    break;

                case GameState.Waiting:
                    ClearAllMessages();
                    _cameraManager?.StopSpectating();
                    ReinitLocalPlayerCamera();

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

        // ─────────────────────────────────────────────────────────────────────
        // Results
        // ─────────────────────────────────────────────────────────────────────

        private void BuildResults()
        {
            var finished    = new List<PlayerRaceData>();
            var notFinished = new List<PlayerRaceData>();

            foreach (var p in Runner.GetAllBehaviours<PlayerRaceData>())
            {
                if (p.HasFinished) finished.Add(p);
                else               notFinished.Add(p);
            }
            finished.Sort((a, b) => a.FinishPlace.CompareTo(b.FinishPlace));

            var sb = new System.Text.StringBuilder();
            foreach (var p in finished)    sb.AppendLine($"{p.FinishPlace}. {p.DisplayName}");
            foreach (var p in notFinished) sb.AppendLine($"— {p.DisplayName} (Not Finished)");

            if (resultsText != null) resultsText.text = sb.ToString();

            var localObj    = Runner.GetPlayerObject(Runner.LocalPlayer);
            var localPlayer = localObj?.GetBehaviour<PlayerRaceData>();
            bool isWinner   = localPlayer != null && localPlayer.HasFinished && localPlayer.FinishPlace == 1;

            if (isWinner && CrazyGames.CrazySDK.IsInitialized)
                CrazyGames.CrazySDK.Game.HappyTime();

            ShowResults = true;
            Cursor.lockState = CursorLockMode.None;
        }

        void OnValidate()
        {
            startingPoints = GetComponentsInChildren<StartingPointBehaviour>();
        }
    }
}
