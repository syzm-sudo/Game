using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheLastPatient
{
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public string chapterId = StoryIds.Chapter;
        public string currentObjective = StoryIds.Enter;
        public List<string> completedObjectives = new List<string>();
        public List<string> flags = new List<string>();
        public List<string> choices = new List<string>();
        public List<string> documents = new List<string>();
        public List<string> inventory = new List<string>();
        public List<string> puzzles = new List<string>();
        public List<string> unlockedEndings = new List<string>();
        public Vector3 position;
        public float yaw;
        public float pitch;
        public float battery = 100;
        public bool flashlightOn;
    }
}
