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
            try
            {
                string json = JsonUtility.ToJson(data, true);

                // Write to temp file first, then rename atomically
                // so a crash mid-write never corrupts the existing save.
                WriteAtomically(SavePath, json);

                Debug.Log($"Spiel gespeichert unter: {SavePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Speichern fehlgeschlagen: {e.Message}");
            }
        }

        private static void WriteAtomically(string path, string json)
        {
            string tempPath = path + ".tmp";
            try
            {
                File.WriteAllText(tempPath, json);
                if (File.Exists(path)) File.Replace(tempPath, path, null);
                else File.Move(tempPath, path);
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
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
