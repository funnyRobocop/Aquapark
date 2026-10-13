using UnityEngine;

namespace NonameGame
{
    public class SaveService
    {
        private const string SaveKey = "KingOfTheHill.SaveData";

        // Сохраняем всю модель целиком в JSON-строку
        public void Save(GameModel model)
        {
            string json = JsonUtility.ToJson(model);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save(); // На WebGL вызывать строго в редкие моменты (окна UI, конец раунда)
        }

        // Загружаем данные. Если сохранения нет — создаем чистую GameModel
        public GameModel Load()
        {
            if (PlayerPrefs.HasKey(SaveKey))
            {
                string json = PlayerPrefs.GetString(SaveKey);
                return JsonUtility.FromJson<GameModel>(json);
            }

            return CreateDefaultData();
        }

        // Начальные данные для нового игрока
        private GameModel CreateDefaultData()
        {
            return new GameModel(this)
            {
                SelectedSkinId = 0,
                PurchasedSkinIdList = new System.Collections.Generic.List<int> { },
                Money = 0,
                Points = 0,
                StatsList = new System.Collections.Generic.List<GameModel.Stats>()
            };
        }
    }
}

