using UnityEngine;

namespace NonameGame
{
    public interface IAchievementsConfig
    {
        int TargetGoldCount { get; }
        int TargetFinishCount { get; }
        int TargetThrowCount { get; }
        int TargetKickCount { get; }
        int TargetPoints { get; }
    }

    [CreateAssetMenu(fileName = "AchievementsConfig", menuName = "Scriptable Objects/AchievementsConfig")]
    public class AchievementsConfig : ScriptableObject, IAchievementsConfig
    {
        [Header("Weekly Quest Goals")]
        [SerializeField] private int targetGoldCount;
        [SerializeField] private int targetFinishCount;
        [SerializeField] private int targetThrowCount;
        [SerializeField] private int targetKickCount;
        [SerializeField] private int targetPoints;

        public int TargetGoldCount => targetGoldCount;

        public int TargetFinishCount => targetFinishCount;

        public int TargetThrowCount => targetThrowCount;

        public int TargetKickCount => targetKickCount;
        public int TargetPoints => targetPoints;
    }
}
