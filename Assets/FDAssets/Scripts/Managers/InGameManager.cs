using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Tooltip("Handles the main game state and updates, controlled by the Shared Mode Master Client.")]
public class InGameManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    /// <summary>
    /// Static reference to the InGameManager.
    /// </summary>
    public static InGameManager Instance { get; private set; }

    [Networked, Capacity(8), Tooltip("The Network Behaviour references for the players in their starting position order.")]
    public NetworkArray<NetworkBehaviourId> playerList => default;

    [Tooltip("Reference to the Starting Position that handle where players respawn and sets their colors.")]
    public StartingPointBehaviour[] startingPoints;

    [Networked(), OnChangedRender(nameof(OnGameStateChanged))]
    [Tooltip("The state of the game.")]
    public GameState gameState { get; set; }

    /// <summary>
    /// The timer for the gameplay.
    /// </summary>
    [Networked()]
    public TickTimer gameplayTimer { get; set; }

    /// <summary>
    /// The timer before gameplay begins.
    /// </summary>
    [Networked()]
    public TickTimer countdownTimer { get; set; }

    // The previously displayed seconds.
    int? previousTimerSeconds;

    [Networked(), Tooltip("The total number of players in the game.")]
    public int TotalPlayers { get; set; }
    [Networked(), Tooltip("The number of players who are near their starting point and ready to player.")]
    public int ReadyPlayers { get; set; }

    /// <summary>
    /// The previous total and ready players values
    /// </summary>
    int previousTotalPlayers, previousReadyPlayers;

    [Networked(), Tooltip("Set to true if all players are ready.")]
    public NetworkBool AllPlayersReady { get; set; }

    [Header("UI Elements")]
    [Tooltip("Text that changes based on the state of the game.")]
    public TextMeshProUGUI gameStateText;

    [Tooltip("Text that displays in game timers")]
    public TextMeshProUGUI timerText;

    [Tooltip("Text that displays the results list.")]
    public TextMeshProUGUI resultsText;

    [Tooltip("Canvas that displays the end results of a game.")]
    public CanvasGroup resultsCanvasGroup;

    [Header("Obstacle Course References")]
    [Tooltip("The array of Obstacle Course sets.")]
    public ObstacleCourseBehaviour[] obstacleCourses;

    /// <summary>
    /// A local property that determines if the game results should be shown; set to false after viewing the results and closing the windows.
    /// </summary>
    public bool ShowResults
    {
        get => showResults;
        set
        {
            showResults = value;

            // Disables the result screen buttons if the results should not be shown.
            resultsCanvasGroup.interactable = value;
        }
    }

    private bool showResults;
    public enum GameState
    {
        // Waiting for the game to begin
        Waiting = 0,

        // Currently in gameplay
        Game = 1,

        // A slight end before the game ends to account.
        EndGame = 2,

        // The results of the game being shown.
        ShowResults = 3,
    }

    #region FUSION METHODS AND CALLBACKS
    public override void Spawned()
    {
        // Locks the cursor to the screen for easier camera movement.
        Cursor.lockState = CursorLockMode.Locked;

        // Sets the static instance since there should only be one.
        Instance = this;

        OnGameStateChanged();

        base.Spawned();

    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Instance = null;
        Cursor.lockState = CursorLockMode.None;
        AudioManager.SetGameState(GameState.Waiting);
        base.Despawned(runner, hasState);
    }

    public void PlayerJoined(PlayerRef player)
    {
        // If the maximum number of players has joined, we hide the invite button.
        if (CrazyGames.CrazySDK.IsInitialized && Runner.SessionInfo.PlayerCount == Runner.SessionInfo.MaxPlayers)
        {
            CrazyGames.CrazySDK.Game.HideInviteButton();
        }
    }
    public void PlayerLeft(PlayerRef player)
    {
        // When a player leaves, we can assume the maximum number of players has not been reached, so we can show the invite button again, but only if we are not in active gameplay.
        if (CrazyGames.CrazySDK.IsInitialized && gameState != GameState.Game)
        {
            CrazyManager.ShowInviteButton();
        }
    }

    /// <summary>
    /// Update called only by the State Authority of the InGameManager; in this case, the Shared Mode Master Client.
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        switch (gameState)
        {
            case GameState.Game:
                if (gameplayTimer.Expired(Runner))
                {
                    gameState = GameState.EndGame;
                    gameplayTimer = TickTimer.CreateFromSeconds(Runner, 5f);

                    // Calls end game on all of the courses.
                    foreach (var course in obstacleCourses)
                    {
                        course.EndGame();
                    }
                }
                break;
            case GameState.EndGame:
                if (gameplayTimer.Expired(Runner))
                {
                    gameState = GameState.ShowResults;
                    gameplayTimer = TickTimer.CreateFromSeconds(Runner, 5f);
                }
                break;
            case GameState.ShowResults:
                if (gameplayTimer.Expired(Runner))
                {
                    gameState = GameState.Waiting;
                }
                break;
            case GameState.Waiting:
                OnUpdateWaitingState();
                break;
        }
    }

    private void OnUpdateWaitingState()
    {
        ReadyPlayers = 0;
        TotalPlayers = 0;
        for (int i = 0; i < playerList.Length; i++)
        {
            // If a player is not available or leaves, this will return false.
            if (!Runner.TryFindBehaviour(playerList[i], out PlayerNetworkBehaviour player))
                continue;

            TotalPlayers++;
            if (player.OnStartPoint)
                ReadyPlayers++;
        }

        // If all players are ready...
        if (TotalPlayers == ReadyPlayers)
        {
            // If the players were previously not ready, the countdown timer is started.
            if (!AllPlayersReady)
            {
                AllPlayersReady = true;
                countdownTimer = TickTimer.CreateFromSeconds(Runner, 3.1f);
            }

            // If the countdown timer expires, the game begins.
            if (countdownTimer.Expired(Runner))
            {
                gameState = GameState.Game;
                gameplayTimer = TickTimer.CreateFromSeconds(Runner, 120);
                foreach (var course in obstacleCourses)
                {
                    course.StartGame();
                }
                countdownTimer = default;
                AllPlayersReady = false;
                TotalPlayers = 0;
                ReadyPlayers = 0;
            }
        }
        else
        {
            AllPlayersReady = false;
        }
    }

    /// <summary>
    /// Updates renders that all players set.  No NetworkProperties that require State Authority are set here.
    /// </summary>
    public override void Render()
    {
        switch (gameState)
        {
            case GameState.Game:
                RenderGameState();
                break;
            case GameState.Waiting:
                RenderWaitingState();
                break;
        }

        // Fades in or out the result screen 
        float resultsAlpha = Mathf.Clamp01(resultsCanvasGroup.alpha + (ShowResults ? Runner.DeltaTime : -Runner.DeltaTime * 2f));
        if (resultsAlpha != resultsCanvasGroup.alpha)
            resultsCanvasGroup.alpha = resultsAlpha;

        // Sets the lock state of the camera if the results should be shown or not.
        Cursor.lockState = ShowResults ? CursorLockMode.None : CursorLockMode.Locked;
    }

    /// <summary>
    /// Updates rendering UI during the waiting State.
    /// </summary>
    private void RenderWaitingState()
    {
        // If the value of ready players and total players has changed from the previously set value, the text is not updated every frame.
        if (previousReadyPlayers != ReadyPlayers || previousTotalPlayers != TotalPlayers)
        {
            previousReadyPlayers = ReadyPlayers;
            previousTotalPlayers = TotalPlayers;
            if (ReadyPlayers != TotalPlayers)
                gameStateText.text = $"Stand On Your Starting Position To Start\nPlayers Ready:  {ReadyPlayers} / {TotalPlayers}";
            else
                gameStateText.text = "All Players Ready";

            timerText.text = string.Empty;
        }

        if (!AllPlayersReady)
            return;

        // Sets the waiting time
        float? remainingWaitTime = countdownTimer.RemainingTime(Runner);
        System.TimeSpan waitTimeSpawn = System.TimeSpan.FromSeconds(0f);
        if (remainingWaitTime.HasValue)
        {
            waitTimeSpawn = System.TimeSpan.FromSeconds(remainingWaitTime.Value);
        }

        if (previousTimerSeconds != waitTimeSpawn.Seconds)
        {
            timerText.text = waitTimeSpawn.Seconds.ToString();
            previousTimerSeconds = waitTimeSpawn.Seconds;
        }
    }

    /// <summary>
    /// Updates rendering UI during the Game State.
    /// </summary>
    private void RenderGameState()
    {
        float? remainingTime = gameplayTimer.RemainingTime(Runner);
        System.TimeSpan ts = System.TimeSpan.FromSeconds(0f);
        if (remainingTime.HasValue)
        {
            ts = System.TimeSpan.FromSeconds(remainingTime.Value);
        }

        if (previousTimerSeconds != ts.Seconds)
        {
            timerText.text = ts.ToString("mm\\:ss");
            previousTimerSeconds = ts.Seconds;
        }
    }


    #endregion

    /// <summary>
    /// Method called when a change to gameState is detected.
    /// </summary>
    void OnGameStateChanged()
    {
        AudioManager.SetGameState(gameState);
        switch (gameState)
        {
            case GameState.Game:
                
                // The invite button is hidden when starting the game.
                if (CrazyGames.CrazySDK.IsInitialized)
                {
                    CrazyGames.CrazySDK.Game.GameplayStart();
                    CrazyGames.CrazySDK.Game.HideInviteButton();
                }

                gameStateText.text = string.Empty;

                SetLocalPlayerArrowAndScore(false, true);
                break;
            case GameState.Waiting:
                // Stops gameplay if we are in the waiting state.
                if (CrazyGames.CrazySDK.IsInitialized)
                {
                    // If we have not reached the maximum number of players, so it's okay to show the invite button.
                    if (Runner.SessionInfo.PlayerCount < Runner.SessionInfo.MaxPlayers)
                        CrazyManager.ShowInviteButton();

                    CrazyGames.CrazySDK.Game.GameplayStop();
                }

                SetLocalPlayerArrowAndScore(true, false);

                break;
            case GameState.EndGame:
                gameStateText.text = "Time's Up!";
                break;
            case GameState.ShowResults:
                OnGameStateShowResults();

                break;
        }
    }

    /// <summary>
    /// Gets the local player and sets the state of their starting point arrow and score.
    /// </summary>
    /// <param name="forceShow"></param>
    /// <param name="resetScore"></param>
    private void SetLocalPlayerArrowAndScore(bool forceShow, bool resetScore)
    {
        var localPlayer = Runner.GetPlayerObject(Runner.LocalPlayer);
        var player = localPlayer?.GetBehaviour<PlayerNetworkBehaviour>();

        // Resets the player score.
        if (player != null)
        {
            if (resetScore)
                player.Score = 0;

            if (player.StartingPointIndex >= 0)
                startingPoints[player.StartingPointIndex].SetArrowState(forceShow || player.HeldFlag.IsValid);
        }
    }

    /// <summary>
    /// When the game state changes to show results
    /// </summary>
    private void OnGameStateShowResults()
    {
        // Gets the score for all players and sorts it from high to low.
        List<PlayerNetworkBehaviour> players = new List<PlayerNetworkBehaviour>(Runner.GetAllBehaviours<PlayerNetworkBehaviour>());
        players.Sort((x, y) => y.Score.CompareTo(x.Score));

        Cursor.lockState = CursorLockMode.None;

        // Determine the winners
        int highScore = 0;
        string resultText = "";
        PlayerNetworkBehaviour lp = null;

        for (int i = 0; i < players.Count; i++)
        {
            highScore = Mathf.Max(players[i].Score, highScore);
            resultText += $"{players[i].PlayerDisplayName}:  {players[i].Score}\n";

            if (players[i].Object.StateAuthority == Runner.LocalPlayer)
                lp = players[i];
        }

        // Determines if the local player was a winner.
        lp.IsWinner = lp.Score == highScore;
        AudioManager.PlayerResultSFX(lp.IsWinner);

        if (lp.IsWinner && CrazyGames.CrazySDK.IsInitialized)
            CrazyGames.CrazySDK.Game.HappyTime();

        resultsText.text = resultText;

        ShowResults = true;
    }

    #region RPCs

    /// <summary>
    /// RPC called when a player joins, providing the NetworkBehaviourId for the recently joined player
    /// </summary>
    /// <param name="newPlayerID"></param>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_PlayerJoined(NetworkBehaviourId newPlayerID)
    {
        for (int i = 0; i < playerList.Length; i++)
        {
            // This means there is already a player at this slot, so we continue
            if (Runner.TryFindBehaviour(playerList[i], out PlayerNetworkBehaviour existingPlayer))
                continue;

            // Sets the player array for this index to this player id.
            playerList.Set(i, newPlayerID);

            // Sends an RPC back to the player to indicate that this is their starting point index.
            if (Runner.TryFindBehaviour(newPlayerID, out PlayerNetworkBehaviour player))
            {
                player.RPC_AssignStartingPointIndex(i);
                return;
            }
        }
    }
    #endregion

    /// <summary>
    /// Method called by the results screen's Leave game button.
    /// </summary>
    public void LeaveGame()
    {
        LoadingScreenBehaviour.Instance.Show("Returning To Main Menu");
        CrazyGames.CrazySDK.Game.HideInviteButton();
        Runner.Shutdown();
    }

    void OnValidate()
    {
        startingPoints = GetComponentsInChildren<StartingPointBehaviour>();
    }
}