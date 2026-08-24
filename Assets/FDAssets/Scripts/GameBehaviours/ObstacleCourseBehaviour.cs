using Fusion;
using UnityEngine;

[Tooltip("Manages the state of an obstacle course including the flag and different tracks.")]
public class ObstacleCourseBehaviour : NetworkBehaviour
{
    [SerializeField, Tooltip("Reference to the flag at the end of the course.")]
    FlagNetworkBehaviour flag;

    [Networked, Tooltip("The direction the course is currenlty moving.")]
    public int Direction { get; set; }

    [Networked, Tooltip("Represents if the game is still active.  If true, the track will reset after descending.")]
    public NetworkBool InGame { get; set; }

    [SerializeField, Tooltip("The speed at which the track moves when translating.")]
    float resetSpeed;

    [SerializeField, Tooltip("The Y position at which the track will stop moving or reset if in game.")]
    float lowerLimit;

    [Networked, Capacity(3), OnChangedRender(nameof(OnChangeObstacleCourseTracks))]
    [Tooltip("The child indices of the track segments that will be active.")]
    public NetworkArray<int> CourseSegmentValues => default;

    [SerializeField, Tooltip("Array of the Transforms of the obstacle source segments.")]
    Transform[] ObstacleCourseTracks;

    /// <summary>
    /// Reference to the Transform Component.
    /// </summary>
    Transform cachedTransform;

    public override void Spawned()
    {
        cachedTransform = transform;

        if (HasStateAuthority)
            RandomizeTracks();
        OnChangeObstacleCourseTracks();
    }

    private void RandomizeTracks()
    {
        for (int i = 0; i < CourseSegmentValues.Length; i++)
        {
            CourseSegmentValues.Set(i, UnityEngine.Random.Range(0, ObstacleCourseTracks[i].childCount));
        }
    }

    public override void FixedUpdateNetwork()
    {
        // If the track is no longer moving and the flag has been deposited, we make the course descend.
        if (Direction == 0)
        {
            if (flag.Collected == FlagNetworkBehaviour.CollectedState.Deposited)
            {
                Direction = -1;
                flag.Collected = FlagNetworkBehaviour.CollectedState.Resetting;
            }
            return;
        }

        // The position code for the track.
        cachedTransform.position += Direction * Vector3.up * Runner.DeltaTime * resetSpeed;

        // If the track is descending and reaches the lower limit, the flag value is reset and the segments are randomized.
        if (Direction < 0)
        {
            if (cachedTransform.position.y < lowerLimit)
            {

                flag.Collected = FlagNetworkBehaviour.CollectedState.NotCollected;
                // TODO:  Reset the course itself

                if (InGame)
                {
                    RandomizeTracks();
                    Direction = 1;
                }

                // Keeps the track from descending too far down.
                cachedTransform.position = Vector3.up * lowerLimit;
            }
            return;
        }

        // Prevents the track from passing 0 and 
        if (cachedTransform.position.y >= 0f)
        {
            cachedTransform.position = Vector3.zero;
            Direction = 0;
        }
    }

    /// <summary>
    /// Sets which tracks are active when the 
    /// </summary>
    void OnChangeObstacleCourseTracks()
    {
        for (int i = 0; i < ObstacleCourseTracks.Length; i++)
        {
            for (int j = 0; j < ObstacleCourseTracks[i].childCount; j++)
            {
                ObstacleCourseTracks[i].GetChild(j).gameObject.SetActive(CourseSegmentValues[i] == j);
            }
        }
    }

    /// <summary>
    /// Causes the track to ascend and indicate that the game has started.
    /// </summary>
    public void StartGame()
    {
        InGame = true;
        Direction = 1;
    }

    /// <summary>
    /// Causes the course to descend and indicate that the game has ended.
    /// </summary>
    internal void EndGame()
    {
        InGame = false;
        Direction = -2;
    }
}
