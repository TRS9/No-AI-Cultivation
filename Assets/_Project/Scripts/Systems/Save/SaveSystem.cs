using UnityEngine;
using System.IO;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    public static class SaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "cultivator_save.json");

        public static void SaveGame(SaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"Spiel gespeichert unter: {SavePath}");
        }

        public static SaveData LoadGame()
        {
            if (!File.Exists(SavePath)) return null;

            string json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<SaveData>(json);
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
    }
}