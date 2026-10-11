using System;
using System.IO;
using UnityEngine;

namespace TheLastPatient
{
    public static class SaveSystem
    {
        public static string SavePath => Path.Combine(Application.persistentDataPath, "chapter01.json");
        public static bool Exists => File.Exists(SavePath);
        public static void Write(SaveData data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            string temporary = SavePath + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
            if (File.Exists(SavePath)) File.Replace(temporary, SavePath, SavePath + ".bak");
            else File.Move(temporary, SavePath);
        }
        public static SaveData Read(ChapterDefinition chapter)
        {
            if (!File.Exists(SavePath)) throw new IOException("Noch kein Checkpoint vorhanden.");
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (!Validate(data, chapter, out string reason)) throw new InvalidDataException(reason);
            return data;
        }
        public static bool Validate(SaveData data, ChapterDefinition chapter, out string reason)
        {
            reason = "Ungültiger oder inkompatibler Spielstand.";
            if (data == null || chapter == null || data.version != 1 || data.chapterId != chapter.id) return false;
            if (data.completedObjectives == null || data.flags == null || data.inventory == null || data.documents == null
                || data.choices == null || data.puzzles == null || data.unlockedEndings == null) return false;
            if (!Finite(data.position.x) || !Finite(data.position.y) || !Finite(data.position.z)
                || !Finite(data.yaw) || !Finite(data.pitch) || !Finite(data.battery)) return false;
            if (Mathf.Abs(data.position.x) > 12 || data.position.z < -18 || data.position.z > 42
                || data.position.y < -1 || data.position.y > 4 || data.battery < 0 || data.battery > 100) return false;
            if (chapter.objectives == null || data.completedObjectives.Count > chapter.objectives.Length) return false;
            for (int i = 0; i < data.completedObjectives.Count; i++)
                if (chapter.objectives[i] == null || chapter.objectives[i].id != data.completedObjectives[i]) return false;
            // Prevent a partially written save from removing necessary items behind locked doors.
            if (data.completedObjectives.Contains(StoryIds.Enter) && !data.flags.Contains(StoryIds.Inside)) return false;
            if (data.completedObjectives.Contains(StoryIds.Flashlight)
                && (!data.inventory.Contains(StoryIds.HasFlashlight) || !data.flags.Contains(StoryIds.HasFlashlight))) return false;
            if (data.completedObjectives.Contains(StoryIds.Key)
                && (!data.inventory.Contains(StoryIds.WardKey) || !data.flags.Contains(StoryIds.WardKey))) return false;
            if (data.completedObjectives.Contains(StoryIds.Tape) && !data.flags.Contains(StoryIds.TapeHeard)) return false;
            reason = string.Empty; return true;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
