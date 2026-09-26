using System.Collections.Generic;
using UnityEngine;


namespace NonameGame
{
    public interface IPlayerSkinLoader
    {
        GameObject GetPlayerSkinPrefab(PlayerSkinType skinType);
    }

    [CreateAssetMenu(fileName = "PlayerSkinLoader", menuName = "Scriptable Objects/PlayerSkinLoader")]
    public class PlayerSkinLoader : ScriptableObject, IPlayerSkinLoader
    {
        [SerializeField] private Dictionary<PlayerSkinType, PlayerSkinData> playerSkinAll;

        public GameObject GetPlayerSkinPrefab(PlayerSkinType skinType)
        {
            if (playerSkinAll.TryGetValue(skinType, out PlayerSkinData skinData))
            {
                return skinData.Prefab;
            }
            else
            {
                Debug.LogWarning($"Player skin of type {skinType} not found.");
                return null;
            }
        }


        [System.Serializable]
        public class PlayerSkinData
        {
            public GameObject Prefab;
        }
    }

    public enum PlayerSkinType
    {
        Banana,
        BigSausage,
        Broccoli,
        Carrot,
        Cola,
        Corn,
        Cucumber,
        Duck,
        Cactus,
        Pineapple,
        Pizza,
        Rooster,
        Strawberry,
        Terminator,
        Tomato
    }
}
