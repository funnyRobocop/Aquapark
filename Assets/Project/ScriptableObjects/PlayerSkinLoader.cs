using System.Collections.Generic;
using UnityEngine;


namespace NonameGame
{
    [CreateAssetMenu(fileName = "PlayerSkinLoader", menuName = "Scriptable Objects/PlayerSkinLoader")]
    public class PlayerSkinLoader : ScriptableObject
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
        Banana
    }
}
