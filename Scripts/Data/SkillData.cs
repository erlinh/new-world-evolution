using Godot;
using System.Collections.Generic;

namespace NewWorldEvolution.Data
{
    public enum SkillType
    {
        Active,
        Passive,
        Unique
    }

    public enum SkillCategory
    {
        Combat,
        Magic,
        Survival,
        Social,
        Stealth,
        Knowledge
    }

    [System.Serializable]
    public class SkillData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public SkillType Type { get; set; }
        public SkillCategory Category { get; set; }
        public int MaxLevel { get; set; }
        public Dictionary<string, object> Requirements { get; set; }
        public List<string> Prerequisites { get; set; }
        public Dictionary<int, SkillLevelData> LevelData { get; set; }
        public bool IsUnique { get; set; }
        public List<string> RestrictedToRaces { get; set; }
        public List<string> RestrictedToProfessions { get; set; }

        public SkillData()
        {
            Requirements = new Dictionary<string, object>();
            Prerequisites = new List<string>();
            LevelData = new Dictionary<int, SkillLevelData>();
            RestrictedToRaces = new List<string>();
            RestrictedToProfessions = new List<string>();
        }
    }

    [System.Serializable]
    public class SkillLevelData
    {
        public int Level { get; set; }
        public string Description { get; set; }
        public int ManaCost { get; set; }
        public int Cooldown { get; set; }
        public float EffectValue { get; set; }
        public Dictionary<string, float> StatBonuses { get; set; }

        public SkillLevelData()
        {
            StatBonuses = new Dictionary<string, float>();
        }
    }
}
