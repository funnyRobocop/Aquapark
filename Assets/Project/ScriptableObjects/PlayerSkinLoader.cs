using System.Collections.Generic;
using UnityEngine;


namespace NonameGame
{
    public interface IPlayerSkinLoader
    {
        Dictionary<PlayerSkin, PlayerSkinData> PlayerSkinAll { get; }
    }

    [CreateAssetMenu(fileName = "PlayerSkinLoader", menuName = "Scriptable Objects/PlayerSkinLoader")]
    public class PlayerSkinLoader : ScriptableObject, IPlayerSkinLoader
    {
        [SerializeField] private Dictionary<PlayerSkin, PlayerSkinData> playerSkinAll;

        public Dictionary<PlayerSkin, PlayerSkinData> PlayerSkinAll => playerSkinAll;
    }


    [System.Serializable]
    public class PlayerSkinData
    {
        public PlayerSkin SkinId;
        public PlayerSkinType SkinType;
        public string NameRu;
        public string NameEn;
        public Sprite Sprite;
        public GameObject Prefab;

        public string GetSkinTypeString()
        {
            if (SkinType == PlayerSkinType.Rare)
                return "Rare";
                
            return "Common";
        }

        public string GetNameString()
        {
            return NameEn;
        }
    }

    public enum PlayerSkinType
    {
        Common,
        Rare
    }

    public enum PlayerSkin
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
