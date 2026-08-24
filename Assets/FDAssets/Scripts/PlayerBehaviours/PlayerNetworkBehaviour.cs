using CrazyGames;
using Fusion;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[Tooltip("The Main Player NetworkBehaviour.")]
public class PlayerNetworkBehaviour : NetworkBehaviour, IPlayerLeft
{
    [Header("Player Information")]
    [Networked, OnChangedRender(nameof(OnPlayerNameSet))]
    [Tooltip("The name for the player that will be displayed near their starting position.")]
    public NetworkString<_128> PlayerName { get; set; }
    public string PlayerDisplayName => (PlayerName.Length == 0) ? $"Player {StartingPointIndex + 1}" : PlayerName.ToString();

    [Networked, OnChangedRender(nameof(OnStartingPointChanged))]
    [Tooltip("Reference to which starting point belongs to the player.  Determines player color as well.")]
    public int StartingPointIndex { get; set; } = -1;

    [Networked]
    [Tooltip("Is the player currently in range of their starting point.")]
    public NetworkBool OnStartPoint { get; set; }

    /// <summary>
    /// The starting point for the character
    /// </summary>
    Vector3? startingPointPosition;

    [SerializeField, Tooltip("The distance factor that determines if the player is near their starting point")]
    float startingPointRange = 1f;

    [Networked, OnChangedRender(nameof(OnFlagUpdate))]
    [Tooltip("Reference to the NetworkBehaviourID of the flag currently behind held.")]
    public NetworkBehaviourId HeldFlag { get; set; }

    [SerializeField(), Tooltip("The Flag GameObject that is toggled when a playing is holding a flag.")]
    GameObject flagGameObject;

    [Networked, OnChangedRender(nameof(OnMasterClientChanged))]
    public NetworkBool IsMasterClient { get; set; }

    [SerializeField(), Tooltip("References to the star mesh renderers which are activate if the player is the Shared Mode MasterClient.")]
    Renderer[] masterClientStarRenderers;

    [Networked, OnChangedRender(nameof(OnScoreChanged))]
    [Tooltip("The player's score, increased when they received a point.")]
    public int Score { get; set; }

    [Networked, OnChangedRender(nameof(OnWinnerChanged))]
    [Tooltip("Did this player win the previous round?")]
    public NetworkBool IsWinner { get; set; }

    [SerializeField, Tooltip("Reference to the Crown GameObject that is activated if the player wins.")]
    GameObject crown;

    [SerializeField, Tooltip("Reference to the ground shadow projector, which toggles if the player explodes.")]
    DecalProjector projector;

    [Header("Movement Parameters")]
    [SerializeField, Tooltip("Reference to the NetworkCharacterController.")]
    NetworkCharacterController ncc;

    [SerializeField, Tooltip("Reference to the character's Animator.")]
    Animator animator;

    const string JUMP_ANIM_PARAMETER = "Jumped";
    const string SPRINT_ANIM_PARAMETER = "Sprint";

    [SerializeField, Tooltip("The run speed of the character.")]
    private float runSpeed;

    [SerializeField, Tooltip("The speed of the character when they are airborne.")]
    private float airborneSpeed;

    [Networked, OnChangedRender(nameof(OnJumpChanged))]
    [Tooltip("Sets of the player has jumped; determines if the jump animation should be played.")]
    public NetworkBool Jumped { get; set; }

    /// <summary>
    /// A short timer that will allow players to jump while airborne.
    /// </summary>
    float coyotoTime = 0f;

    [SerializeField, Tooltip("The maximum amount of time before being able to jump while falling will not work.")]
    float coyoteTimeLimit = 1f;

    [SerializeField, Tooltip("The Y position at which the player will explode if they fall below it.")]
    float yMinimum = -3f;

    #region CAMERA PARAMETERS
    [Header("Camera Information")]
    [SerializeField, Tooltip("Reference to the Camera.")]
    Camera cam;

    [SerializeField, Tooltip("Reference to the Camera's Transform.")]
    Transform camTransform;

    [SerializeField, Tooltip("The local starting position of the camera.")]
    Vector3 camLocalPosition;

    [SerializeField, Tooltip("The offset vector of the camera from the player.")]
    Vector3 camOffset;

    [SerializeField, Tooltip("The distance of the camera from the player.")]
    float camDistance;

    [SerializeField, Tooltip("The minimum y position the camera will move.")]
    float camMinYPosition = 0.5f;

    [SerializeField, Tooltip("Multiplayer that affects the rate of camera rotation.")]
    float camRotateRate = 10;
    #endregion

    #region EXPLOSION PARAMETERS
    [Header("Explosion Parameters")]
    [Networked, OnChangedRender(nameof(OnExplodeChange)), Tooltip("If true, the character will explode.")]
    public NetworkBool Explode { get; set; }

    /// <summary>
    /// The TickTimer used to determine when a character should become active after exploding.
    /// </summary>
    [Networked]
    public TickTimer ExplosionCooldownTimer { get; set; }

    [SerializeField, Tooltip("The amount of time ExplosionCooldownTimer will be set to in seconds after a player explodes.")]
    float explosionCooldownLength;

    [SerializeField, Tooltip("Reference to the manager that affects explosions.")]
    ExplodableSetBehaviour explodableManager;
    #endregion

    [Header("SFX Audio Sources")]
    public AudioSource jumpAS;
    public AudioSource explodeAS;
    public AudioSource pickUpFlagAS;
    public AudioSource scoreAS;

    /// <summary>
    /// Colliders with this tag will cause the player to explode.
    /// </summary>
    const string TAG_KILL = "CanExplode";

    #region FUSION METHODS AND CALLBACKS
    public override void Spawned()
    {
        // Sets up camera information.  The camera is only updated for the player with State Authority, which is the local player.
        cam.enabled = HasStateAuthority;
        camTransform = cam.transform;
        camLocalPosition = camTransform.localPosition;
        camOffset = camLocalPosition.normalized;
        camDistance = camLocalPosition.magnitude;

        // The camera is no longer parents to the player to prevent it from automatically rotating when the player does.
        cam.transform.SetParent(null, true);

        // Sets the NetworkRunner player object based on the state authority.
        Runner.SetPlayerObject(Object.StateAuthority, Object);

        // If the player has state authority...
        if (HasStateAuthority)
        {
            // Calls the game manager rpc that they have joined.
            InGameManager.Instance.RPC_PlayerJoined(Id);

            // Sets whether or not this player is the shared mode master client.
            IsMasterClient = Runner.IsSharedModeMasterClient;

            // Gets and assigns the username.
            CrazySDK.User.GetUser(user =>
            {
                if (user != null)
                {
                    PlayerName = user.username;
                }
                else
                {
                    PlayerName = string.Empty;
                }
            });
        }

        // Executes various OnChangedRender methods on spawn so this information renders properly.
        OnExplodeChange();
        OnFlagUpdate();
        OnStartingPointChanged();
        OnMasterClientChanged();
        OnPlayerNameSet();
        OnWinnerChanged();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // If the camera has not already been destroyed, it is destroyed since it's unparented on spawn.
        if (cam != null)
            Destroy(cam.gameObject);

        // Tries to find and remove the player name from the game if one is active.
        var s = InGameManager.Instance;
        if (s == null)
        {
            s = FindAnyObjectByType<InGameManager>();
            if (s == null)
                return;
        }

        // If the player was despawned before a starting point was assigned, we return.
        if (StartingPointIndex < 0)
        {
            return;
        }

        s.startingPoints[StartingPointIndex].SetPlayerName(false, string.Empty);

        base.Despawned(runner, hasState);
    }


    /// <summary>
    /// Only the local player, who has State Authority, will call this method.
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        // If the player has recently exploded...
        if (Explode)
        {
            UpdateExplosion();
            return;
        }

        // Determines the movement vector and camera rotation based on player input.
        Vector3 moveValue = Vector3.zero;
        moveValue.x = PlayerInputBehaviour.moveValue.x;
        moveValue.z = PlayerInputBehaviour.moveValue.y;

        moveValue = cam.transform.rotation * moveValue;
        moveValue.y = 0f;

        // The movement of the character, which changes if the Network Character Controller is grounded or not.
        if (ncc.Grounded)
        {
            coyotoTime = 0f;
            animator.SetBool(SPRINT_ANIM_PARAMETER, moveValue.sqrMagnitude > 0f);
            ncc.Move(moveValue * runSpeed);
            Jumped = false;

            animator.SetBool(JUMP_ANIM_PARAMETER, false);
        }
        else
        {
            ncc.Move(moveValue * airborneSpeed);
            animator.SetBool(JUMP_ANIM_PARAMETER, true);
            coyotoTime += Runner.DeltaTime;
        }

        if (PlayerInputBehaviour.jumpValue > 0 && !Jumped && coyotoTime <= coyoteTimeLimit)
        {
            Jumped = true;

            Vector3 v = ncc.Velocity;
            v.y = ncc.jumpImpulse;
            ncc.Velocity = v;

            animator.SetBool(JUMP_ANIM_PARAMETER, true);
        }

        if (!Explode)
        {
            // If the player falls below the yMinimum or presses K (an easter egg), they will explode
            if (Input.GetKey(KeyCode.K) || transform.position.y < yMinimum)
            {
                ExplodePlayer();
            }
        }

        // Determines if the player is in range of their starting point by doing a simple distance check.
        if (startingPointPosition.HasValue)
        {
            float distance = Vector3.Distance(transform.position, startingPointPosition.Value);
            OnStartPoint = distance <= startingPointRange && !InGameManager.Instance.ShowResults;

            // If the player is in range and holding a flag, they release the flag and send an RPC to the flag's State Authority that they have deposited it.
            if (OnStartPoint && HeldFlag.IsValid)
            {
                Score++;
                if (Runner.TryFindBehaviour<FlagNetworkBehaviour>(HeldFlag, out var heldFlagBehaviour))
                {
                    heldFlagBehaviour.RPC_ReturnFlag(FlagNetworkBehaviour.CollectedState.Deposited);
                }
                HeldFlag = default(NetworkBehaviourId);
            }
        }
    }

    /// <summary>
    /// Executed if the player has exploded.
    /// </summary>
    private void UpdateExplosion()
    {
        // If the player has a flag, they return and remove it from themselves.
        if (Runner.TryFindBehaviour(HeldFlag, out FlagNetworkBehaviour flag))
        {
            flag.RPC_ReturnFlag(FlagNetworkBehaviour.CollectedState.NotCollected);
            HeldFlag = default(NetworkBehaviourId);
        }

        // If the cooldown timer expires, they are no longer 
        if (ExplosionCooldownTimer.Expired(Runner))
        {
            Explode = false;
        }
        else
        {
            float? remaining = ExplosionCooldownTimer.RemainingTime(Runner);

            // We teleport back to the starting point just before the player has exploded to prevent errors in which the player can explode multiple times if near an obstacle.
            if (remaining.HasValue && remaining.Value < 0.1f)
            {
                if (InGameManager.Instance == null || StartingPointIndex < 0)
                    ncc.Teleport(Vector3.up);
                else
                    ncc.Teleport(InGameManager.Instance.startingPoints[StartingPointIndex].transform.position + Vector3.up);
            }
        }
    }

    /// <summary>
    /// When the player leaves, any player may become the new Master Client, so a quick check is done for this.
    /// </summary>
    /// <param name="player"></param>
    public void PlayerLeft(PlayerRef player)
    {
        if (HasStateAuthority)
            IsMasterClient = Runner.IsSharedModeMasterClient;
    }
#endregion

    #region ONCHANGEDRENDER METHODS
    private void OnExplodeChange()
    {
        if (Explode)
        {
            explodableManager.Explode();
            projector.enabled = false;
            explodeAS.Play();
        }
        else
        {
            explodableManager.Assemble();
            projector.enabled = true;
        }
    }

    void OnJumpChanged()
    {
        if (Jumped)
            jumpAS.Play();
    }

    private void OnFlagUpdate()
    {
        flagGameObject.SetActive(HeldFlag.IsValid);
        if (HeldFlag.IsValid)
        {
            pickUpFlagAS.Play();
        }

        // Toggles the flag while in game mode.
        if (Runner.LocalPlayer == Object.StateAuthority)
        {
            if (InGameManager.Instance != null && StartingPointIndex >= 0)
            {
                bool showFlag = HeldFlag.IsValid && InGameManager.Instance.gameState == InGameManager.GameState.Game;
                InGameManager.Instance.startingPoints[StartingPointIndex].SetArrowState(showFlag);
            }
        }
    }

    void OnPlayerNameSet()
    {
        var s = InGameManager.Instance;
        if (s == null)
        {
            s = FindAnyObjectByType<InGameManager>();
            if (s == null)
                return;
        }

        if (StartingPointIndex < 0)
        {
            return;
        }

        s.startingPoints[StartingPointIndex].SetPlayerName(Runner.LocalPlayer == Object.StateAuthority, PlayerName);
    }

    void OnWinnerChanged()
    {
        crown.SetActive(IsWinner);
    }

    void OnStartingPointChanged()
    {
        var s = InGameManager.Instance;
        if (s == null)
        {
            s = FindAnyObjectByType<InGameManager>();
            if (s == null)
                return;
        }

        if (StartingPointIndex < 0)
        {
            startingPointPosition = null;
            return;
        }

        startingPointPosition = s.startingPoints[StartingPointIndex].transform.position;

        explodableManager.AssignMaterials(s.startingPoints[StartingPointIndex].materials);

        OnPlayerNameSet();
    }

    void OnScoreChanged()
    {
        if (InGameManager.Instance == null || StartingPointIndex < 0)
            return;

        InGameManager.Instance.startingPoints[StartingPointIndex].UpdateScore(Score);
        if (Score > 0)
            scoreAS.Play();
    }

    void OnMasterClientChanged()
    {
        foreach (var r in masterClientStarRenderers)
            r.enabled = IsMasterClient;
    }
    #endregion

    #region RPCs

    /// <summary>
    /// An RPC from the State Authority of the flag sent to the player to confirm the flag was collected by them.
    /// </summary>
    /// <param name="Flag"></param>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_CollectFlag(NetworkBehaviourId Flag)
    {
        HeldFlag = Flag;
    }

    /// <summary>
    /// An RPC sent from the InGameManager to assign the starting point.
    /// </summary>
    /// <param name="index"></param>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_AssignStartingPointIndex(int index)
    {
        StartingPointIndex = index;
    }
    #endregion

    #region UNITY METHODS
    void OnTriggerEnter(Collider other)
    {
        // Only the state authority to the player can cause them to explode
        if (!HasStateAuthority)
            return;

        // If the object has the TAG_KILL, the player will explode if they haven't already.
        if (other.CompareTag(TAG_KILL))
        {
            if (!Explode)
            {
                ExplodePlayer();
            }
        }
    }

    private void LateUpdate()
    {
        if (!cam.enabled)
            return;

        Vector3 camPos = ncc.transform.position + Quaternion.Euler(0, cam.transform.eulerAngles.y, 0) * camOffset * camDistance;
        camPos.y = Mathf.Max(camPos.y, camMinYPosition);
        camTransform.position = camPos;
        cam.transform.RotateAround(ncc.transform.position, Vector3.up, PlayerInputBehaviour.lookValue.x * Time.deltaTime * camRotateRate);
    }
    #endregion
    private void ExplodePlayer()
    {
        Explode = true;
        ExplosionCooldownTimer = TickTimer.CreateFromSeconds(Runner, explosionCooldownLength);
    }
}
