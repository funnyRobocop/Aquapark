using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using VContainer;

namespace NonameGame
{
    public class InGameManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
    {
        public static InGameManager Instance { get; private set; }

        /// <summary>
        /// Используйте в скрипте движения: если true — ввод движения заблокирован (отсчёт 3-2-1).
        /// Вращать камеру мышью по-прежнему разрешено.
        /// </summary>
        //public static bool IsMovementLocked => Instance != null && Instance.MovementLocked;

        public enum GameState
        {
            Waiting = 0,
            Game    = 1,
            EndGame = 2,
            ShowResults = 3,
        }

        [Header("Start Points and trigger and camera")]
        [SerializeField] public StartingPointBehaviour[] startingPoints;
        [SerializeField] private CheckpointTrigger[] checkpointTriggers;
        [SerializeField] private CameraManager _cameraManager;
        [SerializeField] private GameObject _airObjects; 
        
        [Header("Throwable Items")]
        [SerializeField] private ThrowableItem[] _throwableItems;

        [Header("Lobby Settings")]
        [SerializeField] private int   maxPlayersToAutoStart  = 8;
        [SerializeField] private int   minPlayersToStartTimer = 2;
        [SerializeField] private float lobbyWaitSeconds       = 30f;
        [SerializeField] private float readyCountdownSeconds  = 3.1f;

        [Header("Race Settings")]
        [SerializeField] private float raceDurationSeconds    = 300f;
        [SerializeField] private float afterFirstFinishSeconds = 60f;   // таймер после первого финиша
        [SerializeField] private float matchForcedTimerAfter   = 120f;  // через сколько секунд без финиша включить таймер
        [SerializeField] private float matchForcedTimerSeconds = 60f;   // длина принудительного таймера
        [SerializeField] private float endGameDelay           = 0f;
        [SerializeField] private float resultsDelay           = 10f;

        [Header("UI: Level Info Feed")]
        public Transform  playerLevelInfoContent;              // VerticalLayoutGroup
        public GameObject playerLevelInfoPrefab;              // префаб плашки

        [Header("UI: HUD")]
        public LevelUI levelUI;
        public HoldEscToExit escPanel;
        public TextMeshProUGUI gameStateText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI keepPlayingTimerText;

        [Header("UI: Results")]
        public GameObject resultsPanel;
        public GameObject finishMessage;
        public Transform resultsList;
        public GameObject resultsListItemPrefab;

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
        [Networked] public NetworkBool   ForcedCountdown   { get; set; } // принудительный таймер по истечении matchForcedTimerAfter
        //[Networked] public NetworkBool   MovementLocked    { get; set; } // заблок движения во время отсчёта 3-2-1
        [Networked] public TickTimer     keepPlayingTimer  { get; set; } // авто-KeepPlaying на экране результатов

        // Результаты гонки: индекс = слот в playerList, значение = место (1-based, 0 = не финишировал).
        // Устанавливается напрямую на StateAuthority InGameManager-а — без двойного RPC-хопа.
        [Networked, Capacity(8)]
        public NetworkArray<int> SlotFinishPlace => default;

        // ─── Local (only this client) ─────────────────────────────────────────

        private int?  _prevTimerSec;
        private int   _prevTotalPlayers = -1;
        private int   _prevReadyPlayers = -1;
        private bool _showResults;

        private string waitingText;
        private string readyToPlayText;
        private string allReadyText;
        private string startText;
        private string finishText;
        private string keepPlayingText;
        private string notFinishedText;
        private string playerReadyText;
        private string playerFinishText;

        // Карточки готовности (id → GO) — удаляются при снятии Ready
        private readonly Dictionary<NetworkBehaviourId, GameObject> _readyCards = new();

        // Локальный трекер финишировавших (StateAuthority only).
        // player.HasFinished теперь ставится через RPC с задержкой — 
        // нельзя использовать его как guard от дубль на хосте.
        private readonly HashSet<NetworkBehaviourId> _finishedSet = new();

        [Inject] private GameModel _gameModel;
        [Inject] private SoundManager _soundManager;

        public bool ShowResults
        {
            get => _showResults;
            set
            {
                _showResults = value;
                resultsPanel.SetActive(value);
            }
        }

        private void UpdateLocalizedStrings()
        {
            waitingText = LocalizationSettings.StringDatabase.GetLocalizedString("MyStrings", "waiting");
            readyToPlayText = LocalizationSettings.StringDatabase.GetLocalizedString("MyStrings", "readyToPlay");
            allReadyText = LocalizationSettings.StringDatabase.GetLocalizedString("MyStrings", "allReady");
            startText = LocalizationSettings.StringDatabase.GetLocalizedString("MyStrings", "start");
            finishText = LocalizationSettings.StringDatabase.GetLocalizedString("MyStrings", "finish");
            keepPlayingText = LocalizationSettings.StringDatabase.GetLocalizedString("MyStrings", "keepPlaying");
            notFinishedText = LocalizationSettings.StringDatabase.GetLocalizedString("MyStrings", "notFinished");
            playerReadyText = LocalizationSettings.StringDatabase.GetLocalizedString("MyStrings", "playerReady");
            playerFinishText = LocalizationSettings.StringDatabase.GetLocalizedString("MyStrings", "playerFinish");
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

            //Настройки качества
            if (_gameModel.Settings.Quality != (int)QualityType.High)
            {
                Destroy(_airObjects);
            }

            if (_gameModel.Settings.Quality == (int)QualityType.Low)
            {
                QualitySettings.lodBias = 0.35f;
                //QualitySettings.maximumLODLevel = 2;
            }
            else
            {
                QualitySettings.lodBias = 0.8f;
            }

            UpdateLocalizedStrings();
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

            // Если мы не StateAuthority но гонка идёт и все игроки ушли — запрашиваем authority
            if (!Object.HasStateAuthority)
            {
                Debug.Log($"[InGameManager] PlayerLeft fired on non-authority, gameState={gameState}, requesting authority if needed.");
                if (gameState == GameState.Game && TotalPlayers > 0 && AllRegisteredPlayersGone())
                {
                    Debug.Log("[InGameManager] All players gone, requesting StateAuthority.");
                    Object.RequestStateAuthority();
                }
                return;
            }

            // Удаляем ушедшего из playerList и добавляем в _finishedSet,
            // чтобы он не считался в TotalPlayers и FinishedCount
            for (int i = 0; i < playerList.Length; i++)
            {
                if (!Runner.TryFindBehaviour(playerList[i], out PlayerRaceData p)) continue;
                if (p.Object.InputAuthority != player)                            continue;

                // Считаем ушедшего финишировавшим, чтобы не ждать его
                _finishedSet.Add(p.Id);
                SlotFinishPlace.Set(i, -1); // слот освобождён (игрок вышел)
                break;
            }

            // Если идёт гонка — проверяем, не закончилась ли она
            if (gameState == GameState.Game) CheckRaceOver();
        }

        /// <summary>
        /// Считает активных (невышедших) игроков во время гонки.
        /// Если все оставшиеся финишировали / вышли — заканчивает гонку.
        /// </summary>
        /// <summary>
        /// Возвращает true если в сессии есть подключённые игроки,
        /// которые НЕ зарегистрированы в playerList (наблюдатели).
        /// </summary>
        /// <summary>
        /// Каждый тик во время гонки проверяет, не отключился ли кто из playerList.
        /// Надёжнее чем PlayerLeft, который может не сработать если уходящий — StateAuthority.
        /// </summary>
        private void CheckDisconnectedPlayers()
        {
            bool anyNew = false;
            for (int i = 0; i < playerList.Length; i++)
            {
                if (!Runner.TryFindBehaviour(playerList[i], out PlayerRaceData p)) continue;
                if (_finishedSet.Contains(p.Id)) continue; // уже учтён

                PlayerRef auth = p.Object.InputAuthority;

                // Проверка 1: InputAuthority стал None — игрок ушёл
                bool gone = !auth.IsValid;

                // Проверка 2: если authority ещё валиден — проверим по ActivePlayers
                if (!gone)
                {
                    gone = true;
                    foreach (var ap in Runner.ActivePlayers)
                        if (ap == auth) { gone = false; break; }
                }

                if (gone)
                {
                    Debug.Log($"[InGameManager] CheckDisconnectedPlayers: player {auth} gone, marking left.");
                    _finishedSet.Add(p.Id);
                    SlotFinishPlace.Set(i, -1);
                    anyNew = true;
                }
            }

            // Доп. проверка: если объект в playerList вообще не находится (despawned) — тоже ушёл
            // Этот случай обрабатывается в CheckRaceOver (который тоже скипает ненайденные)

            if (anyNew) CheckRaceOver();

            // На случай если объекты despawned и аctive=0 — вызываем CheckRaceOver в любом случае
            if (!anyNew) CheckRaceOver();
        }

        /// <summary>
        /// true — если ни один игрок из playerList больше не подключён.
        /// Используется спектатором чтобы понять, надо ли захватить StateAuthority.
        /// </summary>
        private bool AllRegisteredPlayersGone()
        {
            for (int i = 0; i < playerList.Length; i++)
            {
                if (!Runner.TryFindBehaviour(playerList[i], out PlayerRaceData p)) continue;
                foreach (var ap in Runner.ActivePlayers)
                    if (ap == p.Object.InputAuthority) return false; // хоть один ещё онлайн
            }
            return true;
        }

        private bool HasSpectators()
        {
            foreach (var pr in Runner.ActivePlayers)
            {
                bool found = false;
                for (int i = 0; i < playerList.Length; i++)
                {
                    if (!Runner.TryFindBehaviour(playerList[i], out PlayerRaceData p)) continue;
                    if (p.Object.InputAuthority == pr) { found = true; break; }
                }
                if (!found) return true; // есть хотя бы один наблюдатель
            }
            return false;
        }

        private void CheckRaceOver()
        {
            int active = 0;
            for (int i = 0; i < playerList.Length; i++)
            {
                if (!Runner.TryFindBehaviour(playerList[i], out PlayerRaceData p)) continue;
                if (!_finishedSet.Contains(p.Id)) active++;
            }
            if (active == 0 && TotalPlayers > 0)
            {
                // Если есть наблюдатели — сбрасываем в ожидание
                if (HasSpectators())
                {
                    ResetToWaiting();
                    return;
                }

                gameState     = GameState.EndGame;
                gameplayTimer = TickTimer.CreateFromSeconds(Runner, endGameDelay);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // FixedUpdateNetwork
        // ───────────────────────────────────────────────────���─────────────────

        // Диагностика: логи каждые 3 секунды
        private float _diagTimer;

        public override void FixedUpdateNetwork()
        {
            // === ДИАГНОСТИКА: лог каждые 3 сек пока Game ===
            if (gameState == GameState.Game)
            {
                _diagTimer -= Runner.DeltaTime;
                if (_diagTimer <= 0f)
                {
                    _diagTimer = 3f;
                    int activePlayers = 0;
                    foreach (var _ in Runner.ActivePlayers) activePlayers++;
                    Debug.Log($"[DIAG] HasAuth={Object.HasStateAuthority} TotalPlayers={TotalPlayers} ActivePlayers={activePlayers} state={gameState}");
                    for (int di = 0; di < playerList.Length; di++)
                    {
                        bool found = Runner.TryFindBehaviour(playerList[di], out PlayerRaceData dp);
                        if (!found && playerList[di] == default) continue;
                        Debug.Log($"[DIAG]   slot[{di}] found={found} " + (found ? $"auth={dp.Object.InputAuthority} authValid={dp.Object.InputAuthority.IsValid}" : ""));
                    }
                }
            }
            // ============================================================

            if (!Object.HasStateAuthority)
            {
                // Спектатор: если гонка идёт и все зарегистрированные игроки ушли — забираем StateAuthority
                if (gameState == GameState.Game && TotalPlayers > 0 && AllRegisteredPlayersGone())
                    Object.RequestStateAuthority();
                return;
            }

            switch (gameState)
            {
                case GameState.Waiting: UpdateWaitingState(); break;

                case GameState.Game:
                    // Проверяем отключившихся игроков (надёжнее чем PlayerLeft)
                    CheckDisconnectedPlayers();
                    // Принудительный т��ймер: если никто не финишировал за matchForcedTimerAfter секунд
                    if (!FirstFinished && !ForcedCountdown)
                    {
                        float elapsed = raceDurationSeconds - (gameplayTimer.RemainingTime(Runner) ?? raceDurationSeconds);
                        if (elapsed >= matchForcedTimerAfter)
                        {
                            ForcedCountdown = true;
                            gameplayTimer   = TickTimer.CreateFromSeconds(Runner, matchForcedTimerSeconds);
                        }
                    }
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

                case GameState.ShowResults:
                    // Авто Keep Playing по истечении 10 секунд
                    if (keepPlayingTimer.Expired(Runner))
                        ResetToWaiting();
                    break;
            }
        }

        // ───────────��─────────────────────────────────────────────────────────
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
                lobbyTimer        = TickTimer.CreateFromSeconds(Runner, lobbyWaitSeconds);
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
                    AllPlayersReady  = true;
                    //MovementLocked   = true; // заблок движения на время отсчёта
                    countdownTimer   = TickTimer.CreateFromSeconds(Runner, readyCountdownSeconds);
                }
                if (countdownTimer.Expired(Runner)) { StartRace(); return; }
            }
            else
            {
                // Если не все готовы — сбрасываем отсчёт и раз��локируем движение
                if (AllPlayersReady) { AllPlayersReady = false; /*MovementLocked = false;*/ countdownTimer = default; }
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
            //MovementLocked    = false; // разблокировать с стартом гонки
            FinishedCount     = 0;
            FirstFinished     = false;
            ForcedCountdown   = false;
            keepPlayingTimer  = default;
            _finishedSet.Clear();
            for (int i = 0; i < SlotFinishPlace.Length; i++) SlotFinishPlace.Set(i, 0);

            for (int i = 0; i < playerList.Length; i++)
            {
                if (!Runner.TryFindBehaviour(playerList[i], out PlayerRaceData p)) continue;
                p.RPC_ResetForNewRound();
            }

            foreach (var item in checkpointTriggers)
                item.Clear();
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

            // Записываем место прямо на InGameManager (StateAuthority) — реплицируется без
            // двойного RPC-хопа. BuildResults читает отсюда, а не из HasFinished игрока.
            for (int i = 0; i < playerList.Length; i++)
            {
                if (playerList[i] == player.Id)
                {
                    SlotFinishPlace.Set(i, FinishedCount);
                    break;
                }
            }

            // Игрок сам себя помечает через RPC (для IsVisible / камеры).
            player.RPC_SetFinished(FinishedCount);

            // Первый финиш — перезапускаем таймер на 1 минуту
            if (!FirstFinished)
            {
                FirstFinished = true;
                gameplayTimer = TickTimer.CreateFromSeconds(Runner, afterFirstFinishSeconds);
            }

            // Проверяем завершение гонки через CheckRaceOver —
            // учитывает и финишировавших, и вышедших из сессии игроков.
            CheckRaceOver();
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
                    go.GetComponent<PlayerLevelInfo>()?.ShowReady(string.Format(playerReadyText, name));
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
        public void OnPlayerFinishedUI(string name, bool isFirst)
        {
            if (playerLevelInfoContent == null || playerLevelInfoPrefab == null) return;
            var go = Instantiate(playerLevelInfoPrefab, playerLevelInfoContent);
            go.GetComponent<PlayerLevelInfo>()?.ShowFinish(string.Format(playerFinishText, name), isFirst);
        }

        /// <summary>После Keep Playing возвращает камеру на локального.</summary>
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
            {
                if (child.GetComponent<PlayerLevelInfo>() != null)
                {
                    Destroy(child.gameObject);
                }
            }
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
            /*var _loadingUI = FindAnyObjectByType<LoadingUI>();
            _loadingUI?.Show();*/
            if (CrazyGames.CrazySDK.IsInitialized)
                CrazyGames.CrazySDK.Game.HideInviteButton();

            var localData = PlayerRaceData.LocalInstance;
            if (localData != null)
            {
                _gameModel.SessionKickCount = localData.KickCount;
                _gameModel.SessionThrowCount = localData.ThrowCount;
                _gameModel.SessionGoldCount = localData.GoldCount;
                _gameModel.SessionFinishCount = localData.FinishCount;
                Debug.Log($"[InGameManager] Session totals: Kick={_gameModel.SessionKickCount}, Throw={_gameModel.SessionThrowCount}, Gold={_gameModel.SessionGoldCount}, Finish={_gameModel.SessionFinishCount}");
            }

            _soundManager?.PlayMusic();
            var scene = SceneManager.GetSceneByName("Level1");
            if (scene.isLoaded)
                SceneManager.UnloadSceneAsync(scene);
                
            // Выгрузка Addressable-скинов всех игроков на этом клиенте
            foreach (var view in Runner.GetAllBehaviours<PlayerView>())
                view.ReleaseSkin();

            // Уведомляем StateAuthority до отключения, чтобы он мог сбросить состояние
            // (PlayerLeft может не вызваться если уходящий сам является StateAuthority)
            RPC_NotifyPlayerLeaving(Runner.LocalPlayer);

            Runner.Shutdown();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_NotifyPlayerLeaving(PlayerRef leavingPlayer)
        {
            if (!Object.HasStateAuthority) return;
            if (gameState != GameState.Game) return;

            // Ищем PlayerRaceData уходящего игрока по переданному PlayerRef
            var sender = leavingPlayer;
            for (int i = 0; i < playerList.Length; i++)
            {
                if (!Runner.TryFindBehaviour(playerList[i], out PlayerRaceData p)) continue;
                if (p.Object.InputAuthority != sender) continue;

                _finishedSet.Add(p.Id);
                SlotFinishPlace.Set(i, -1);
                break;
            }

            // Если остались только наблюдатели — сбрасываем в ожидание
            int active = 0;
            for (int i = 0; i < playerList.Length; i++)
            {
                if (!Runner.TryFindBehaviour(playerList[i], out PlayerRaceData p)) continue;
                if (!_finishedSet.Contains(p.Id)) active++;
            }
            if (active == 0)
                ResetToWaiting();
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

            gameState = GameState.Waiting;
            gameplayTimer = default;
            countdownTimer = default;
            lobbyTimer = default;
            LobbyTimerRunning = false;
            AllPlayersReady = false;
            FinishedCount = 0;
            TotalPlayers = 0;
            ReadyPlayers = 0;
            FirstFinished   = false;
            ForcedCountdown = false;
            //MovementLocked    = false;
            keepPlayingTimer = default;
            _finishedSet.Clear();
            for (int i = 0; i < SlotFinishPlace.Length; i++) SlotFinishPlace.Set(i, 0);

            ResetThrowableItems();
            // Дорегистрировать опоздавших, которых не было в playerList
            RegisterPendingPlayers();
        }

        private void ResetThrowableItems()
        {
            if (_throwableItems == null) return;
            foreach (var item in _throwableItems)
            {
                if (item == null) continue;
                if (item.Object == null || !item.Object.IsValid) continue; // объект уже уничтожен

                if (item.Object.HasStateAuthority)
                    item.ResetToInitial();
                else if (Runner != null && Runner.IsRunning)
                    item.RPC_ResetToInitial(); // RPC только если раннер ещё жив
            }
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
                case GameState.Game: RenderGameState(); break;
                case GameState.ShowResults: RenderShowResultsState(); break;
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
                    gameStateText.transform.parent.gameObject.SetActive(true);
                    if (TotalPlayers == 0)
                        gameStateText.text = string.Format(waitingText, maxPlayersToAutoStart);
                    else if (ReadyPlayers < TotalPlayers)
                        gameStateText.text = string.Format(readyToPlayText, ReadyPlayers, TotalPlayers);
                    else
                        gameStateText.text = allReadyText;
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
                if (_prevTimerSec != s) { _prevTimerSec = s; timerText.text = string.Format(startText, s); }
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

            // таймер виден после первого финиша ИЛИ когда включился принудительный таймер
            if (!FirstFinished && !ForcedCountdown)
            {
                if (_prevTimerSec != null) { _prevTimerSec = null; timerText.text = string.Empty; }
                return;
            }

            int s = Mathf.CeilToInt(gameplayTimer.RemainingTime(Runner) ?? 0f);
            if (_prevTimerSec == s) return;
            _prevTimerSec = s;
            timerText.text = $"{s / 60:00}:{s % 60:00}";
        }

        // обновление таймера на экране результатов каждый кадр
        private void RenderShowResultsState()
        {
            if (keepPlayingTimerText == null) return;
            if (keepPlayingTimer.ExpiredOrNotRunning(Runner))
            {
                keepPlayingTimerText.text = string.Empty;
                return;
            }
            float rem = keepPlayingTimer.RemainingTime(Runner) ?? 0f;
            keepPlayingTimerText.text = rem > 0f
                ? string.Format(keepPlayingText, Mathf.CeilToInt(rem))
                : string.Empty;
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
                    ClearAllMessages();
                    _cameraManager?.StopSpectating();
                    _soundManager.PlayMusic();
                    if (CrazyGames.CrazySDK.IsInitialized)
                    {
                        CrazyGames.CrazySDK.Game.GameplayStart();
                        CrazyGames.CrazySDK.Game.HideInviteButton();
                    }
                    if (gameStateText != null)
                    {
                        gameStateText.transform.parent.gameObject.SetActive(false);
                        gameStateText.text = string.Empty; 
                    }
                    if (timerText     != null) timerText.text     = string.Empty;
                    ShowResults = false;
                    break;

                case GameState.Waiting:
                    ClearAllMessages();
                    _cameraManager?.StopSpectating();
                    ReinitLocalPlayerCamera();
                    _soundManager.StopMusic();
                    escPanel?.Hide();

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
                    {
                        gameStateText.transform.parent.gameObject.SetActive(true);
                        gameStateText.text = finishText;
                    }
                    break;

                case GameState.ShowResults:
                    BuildResults();
                    escPanel?.Show();
                    ClearAllMessages();
                    if (timerText != null) timerText.text = string.Empty;  // убираем застывший таймер
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Results
        // ─────────────────────────────────────────────────────────────────────

        private void BuildResults()
        {
            // SlotFinishPlace устанавливается напрямую на StateAuthority InGameManager-а —
            // без двойного RPC-хопа, поэтому гарантированно актуально на всех клиентах.
            var finished    = new List<(PlayerRaceData player, int place)>();
            var notFinished = new List<PlayerRaceData>();

            for (int i = 0; i < playerList.Length; i++)
            {
                if (!Runner.TryFindBehaviour(playerList[i], out PlayerRaceData p)) continue;
                int place = SlotFinishPlace[i];
                if (place > 0)
                {
                    finished.Add((p, place));
                    // Гарантируем выключение рендерера: RPC_SetFinished мог не успеть за endGameDelay
                    p.ForceHide();
                }
                else
                {
                    notFinished.Add(p);
                }
            }
            finished.Sort((a, b) => a.place.CompareTo(b.place));

            var playerCount = finished.Count + notFinished.Count;

            finishMessage.SetActive(playerCount < 2);

            foreach (Transform child in resultsList)
            {
                Destroy(child.gameObject);
            }
            foreach (var (p, place) in finished)
            {
                var go = Instantiate(resultsListItemPrefab, resultsList);
                go.GetComponent<LevelResultsItem>()?.Load(p, place, playerCount);
            }
            foreach (var p in notFinished)
            {
                var go = Instantiate(resultsListItemPrefab, resultsList);
                go.GetComponent<LevelResultsItem>()?.Load(p, 0, playerCount);
            }            

            var localObj    = Runner.GetPlayerObject(Runner.LocalPlayer);
            var localPlayer = localObj?.GetBehaviour<PlayerRaceData>();

            // Первое место локального игрока — через SlotFinishPlace
            int localSlot     = -1;
            int localPlace    = 0;
            for (int i = 0; i < playerList.Length; i++)
            {
                if (localPlayer != null && playerList[i] == localPlayer.Id)
                    { localSlot = i; localPlace = SlotFinishPlace[i]; break; }
            }
            bool isWinner = localPlace == 1;

            if (isWinner && CrazyGames.CrazySDK.IsInitialized)
                CrazyGames.CrazySDK.Game.HappyTime();

            // Авто-KeepPlaying через 10 секунд (только на StateAuthority)
            if (Object.HasStateAuthority)
                keepPlayingTimer = TickTimer.CreateFromSeconds(Runner, 10f);

            ShowResults = true;
            Cursor.lockState = CursorLockMode.None;
        }

        void OnValidate()
        {
            startingPoints = GetComponentsInChildren<StartingPointBehaviour>();
        }
    }
}
