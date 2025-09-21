using Godot;
using System.Collections.Generic;
using System.Linq;
using NewWorldEvolution.Data;
using NewWorldEvolution.Core;

namespace NewWorldEvolution.Player.Profession
{
    public partial class ProfessionManager : Node
    {
        public Dictionary<string, ProfessionNode> ProfessionTree { get; private set; }
        public string CurrentProfession { get; private set; }
        public List<string> AvailableProfessions { get; private set; }
        public int ProfessionLevel { get; private set; }
        public int ProfessionExperience { get; private set; }

        [Signal] public delegate void ProfessionAvailableEventHandler(string professionName);
        [Signal] public delegate void ProfessionChangedEventHandler(string professionName);
        [Signal] public delegate void ProfessionLevelUpEventHandler(int newLevel);

        public override void _Ready()
        {
            ProfessionTree = new Dictionary<string, ProfessionNode>();
            AvailableProfessions = new List<string>();
            ProfessionLevel = 1;
            ProfessionExperience = 0;
            
            InitializeProfessionTree();
        }

        private void InitializeProfessionTree()
        {
            var playerController = GetNode<PlayerController>("..");
            if (playerController == null) return;

            string race = playerController.CurrentRace;
            var raceData = GameManager.Instance.GetRaceData(race);

            if (raceData?.CanEvolve == false && raceData.ProfessionPaths != null)
            {
                BuildProfessionTree(raceData);
                CheckAvailableProfessions();
            }
        }

        private void BuildProfessionTree(RaceData raceData)
        {
            // Create nodes for all professions
            foreach (var professionPath in raceData.ProfessionPaths)
            {
                var node = new ProfessionNode
                {
                    Name = professionPath.Key,
                    ProfessionData = professionPath.Value,
                    Children = professionPath.Value.NextProfessions?.ToList() ?? new List<string>(),
                    IsUnlocked = false
                };

                ProfessionTree[professionPath.Key] = node;
            }

            // Set up parent-child relationships
            foreach (var professionPath in raceData.ProfessionPaths)
            {
                AddToParentNodes(professionPath.Key, professionPath.Value);
            }

            // Mark base professions as available (those with no requirements)
            MarkBaseProfessions();
        }

        private void AddToParentNodes(string professionName, ProfessionPath professionData)
        {
            foreach (var kvp in ProfessionTree)
            {
                var node = kvp.Value;
                if (node.ProfessionData?.NextProfessions?.Contains(professionName) == true)
                {
                    if (!node.Children.Contains(professionName))
                        node.Children.Add(professionName);
                }
            }
        }

        private void MarkBaseProfessions()
        {
            foreach (var kvp in ProfessionTree)
            {
                var node = kvp.Value;
                if (node.ProfessionData.Requirements.Count == 0)
                {
                    node.IsUnlocked = true;
                    AvailableProfessions.Add(node.Name);
                }
            }
        }

        public void CheckAvailableProfessions()
        {
            var playerStats = GetNode<PlayerStats>("../PlayerStats");
            if (playerStats == null) return;

            var previouslyAvailable = new HashSet<string>(AvailableProfessions);
            
            // Don't clear available professions, only add new ones
            foreach (var kvp in ProfessionTree)
            {
                string professionName = kvp.Key;
                var node = kvp.Value;

                if (!node.IsUnlocked && CanChangeToProfession(node.ProfessionData, playerStats))
                {
                    node.IsUnlocked = true;
                    
                    if (!AvailableProfessions.Contains(professionName))
                    {
                        AvailableProfessions.Add(professionName);
                        
                        if (!previouslyAvailable.Contains(professionName))
                        {
                            EmitSignal(SignalName.ProfessionAvailable, professionName);
                            GD.Print($"New profession available: {professionName}");
                        }
                    }
                }
            }
        }

        private bool CanChangeToProfession(ProfessionPath professionData, PlayerStats playerStats)
        {
            if (professionData?.Requirements == null) return true;

            foreach (var requirement in professionData.Requirements)
            {
                if (!CheckProfessionRequirement(requirement.Key, requirement.Value, playerStats))
                    return false;
            }

            return true;
        }

        private bool CheckProfessionRequirement(string requirementType, object value, PlayerStats playerStats)
        {
            switch (requirementType.ToLower())
            {
                case "level":
                    return playerStats.Level >= (int)value;
                case "profession_level":
                    return ProfessionLevel >= (int)value;
                case "strength":
                    return playerStats.Strength >= (int)value;
                case "intelligence":
                    return playerStats.Intelligence >= (int)value;
                case "dexterity":
                    return playerStats.Dexterity >= (int)value;
                case "constitution":
                    return playerStats.Constitution >= (int)value;
                case "wisdom":
                    return playerStats.Wisdom >= (int)value;
                case "charisma":
                    return playerStats.Charisma >= (int)value;
                case "current_profession":
                    return CurrentProfession == value.ToString();
                case "skill":
                    var skillManager = GetNode<Skills.SkillManager>("../SkillManager");
                    if (skillManager != null && value is string skillName)
                    {
                        return skillManager.GetSkillLevel(skillName) > 0;
                    }
                    return false;
                case "skill_level":
                    if (value is string skillLevelStr)
                    {
                        var parts = skillLevelStr.Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int requiredLevel))
                        {
                            var skillMgr = GetNode<Skills.SkillManager>("../SkillManager");
                            return skillMgr?.GetSkillLevel(parts[0]) >= requiredLevel;
                        }
                    }
                    return false;
                default:
                    GD.PrintErr($"Unknown profession requirement: {requirementType}");
                    return false;
            }
        }

        public bool ChangeProfession(string professionName)
        {
            if (!AvailableProfessions.Contains(professionName))
            {
                GD.Print($"Profession {professionName} is not available");
                return false;
            }

            if (!ProfessionTree.ContainsKey(professionName))
            {
                GD.PrintErr($"Profession {professionName} not found in tree");
                return false;
            }

            var professionNode = ProfessionTree[professionName];
            var playerController = GetNode<PlayerController>("..");
            var playerStats = GetNode<PlayerStats>("../PlayerStats");
            var professionSkillManager = GetNode<Skills.SkillManager>("../SkillManager");

            if (playerController == null || playerStats == null || professionSkillManager == null)
                return false;

            // Remove old profession bonuses if changing professions
            if (!string.IsNullOrEmpty(CurrentProfession))
            {
                RemoveProfessionEffects(CurrentProfession, playerStats, professionSkillManager);
            }

            // Apply new profession effects
            ApplyProfessionEffects(professionNode.ProfessionData, playerStats, professionSkillManager);

            // Update current profession
            string previousProfession = CurrentProfession;
            CurrentProfession = professionName;
            playerController.CurrentProfession = professionName;

            // Reset profession level and experience when changing professions
            ProfessionLevel = 1;
            ProfessionExperience = 0;

            // Check for newly available professions
            CheckAvailableProfessions();

            EmitSignal(SignalName.ProfessionChanged, professionName);
            GD.Print($"Changed profession from {previousProfession ?? "None"} to {professionName}!");

            return true;
        }

        private void ApplyProfessionEffects(ProfessionPath professionData, PlayerStats playerStats, Skills.SkillManager skillManager)
        {
            // Apply stat bonuses scaled by profession level
            if (professionData.StatBonuses != null)
            {
                foreach (var bonus in professionData.StatBonuses)
                {
                    int scaledBonus = bonus.Value * ProfessionLevel;
                    playerStats.ApplyStatBonus(bonus.Key, scaledBonus);
                    GD.Print($"Profession bonus: +{scaledBonus} {bonus.Key}");
                }
            }

            // Unlock profession-specific skills
            if (professionData.UnlockedSkills != null)
            {
                foreach (string skillName in professionData.UnlockedSkills)
                {
                    skillManager.UnlockSkill(skillName);
                    GD.Print($"Profession unlocked skill: {skillName}");
                }
            }

            // Recalculate derived stats
            playerStats.CalculateDerivedStats();
        }

        private void RemoveProfessionEffects(string professionName, PlayerStats playerStats, Skills.SkillManager skillManager)
        {
            if (!ProfessionTree.ContainsKey(professionName)) return;

            var professionData = ProfessionTree[professionName].ProfessionData;

            // Remove stat bonuses (by applying negative bonuses)
            if (professionData.StatBonuses != null)
            {
                foreach (var bonus in professionData.StatBonuses)
                {
                    int scaledBonus = bonus.Value * ProfessionLevel;
                    playerStats.ApplyStatBonus(bonus.Key, -scaledBonus);
                }
            }

            // Note: We don't remove skills as they represent learned knowledge
            // that persists even when changing professions

            playerStats.CalculateDerivedStats();
        }

        public void GainProfessionExperience(int amount)
        {
            if (string.IsNullOrEmpty(CurrentProfession)) return;

            ProfessionExperience += amount;
            int experienceNeeded = CalculateExperienceForNext();

            while (ProfessionExperience >= experienceNeeded)
            {
                LevelUpProfession();
                experienceNeeded = CalculateExperienceForNext();
            }
        }

        private void LevelUpProfession()
        {
            ProfessionLevel++;
            ProfessionExperience = 0;

            // Apply additional profession bonuses for the new level
            if (!string.IsNullOrEmpty(CurrentProfession) && ProfessionTree.ContainsKey(CurrentProfession))
            {
                var professionData = ProfessionTree[CurrentProfession].ProfessionData;
                var playerStats = GetNode<PlayerStats>("../PlayerStats");
                
                if (playerStats != null && professionData.StatBonuses != null)
                {
                    foreach (var bonus in professionData.StatBonuses)
                    {
                        // Apply one level worth of bonuses
                        playerStats.ApplyStatBonus(bonus.Key, bonus.Value);
                    }
                    playerStats.CalculateDerivedStats();
                }
            }

            // Check for newly available professions
            CheckAvailableProfessions();

            EmitSignal(SignalName.ProfessionLevelUp, ProfessionLevel);
            GD.Print($"Profession level up! {CurrentProfession} is now level {ProfessionLevel}");
        }

        private int CalculateExperienceForNext()
        {
            return 150 + (ProfessionLevel * 75);
        }

        public List<string> GetAvailableProfessions()
        {
            return new List<string>(AvailableProfessions);
        }

        public ProfessionNode GetProfessionNode(string professionName)
        {
            return ProfessionTree.ContainsKey(professionName) ? ProfessionTree[professionName] : null;
        }

        public bool IsFinalProfession(string professionName)
        {
            if (!ProfessionTree.ContainsKey(professionName))
                return false;

            var node = ProfessionTree[professionName];
            return node.ProfessionData?.IsFinalProfession == true || node.Children.Count == 0;
        }

        public string GetProfessionDescription(string professionName)
        {
            if (ProfessionTree.ContainsKey(professionName))
            {
                return ProfessionTree[professionName].ProfessionData?.Description ?? "No description available.";
            }
            return "Profession not found.";
        }

        public List<ProfessionNode> GetProfessionsByCategory()
        {
            // This could be expanded to categorize professions
            return ProfessionTree.Values.ToList();
        }

        public float GetProfessionProgress()
        {
            if (ProfessionLevel == 0) return 0.0f;
            
            int experienceNeeded = CalculateExperienceForNext();
            return (float)ProfessionExperience / experienceNeeded;
        }
    }

    public class ProfessionNode
    {
        public string Name { get; set; }
        public ProfessionPath ProfessionData { get; set; }
        public List<string> Children { get; set; }
        public bool IsUnlocked { get; set; }
        public bool IsActive { get; set; }

        public ProfessionNode()
        {
            Children = new List<string>();
        }
    }
}
