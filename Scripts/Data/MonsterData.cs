using Godot;
using System.Collections.Generic;

namespace NewWorldEvolution.Data
{
    public enum MonsterType
    {
        Beast,
        Humanoid,
        Magical,
        Undead,
        Elemental,
        Dragon,
        Aberration
    }

    public enum MonsterBehavior
    {
        Passive,      // Won't attack unless provoked
        Neutral,      // Will defend territory but not hunt
        Aggressive,   // Will attack on sight
        Hostile,      // Actively hunts players
        Territorial   // Defends specific area aggressively
    }

    public enum MonsterRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Boss
    }

    [System.Serializable]
    public class MonsterStats
    {
        public int Level { get; set; } = 1;
        public int Health { get; set; } = 100;
        public int MaxHealth { get; set; } = 100;
        public int Mana { get; set; } = 50;
        public int MaxMana { get; set; } = 50;
        public int Attack { get; set; } = 10;
        public int Defense { get; set; } = 5;
        public int Speed { get; set; } = 100;
        public float AttackSpeed { get; set; } = 1.0f;
        public float DetectionRange { get; set; } = 100.0f;
        public float AttackRange { get; set; } = 50.0f;
        public int ExperienceReward { get; set; } = 10;
    }

    [System.Serializable]
    public class MonsterData
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public MonsterType Type { get; set; }
        public MonsterBehavior Behavior { get; set; }
        public MonsterRarity Rarity { get; set; }
        public MonsterStats BaseStats { get; set; }
        public List<string> Abilities { get; set; }
        public List<string> LootTable { get; set; }
        public Dictionary<string, object> SpecialProperties { get; set; }
        public string SpritePath { get; set; }
        public Color NameColor { get; set; }
        public Vector2 SpriteScale { get; set; } = Vector2.One;

        public MonsterData()
        {
            BaseStats = new MonsterStats();
            Abilities = new List<string>();
            LootTable = new List<string>();
            SpecialProperties = new Dictionary<string, object>();
            NameColor = Colors.White;
        }

        public MonsterStats GetScaledStats(int level)
        {
            var stats = new MonsterStats
            {
                Level = level,
                MaxHealth = BaseStats.MaxHealth + (level - 1) * 15,
                MaxMana = BaseStats.MaxMana + (level - 1) * 5,
                Attack = BaseStats.Attack + (level - 1) * 3,
                Defense = BaseStats.Defense + (level - 1) * 2,
                Speed = BaseStats.Speed,
                AttackSpeed = BaseStats.AttackSpeed,
                DetectionRange = BaseStats.DetectionRange,
                AttackRange = BaseStats.AttackRange,
                ExperienceReward = BaseStats.ExperienceReward + (level - 1) * 5
            };
            
            stats.Health = stats.MaxHealth;
            stats.Mana = stats.MaxMana;
            
            return stats;
        }
    }
}
