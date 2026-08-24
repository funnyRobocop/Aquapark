using Fusion;
using UnityEngine;

[Tooltip("NetworkBehaviour that tracks the state of the flags on obstacle courses.")]
public class FlagNetworkBehaviour : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnCollectionChanged))]
    [Tooltip("The Networked Collected state of the flag.")]
    public CollectedState Collected { get; set; }

    /// <summary>
    /// A local state of the flag is used to make it appear to be collected quickly when in local gameplay.
    /// </summary>
    CollectedState? localCollectedState;

    /// <summary>
    /// A timer tracked per client that, when it elapses, will check to see if it differs from Collected.
    /// </summary>
    float localCollectedTimer;

    /// <summary>
    /// After localCollectedTimer exceeds this value, the flag will check its Networked Collected state and update if it differs.
    /// </summary>
    float localCollectedTimerLimit = 2f;

    [SerializeField(), Tooltip("The GameObject toggles depending on the Collected state of the flag")]
    GameObject flagGameObject;

    public enum CollectedState : byte
    {
        NotCollected = 0,
        Held = 1,
        Deposited = 2,
        Resetting = 3,
        Dropped = 4,
    }

    public override void Spawned()
    {
        base.Spawned();
        OnCollectionChanged();
    }

    void OnCollectionChanged()
    {
        // If the object has not been collected, the object is displayed.
        flagGameObject.SetActive(Collected == CollectedState.NotCollected);
        localCollectedState = null;
    }

    /// <summary>
    /// An RPC that can be sent from any client to the state authority to determine the state of the flag.
    /// </summary>
    /// <param name="collectState"></param>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_ReturnFlag(CollectedState collectState)
    {
        Collected = collectState;
    }

    /// <summary>
    /// This is tracked per client, so it is not FixedUpdateNetwork
    /// </summary>
    private void Update()
    {
        // If the local collection state has a value
        if (localCollectedState.HasValue)
        {
            localCollectedTimer += Time.deltaTime;
            if (localCollectedTimer >= localCollectedTimerLimit)
            {
                if (localCollectedState.Value != Collected)
                {
                    OnCollectionChanged();
                }
                localCollectedState = null;
            }
        }
    }

    /// <summary>
    /// If a player collides with the flag on the StateAuthority, it will send an RPC to that player that they collected the flag.
    /// If the player is not the State Authority, it will register a "local" collection state to make it appear that the flag was picked up.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out PlayerNetworkBehaviour player))
            return;

        if (player.HeldFlag.IsValid)
            return;

        if (HasStateAuthority)
        {
            if (Collected == CollectedState.NotCollected)
            {
                Collected = CollectedState.Held;
                player.RPC_CollectFlag(Id);
            }
        }
        else if (localCollectedState != CollectedState.Held)
        {
            localCollectedState = CollectedState.Held;
            localCollectedTimer = 0f;
            flagGameObject.SetActive(false);
        }
    }
}