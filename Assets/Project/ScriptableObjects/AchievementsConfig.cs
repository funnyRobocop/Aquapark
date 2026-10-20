using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace NonameGame
{
    public enum AchievementType
    {
        Gold,
        Finish,
        Throw,
        Kick
    }

    [Serializable]
    public class AchievementDefinition
    {
        public string Id;               // уникальный id, например "kick_50"
        public AchievementType Type;
        public int Goal;
        public int RewardMoney;         // сколько денег даём при клейме
        public string[] Title;
        public string[] Description;           // текст для UI
        public Sprite Sprite;
    }

    [Serializable]
    public class BattlePassLevelReward
    {
        public int RequiredPoints;
        public int RewardMoney;
        public string Id;
    }

    [CreateAssetMenu(fileName = "AchievementsConfig", menuName = "Scriptable Objects/AchievementsConfig")]
    public class AchievementsConfig : ScriptableObject
    {
        [Header("Список всех ачивок")]
        [SerializeField] private List<AchievementDefinition> Achievements = new();

        private Dictionary<string, AchievementDefinition> _achievementsAll;
        public Dictionary<string, AchievementDefinition> AchievementsAll => _achievementsAll;

        [Header("Награды боевого пропуска по очкам")]
        public List<BattlePassLevelReward> LevelRewards = new();

        [Header("Целевые очки боевого пропуска")]
        public int TargetPoints;

        public void Initialize()
        {
            _achievementsAll = new Dictionary<string, AchievementDefinition>();

            foreach (var item in Achievements)
            {
                if (item != null && !_achievementsAll.ContainsKey(item.Id))
                {
                    _achievementsAll.Add(item.Id, item);
                }
            }
        }

        void OnValidate()
        {
            foreach (var item in Achievements)
            {
                if (item.Title.Length == 0 || item.Description.Length == 0)
                {
                    if (item.Type == AchievementType.Kick)
                    {
                        item.Title = new string[] { "Give 'Em a Kick!", "Пни их!" };
                        item.Description = new string[] { "Kick Other Players", "Пинай других игроков" };
                    }
                    else if (item.Type == AchievementType.Throw)
                    {
                        item.Title = new string[] { "Catch!", "Лови!" };
                        item.Description = new string[] { "Throw an Item at Another Player", "Бросай предметы в других игроков" };
                    }
                    else if (item.Type == AchievementType.Gold)
                    {
                        item.Title = new string[] { "#1 Number One", "#1 Номер один" };
                        item.Description = new string[] { "Take First Place", "Займи первое место" };
                    }
                    else if (item.Type == AchievementType.Finish)
                    {
                        item.Title = new string[] { "Cross the Line", "Перейти черту" };
                        item.Description = new string[] { "Reach the Finish Line", "Достигни финиша" };
                    }
                }
            }
        }
    }    
}
