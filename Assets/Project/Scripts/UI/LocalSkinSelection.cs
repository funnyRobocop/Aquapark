using UnityEngine;

namespace NonameGame
{
    // Local preference only. It does not prove ownership of a purchased skin.
    public static class LocalSkinSelection
    {
        private const string Key = "KingOfTheHill.SelectedSkinId";
        public static int SelectedSkinId => Mathf.Max(0, PlayerPrefs.GetInt(Key, 0));

        public static void Select(int skinId)
        {
            PlayerPrefs.SetInt(Key, Mathf.Max(0, skinId));
            PlayerPrefs.Save();
        }
    }
}
