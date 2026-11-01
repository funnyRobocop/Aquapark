namespace YG
{
    public partial class SavesYG
    {
        public string PlayerName = "";
        public int SelectedSkinId = -1;
        public int[] PurchasedSkinIdList = new int[0];
        public int Money = 1000;
        public int Points = 0;

        // Статистика
        public int[] StatsIds = new int[0];
        public int[] StatsFinish = new int[0];
        public int[] StatsGold = new int[0];
        public int[] StatsKick = new int[0];
        public int[] StatsThrow = new int[0];

        // Ачивки
        public string[] AchievementIds = new string[0];
        public int[] AchievementCurrent = new int[0];
        public int[] AchievementGoal = new int[0];
        public bool[] AchievementRewardTaken = new bool[0];

        // Battle Pass
        public string[] ClaimedBattlePassRewards = new string[0];
    }
}