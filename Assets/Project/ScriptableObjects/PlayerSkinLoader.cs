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
        // 1. В инспекторе мы настраиваем простой, понятный для Unity список.
        // Он будет отлично отображать все поля, включая AssetReference!
        [SerializeField] private List<PlayerSkinData> skinsList = new List<PlayerSkinData>();

        // 2. Сам словарь делаем скрытым от инспектора, но доступным через интерфейс
        private Dictionary<PlayerSkin, PlayerSkinData> _playerSkinAll;
        public Dictionary<PlayerSkin, PlayerSkinData> PlayerSkinAll => _playerSkinAll;

        // 3. Этот метод нужно вызвать ОДИН раз при старте игры (например, в вашем GameManager или Bootstrapper)
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
