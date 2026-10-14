using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;


namespace NonameGame
{
    public interface IPlayerSkinLoader
    {        
        Dictionary<PlayerSkin, PlayerSkinData> PlayerSkinAll { get; }
        void Initialize();
    }

    [CreateAssetMenu(fileName = "PlayerSkinLoader", menuName = "Scriptable Objects/PlayerSkinLoader")]
    public class PlayerSkinLoader : ScriptableObject, IPlayerSkinLoader
    {
        [SerializeField] private List<PlayerSkinData> skinsList = new List<PlayerSkinData>();

        private Dictionary<PlayerSkin, PlayerSkinData> _playerSkinAll;
        public Dictionary<PlayerSkin, PlayerSkinData> PlayerSkinAll => _playerSkinAll;

        public void Initialize()
        {
            _playerSkinAll = new Dictionary<PlayerSkin, PlayerSkinData>();

            foreach (var skin in skinsList)
            {
                if (skin != null && !_playerSkinAll.ContainsKey(skin.SkinId))
                {
                    _playerSkinAll.Add(skin.SkinId, skin);
                }
            }
        }
    }

    [System.Serializable]
    public class PlayerSkinData
    {
        public PlayerSkin SkinId;
        public PlayerSkinType SkinType;
        public string NameRu;
        public string NameEn;

        // Теперь эти поля гарантированно появятся в инспекторе внутри списка!
        public AssetReferenceSprite SpriteReference;
        public AssetReference PrefabReference;

        public string GetSkinTypeString() => SkinType == PlayerSkinType.Rare ? "Rare" : "Common";
        public string GetNameString() => NameEn;
        public int Price => SkinType == PlayerSkinType.Rare ? 5000 : 1000;
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
