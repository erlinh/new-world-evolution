using Godot;
using System.Collections.Generic;
using NewWorldEvolution.Data;

namespace NewWorldEvolution.Player
{
    public partial class PlayerStats : Node
    {
        [Export] public int Level { get; set; } = 1;
        [Export] public int CurrentExperience { get; set; } = 0;
        [Export] public int ExperienceToNext { get; set; } = 100;
        [Export] public int StatPoints { get; set; } = 0;

        // Base Stats
        [Export] public int Strength { get; set; } = 10;
        [Export] public int Intelligence { get; set; } = 10;
        [Export] public int Dexterity { get; set; } = 10;
        [Export] public int Constitution { get; set; } = 10;
        [Export] public int Wisdom { get; set; } = 10;
        [Export] public int Charisma { get; set; } = 10;

        // Derived Stats
        [Export] public int Health { get; set; } = 100;
        [Export] public int MaxHealth { get; set; } = 100;
        [Export] public int Mana { get; set; } = 50;
        [Export] public int MaxMana { get; set; } = 50;
        [Export] public int Stamina { get; set; } = 100;
        [Export] public int MaxStamina { get; set; } = 100;

        // Special Stats
        [Export] public int Corruption { get; set; } = 0;
        [Export] public int Honor { get; set; } = 0;
        [Export] public int Fame { get; set; } = 0;

        // Combat Stats
        [Export] public int AttackPower { get; set; } = 10;
        [Export] public int MagicPower { get; set; } = 10;
        [Export] public int Defense { get; set; } = 5;
        [Export] public int MagicDefense { get; set; } = 5;
        [Export] public float CriticalChance { get; set; } = 0.05f;
        [Export] public float DodgeChance { get; set; } = 0.05f;

        public Dictionary<string, int> CustomStats { get; private set; }

        public override void _Ready()
        {
            CustomStats = new Dictionary<string, int>();
            CalculateDerivedStats();
        }

        public void InitializeFromRace(RaceData raceData)
        {
            if (raceData.BaseStats != null)
            {
                foreach (var stat in raceData.BaseStats)
                {
                    ApplyStatBonus(stat.Key, stat.Value);
                }
            }
            
            CalculateDerivedStats();
        }

        public void ApplyStatBonus(string statName, int bonus)
        {
            switch (statName.ToLower())
            {
                case "strength":
                    Strength += bonus;
                    break;
                case "intelligence":
                    Intelligence += bonus;
                    break;
                case "dexterity":
                    Dexterity += bonus;
                    break;
                case "constitution":
                    Constitution += bonus;
                    break;
                case "wisdom":
                    Wisdom += bonus;
                    break;
                case "charisma":
                    Charisma += bonus;
                    break;
                case "corruption":
                    Corruption += bonus;
                    break;
                case "honor":
                    Honor += bonus;
                    break;
                case "fame":
                    Fame += bonus;
                    break;
                default:
                    if (CustomStats.ContainsKey(statName))
                        CustomStats[statName] += bonus;
                    else
                        CustomStats[statName] = bonus;
                    break;
            }
            
            CalculateDerivedStats();
        }

        public void CalculateDerivedStats()
        {
            // Calculate health based on constitution
            int newMaxHealth = 50 + (Constitution * 10) + (Level * 5);
            if (MaxHealth != newMaxHealth)
            {
                float healthRatio = MaxHealth > 0 ? (float)Health / MaxHealth : 1.0f;
                MaxHealth = newMaxHealth;
                Health = Mathf.RoundToInt(MaxHealth * healthRatio);
            }

            // Calculate mana based on intelligence and wisdom
            int newMaxMana = 25 + (Intelligence * 5) + (Wisdom * 3) + (Level * 2);
            if (MaxMana != newMaxMana)
            {
                float manaRatio = MaxMana > 0 ? (float)Mana / MaxMana : 1.0f;
                MaxMana = newMaxMana;
                Mana = Mathf.RoundToInt(MaxMana * manaRatio);
            }

            // Calculate stamina based on constitution and dexterity
            int newMaxStamina = 75 + (Constitution * 5) + (Dexterity * 3) + (Level * 3);
            if (MaxStamina != newMaxStamina)
            {
                float staminaRatio = MaxStamina > 0 ? (float)Stamina / MaxStamina : 1.0f;
                MaxStamina = newMaxStamina;
                Stamina = Mathf.RoundToInt(MaxStamina * staminaRatio);
            }

            // Calculate combat stats
            AttackPower = 5 + Strength + (Level / 2);
            MagicPower = 5 + Intelligence + (Level / 2);
            Defense = 2 + (Constitution / 2) + (Level / 3);
            MagicDefense = 2 + (Wisdom / 2) + (Level / 3);
            CriticalChance = 0.05f + (Dexterity * 0.002f);
            DodgeChance = 0.05f + (Dexterity * 0.003f);
        }

        public int CalculateExperienceToNext()
        {
            return 100 + (Level * 50) + (Level * Level * 10);
        }

        public void RestoreHealth(int amount)
        {
            Health = Mathf.Min(Health + amount, MaxHealth);
        }

        public void RestoreMana(int amount)
        {
            Mana = Mathf.Min(Mana + amount, MaxMana);
        }

        public void RestoreStamina(int amount)
        {
            Stamina = Mathf.Min(Stamina + amount, MaxStamina);
        }

        public bool ConsumeHealth(int amount)
        {
            if (Health >= amount)
            {
                Health -= amount;
                return true;
            }
            return false;
        }

        public bool ConsumeMana(int amount)
        {
            if (Mana >= amount)
            {
                Mana -= amount;
                return true;
            }
            return false;
        }

        public bool ConsumeStamina(int amount)
        {
            if (Stamina >= amount)
            {
                Stamina -= amount;
                return true;
            }
            return false;
        }

        public int GetStatValue(string statName)
        {
            switch (statName.ToLower())
            {
                case "level": return Level;
                case "strength": return Strength;
                case "intelligence": return Intelligence;
                case "dexterity": return Dexterity;
                case "constitution": return Constitution;
                case "wisdom": return Wisdom;
                case "charisma": return Charisma;
                case "health": return Health;
                case "maxhealth": return MaxHealth;
                case "mana": return Mana;
                case "maxmana": return MaxMana;
                case "stamina": return Stamina;
                case "maxstamina": return MaxStamina;
                case "corruption": return Corruption;
                case "honor": return Honor;
                case "fame": return Fame;
                case "attackpower": return AttackPower;
                case "magicpower": return MagicPower;
                case "defense": return Defense;
                case "magicdefense": return MagicDefense;
                default:
                    return CustomStats.ContainsKey(statName) ? CustomStats[statName] : 0;
            }
        }

        public void ModifyCorruption(int amount)
        {
            Corruption += amount;
            Corruption = Mathf.Max(0, Corruption); // Corruption can't go below 0
            
            // Check for corruption-based changes
            CheckCorruptionEffects();
        }

        public void ModifyHonor(int amount)
        {
            Honor += amount;
            Honor = Mathf.Max(0, Honor); // Honor can't go below 0
            
            // Check for honor-based changes
            CheckHonorEffects();
        }

        private void CheckCorruptionEffects()
        {
            if (Corruption >= 100)
            {
                // High corruption might unlock dark goals or transformations
                GD.Print("High corruption detected - dark powers awaken...");
            }
        }

        private void CheckHonorEffects()
        {
            if (Honor >= 100)
            {
                // High honor might unlock heroic goals or abilities
                GD.Print("High honor detected - heroic destiny calls...");
            }
        }
    }
}
