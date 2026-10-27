using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;


namespace NonameGame
{
    public interface IPlayerSkinLoader
    {        
        Dictionary<PlayerSkin, PlayerSkinData> PlayerSkinAll { get; }
        void Initialize();
        public void ReleaseAll();
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

        public void ReleaseAll()
        {
            
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

        public string GetSkinTypeString(int lang)
        {
            if (SkinType == PlayerSkinType.Rare)
            {
                if (lang == 1)
                    return "Редкий";
                else
                    return "Rare";
            }
            else
            {
                if (lang == 1)
                    return "Обычный";
                else
                    return "Regular";
            };
        }

        public string GetNameString(int lang)
        {
            if (lang == 1)
                return NameRu;
            else
                return NameEn;
        }
        
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
        Tomato,
        None
    }
}
