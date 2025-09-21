using Godot;
using System.Collections.Generic;
using System.Linq;
using NewWorldEvolution.Data;
using NewWorldEvolution.Core;

namespace NewWorldEvolution.Skills
{
    public partial class SkillManager : Node
    {
        public Dictionary<string, LearnedSkill> LearnedSkills { get; private set; }
        public Dictionary<string, int> SkillExperience { get; private set; }
        public List<string> ActiveSkillSlots { get; private set; }
        
        [Export] public int MaxActiveSkills = 6;

        [Signal] public delegate void SkillLearnedEventHandler(string skillName);
        [Signal] public delegate void SkillLevelUpEventHandler(string skillName, int newLevel);
        [Signal] public delegate void SkillUsedEventHandler(string skillName);

        public override void _Ready()
        {
            LearnedSkills = new Dictionary<string, LearnedSkill>();
            SkillExperience = new Dictionary<string, int>();
            ActiveSkillSlots = new List<string>(new string[MaxActiveSkills]);
        }

        public void InitializeStartingSkills(List<string> startingSkills)
        {
            foreach (string skillName in startingSkills)
            {
                LearnSkill(skillName, 1);
            }
        }

        public bool LearnSkill(string skillName, int initialLevel = 1)
        {
            var skillData = GameManager.Instance.GetSkillData(skillName);
            if (skillData == null)
            {
                GD.PrintErr($"Skill data not found: {skillName}");
                return false;
            }

            if (LearnedSkills.ContainsKey(skillName))
            {
                GD.Print($"Skill {skillName} already learned");
                return false;
            }

            if (!CanLearnSkill(skillData))
            {
                GD.Print($"Cannot learn skill {skillName} - requirements not met");
                return false;
            }

            var learnedSkill = new LearnedSkill
            {
                Name = skillName,
                Level = initialLevel,
                IsActive = skillData.Type == SkillType.Active
            };

            LearnedSkills[skillName] = learnedSkill;
            SkillExperience[skillName] = 0;

            // Apply passive skill effects immediately
            if (skillData.Type == SkillType.Passive)
            {
                ApplyPassiveSkillEffects(skillData, initialLevel);
            }

            EmitSignal(SignalName.SkillLearned, skillName);
            GD.Print($"Learned skill: {skillName} at level {initialLevel}");
            return true;
        }

        public bool UnlockSkill(string skillName)
        {
            return LearnSkill(skillName, 1);
        }

        public bool CanLearnSkill(SkillData skillData)
        {
            // Check prerequisites
            foreach (string prerequisite in skillData.Prerequisites)
            {
                if (!LearnedSkills.ContainsKey(prerequisite))
                    return false;
            }

            // Check requirements
            var playerStats = GetNode<Player.PlayerStats>("../PlayerStats");
            if (playerStats == null) return false;

            foreach (var requirement in skillData.Requirements)
            {
                if (!CheckRequirement(requirement.Key, requirement.Value, playerStats))
                    return false;
            }

            // Check race restrictions
            string currentRace = GameManager.Instance.CurrentPlayerRace;
            if (skillData.RestrictedToRaces.Count > 0 && !skillData.RestrictedToRaces.Contains(currentRace))
                return false;

            // Check profession restrictions (if applicable)
            var playerController = GetNode<Player.PlayerController>("..");
            if (playerController != null && skillData.RestrictedToProfessions.Count > 0)
            {
                if (string.IsNullOrEmpty(playerController.CurrentProfession) || 
                    !skillData.RestrictedToProfessions.Contains(playerController.CurrentProfession))
                    return false;
            }

            return true;
        }

        private bool CheckRequirement(string requirementType, object value, Player.PlayerStats stats)
        {
            switch (requirementType.ToLower())
            {
                case "level":
                    return stats.Level >= (int)value;
                case "strength":
                    return stats.Strength >= (int)value;
                case "intelligence":
                    return stats.Intelligence >= (int)value;
                case "dexterity":
                    return stats.Dexterity >= (int)value;
                case "constitution":
                    return stats.Constitution >= (int)value;
                case "wisdom":
                    return stats.Wisdom >= (int)value;
                case "charisma":
                    return stats.Charisma >= (int)value;
                default:
                    return true;
            }
        }

        public bool UseSkill(string skillName, Node target = null)
        {
            if (!LearnedSkills.ContainsKey(skillName))
            {
                GD.Print($"Skill {skillName} not learned");
                return false;
            }

            var skillData = GameManager.Instance.GetSkillData(skillName);
            if (skillData == null || skillData.Type != SkillType.Active)
            {
                GD.Print($"Skill {skillName} is not an active skill");
                return false;
            }

            var learnedSkill = LearnedSkills[skillName];
            if (!skillData.LevelData.ContainsKey(learnedSkill.Level))
            {
                GD.PrintErr($"Level data not found for {skillName} level {learnedSkill.Level}");
                return false;
            }

            var levelData = skillData.LevelData[learnedSkill.Level];

            // Check cooldown
            if (learnedSkill.IsOnCooldown())
            {
                GD.Print($"Skill {skillName} is on cooldown");
                return false;
            }

            // Check mana cost
            var playerStats = GetNode<Player.PlayerStats>("../PlayerStats");
            if (playerStats != null && !playerStats.ConsumeMana(levelData.ManaCost))
            {
                GD.Print($"Not enough mana to use {skillName}");
                return false;
            }

            // Execute skill effect
            ExecuteActiveSkillEffect(skillData, levelData, target);

            // Set cooldown
            learnedSkill.LastUsed = Time.GetTimeStringFromSystem();

            // Gain skill experience
            GainSkillExperience(skillName, 10);

            EmitSignal(SignalName.SkillUsed, skillName);
            GD.Print($"Used skill: {skillName}");
            return true;
        }

        private void ExecuteActiveSkillEffect(SkillData skillData, SkillLevelData levelData, Node target)
        {
            switch (skillData.Category)
            {
                case SkillCategory.Combat:
                    ExecuteCombatSkill(skillData, levelData, target);
                    break;
                case SkillCategory.Magic:
                    ExecuteMagicSkill(skillData, levelData, target);
                    break;
                case SkillCategory.Stealth:
                    ExecuteStealthSkill(skillData, levelData);
                    break;
                // Add more categories as needed
                default:
                    GD.Print($"Executing generic skill effect for {skillData.Name}");
                    break;
            }
        }

        private void ExecuteCombatSkill(SkillData skillData, SkillLevelData levelData, Node target)
        {
            // Example combat skill implementation
            float damage = levelData.EffectValue;
            GD.Print($"Combat skill {skillData.Name} deals {damage} damage");
            
            // Apply damage to target if it exists and has appropriate methods
        }

        private void ExecuteMagicSkill(SkillData skillData, SkillLevelData levelData, Node target)
        {
            // Example magic skill implementation
            float magicEffect = levelData.EffectValue;
            GD.Print($"Magic skill {skillData.Name} creates magical effect with power {magicEffect}");
        }

        private void ExecuteStealthSkill(SkillData skillData, SkillLevelData levelData)
        {
            // Example stealth skill implementation
            float stealthBonus = levelData.EffectValue;
            GD.Print($"Stealth skill {skillData.Name} provides {stealthBonus} stealth bonus");
        }

        private void ApplyPassiveSkillEffects(SkillData skillData, int level)
        {
            if (!skillData.LevelData.ContainsKey(level)) return;

            var levelData = skillData.LevelData[level];
            var playerStats = GetNode<Player.PlayerStats>("../PlayerStats");
            
            if (playerStats != null && levelData.StatBonuses != null)
            {
                foreach (var bonus in levelData.StatBonuses)
                {
                    playerStats.ApplyStatBonus(bonus.Key, (int)bonus.Value);
                }
            }
        }

        public void GainSkillExperience(string skillName, int amount)
        {
            if (!LearnedSkills.ContainsKey(skillName) || !SkillExperience.ContainsKey(skillName))
                return;

            SkillExperience[skillName] += amount;
            
            var learnedSkill = LearnedSkills[skillName];
            var skillData = GameManager.Instance.GetSkillData(skillName);
            
            if (skillData != null && learnedSkill.Level < skillData.MaxLevel)
            {
                int experienceNeeded = CalculateExperienceForNext(learnedSkill.Level);
                
                if (SkillExperience[skillName] >= experienceNeeded)
                {
                    LevelUpSkill(skillName);
                }
            }
        }

        private int CalculateExperienceForNext(int currentLevel)
        {
            return 100 + (currentLevel * 25);
        }

        private void LevelUpSkill(string skillName)
        {
            if (!LearnedSkills.ContainsKey(skillName)) return;

            var learnedSkill = LearnedSkills[skillName];
            var skillData = GameManager.Instance.GetSkillData(skillName);
            
            if (skillData != null && learnedSkill.Level < skillData.MaxLevel)
            {
                learnedSkill.Level++;
                SkillExperience[skillName] = 0;

                // Apply new level bonuses for passive skills
                if (skillData.Type == SkillType.Passive)
                {
                    ApplyPassiveSkillEffects(skillData, learnedSkill.Level);
                }

                EmitSignal(SignalName.SkillLevelUp, skillName, learnedSkill.Level);
                GD.Print($"Skill {skillName} leveled up to {learnedSkill.Level}!");
            }
        }

        public bool SetActiveSkill(string skillName, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxActiveSkills)
                return false;

            if (!LearnedSkills.ContainsKey(skillName))
                return false;

            var skillData = GameManager.Instance.GetSkillData(skillName);
            if (skillData?.Type != SkillType.Active)
                return false;

            ActiveSkillSlots[slotIndex] = skillName;
            GD.Print($"Set {skillName} to active slot {slotIndex}");
            return true;
        }

        public string GetActiveSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxActiveSkills)
                return null;

            return ActiveSkillSlots[slotIndex];
        }

        public List<string> GetAvailableSkills()
        {
            return LearnedSkills.Keys.ToList();
        }

        public LearnedSkill GetLearnedSkill(string skillName)
        {
            return LearnedSkills.ContainsKey(skillName) ? LearnedSkills[skillName] : null;
        }

        public int GetSkillLevel(string skillName)
        {
            return LearnedSkills.ContainsKey(skillName) ? LearnedSkills[skillName].Level : 0;
        }
    }

    public class LearnedSkill
    {
        public string Name { get; set; }
        public int Level { get; set; }
        public bool IsActive { get; set; }
        public string LastUsed { get; set; }

        public bool IsOnCooldown()
        {
            if (string.IsNullOrEmpty(LastUsed))
                return false;

            // Simple cooldown check - this could be more sophisticated
            // For now, assume 1 second cooldown for all skills
            return false; // Simplified for demo
        }
    }
}
