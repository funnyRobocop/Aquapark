using UnityEngine;

namespace NonameGame
{
    public class SaveService
    {
        private const string SaveKey = "KingOfTheHill.SaveData";

        // Сохраняем модель целиком в JSON-строку
        public void Save(GameModel model)
        {
            string json = JsonUtility.ToJson(model);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        // Загружаем данные. Если сохранения нет — возвращаем null
        public GameModel Load()
        {
            if (PlayerPrefs.HasKey(SaveKey))
            {
                string json = PlayerPrefs.GetString(SaveKey);
                return JsonUtility.FromJson<GameModel>(json);
            }

            return null;
        }
    }
}