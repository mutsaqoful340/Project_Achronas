using UnityEngine;
using System.IO;
using System.Collections.Generic;

public static class SaveSystem
{
    private static string path = Application.persistentDataPath + "/save.json";

    // 🔥 WRAPPER (WAJIB buat List)
    [System.Serializable]
    public class Wrapper
    {
        public List<SaveData> list;
    }

    // ✅ SAVE LIST
    public static void SaveGame(List<SaveData> data)
    {
        Wrapper wrapper = new Wrapper();
        wrapper.list = data;

        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(path, json);

        Debug.Log("Game Saved di: " + path);
        Debug.Log("Isi Save: " + json);
    }

    // ✅ LOAD LIST
    public static List<SaveData> LoadAllCheckpoints()
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);

            Debug.Log("Load dari: " + path);
            Debug.Log("Isi save: " + json);

            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(json);

            // 🔥 kalau kosong, biar nggak null
            if (wrapper == null || wrapper.list == null)
                return new List<SaveData>();

            return wrapper.list;
        }
        else
        {
            Debug.Log("Save tidak ditemukan di: " + path);
            return new List<SaveData>();
        }
    }

    // ✅ DELETE
    public static void DeleteSave()
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Save dihapus");
        }
    }
}