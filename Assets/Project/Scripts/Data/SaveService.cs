using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YG;

namespace NonameGame
{
    public class SaveService
    {
        public void Save(GameModel model)
        {
            // Основные данные
            YG2.saves.PlayerName = model.PlayerName ?? "";
            YG2.saves.SelectedSkinId = model.SelectedSkinId;
            YG2.saves.PurchasedSkinIdList = model.PurchasedSkinIdList?.ToArray() ?? new int[0];
            YG2.saves.Money = model.Money;
            YG2.saves.Points = model.Points;
            YG2.saves.ClaimedBattlePassRewards = model.ClaimedBattlePassRewards?.ToArray() ?? new string[0];

            // Статистика
            if (model.StatsList != null && model.StatsList.Count > 0)
            {
                YG2.saves.StatsIds = model.StatsList.Select(s => s.Id).ToArray();
                YG2.saves.StatsFinish = model.StatsList.Select(s => s.FinishCount).ToArray();
                YG2.saves.StatsGold = model.StatsList.Select(s => s.GoldCount).ToArray();
                YG2.saves.StatsKick = model.StatsList.Select(s => s.KickCount).ToArray();
                YG2.saves.StatsThrow = model.StatsList.Select(s => s.ThrowCount).ToArray();
            }
            else
            {
                YG2.saves.StatsIds = YG2.saves.StatsFinish = YG2.saves.StatsGold =
                YG2.saves.StatsKick = YG2.saves.StatsThrow = new int[0];
            }

            // Ачивки
            if (model.Achievements != null && model.Achievements.Count > 0)
            {
                YG2.saves.AchievementIds = model.Achievements.Select(a => a.Id).ToArray();
                YG2.saves.AchievementCurrent = model.Achievements.Select(a => a.Current).ToArray();
                YG2.saves.AchievementGoal = model.Achievements.Select(a => a.Goal).ToArray();
                YG2.saves.AchievementRewardTaken = model.Achievements.Select(a => a.RewardTaken).ToArray();
            }
            else
            {
                YG2.saves.AchievementIds = new string[0];
                YG2.saves.AchievementCurrent = YG2.saves.AchievementGoal = new int[0];
                YG2.saves.AchievementRewardTaken = new bool[0];
            }

            YG2.SaveProgress(); // ← отправка в облако
        }

        public void LoadInto(GameModel model)
        {
            model.PlayerName = YG2.saves.PlayerName;
            model.SelectedSkinId = YG2.saves.SelectedSkinId;
            model.PurchasedSkinIdList = YG2.saves.PurchasedSkinIdList?.ToList() ?? new List<int>();
            model.Money = YG2.saves.Money;
            model.Points = YG2.saves.Points;
            model.ClaimedBattlePassRewards = YG2.saves.ClaimedBattlePassRewards?.ToList() ?? new List<string>();

            // Статистика
            model.StatsList = new List<GameModel.Stats>();
            if (YG2.saves.StatsIds != null)
            {
                for (int i = 0; i < YG2.saves.StatsIds.Length; i++)
                {
                    model.StatsList.Add(new GameModel.Stats(
                        YG2.saves.StatsIds[i],
                        YG2.saves.StatsFinish[i],
                        YG2.saves.StatsGold[i],
                        YG2.saves.StatsKick[i],
                        YG2.saves.StatsThrow[i]
                    ));
                }
            }

            // Ачивки
            model.Achievements = new List<GameModel.AchievementData>();
            if (YG2.saves.AchievementIds != null)
            {
                for (int i = 0; i < YG2.saves.AchievementIds.Length; i++)
                {
                    var ach = new GameModel.AchievementData(
                        YG2.saves.AchievementIds[i],
                        YG2.saves.AchievementGoal[i]
                    );
                    ach.Current = YG2.saves.AchievementCurrent[i];
                    ach.RewardTaken = YG2.saves.AchievementRewardTaken[i];
                    model.Achievements.Add(ach);
                }
            }
        }
    }
}