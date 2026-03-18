using UnityEngine;
using System.IO;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    public static class SaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "cultivator_save.json");
        private static string TempPath => SavePath + ".tmp";

        public static void SaveGame(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);

                // Write to temp file first, then rename atomically
                // so a crash mid-write never corrupts the existing save.
                File.WriteAllText(TempPath, json);
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(TempPath, SavePath);

                Debug.Log($"Spiel gespeichert unter: {SavePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Speichern fehlgeschlagen: {e.Message}");
                if (File.Exists(TempPath)) File.Delete(TempPath);
            }
        }

        public static SaveData LoadGame()
        {
            if (!File.Exists(SavePath)) return null;

            try
            {
                string json = File.ReadAllText(SavePath);
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Laden fehlgeschlagen: {e.Message}");
                return null;
            }
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
    }
}
