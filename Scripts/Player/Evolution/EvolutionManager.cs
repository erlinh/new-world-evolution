using Godot;
using System.Collections.Generic;
using System.Linq;
using NewWorldEvolution.Data;
using NewWorldEvolution.Core;

namespace NewWorldEvolution.Player.Evolution
{
    public partial class EvolutionManager : Node
    {
        public Dictionary<string, EvolutionNode> EvolutionTree { get; private set; }
        public string CurrentEvolution { get; private set; }
        public List<string> AvailableEvolutions { get; private set; }

        [Signal] public delegate void EvolutionAvailableEventHandler(string evolutionName);
        [Signal] public delegate void EvolutionCompletedEventHandler(string evolutionName);

        public override void _Ready()
        {
            EvolutionTree = new Dictionary<string, EvolutionNode>();
            AvailableEvolutions = new List<string>();
            InitializeEvolutionTree();
        }

        private void InitializeEvolutionTree()
        {
            var playerController = GetNode<PlayerController>("..");
            if (playerController == null) return;

            string race = playerController.CurrentRace;
            if (string.IsNullOrEmpty(race))
            {
                // Try to get race from GameManager's selected race
                race = GameManager.SelectedRace;
            }

            if (string.IsNullOrEmpty(race)) return;

            var raceData = GameManager.Instance?.GetRaceData(race);

            if (raceData?.CanEvolve == true && raceData.EvolutionPaths != null)
            {
                BuildEvolutionTree(raceData);
                CheckAvailableEvolutions();
            }
        }

        private void BuildEvolutionTree(RaceData raceData)
        {
            // Create root node for base race
            var rootNode = new EvolutionNode
            {
                Name = raceData.Name,
                IsRoot = true,
                IsUnlocked = true,
                Children = new List<string>()
            };

            EvolutionTree[raceData.Name] = rootNode;

            // Build tree from evolution paths
            foreach (var evolutionPath in raceData.EvolutionPaths)
            {
                var node = new EvolutionNode
                {
                    Name = evolutionPath.Key,
                    EvolutionData = evolutionPath.Value,
                    Children = evolutionPath.Value.NextEvolutions?.ToList() ?? new List<string>(),
                    IsUnlocked = false
                };

                EvolutionTree[evolutionPath.Key] = node;

                // Add this evolution as child to its prerequisites
                AddToParentNodes(evolutionPath.Key, evolutionPath.Value);
            }

            CurrentEvolution = raceData.Name;
        }

        private void AddToParentNodes(string evolutionName, EvolutionPath evolutionData)
        {
            // Find which evolutions can lead to this one
            foreach (var kvp in EvolutionTree)
            {
                var node = kvp.Value;
                if (node.EvolutionData?.NextEvolutions?.Contains(evolutionName) == true)
                {
                    if (!node.Children.Contains(evolutionName))
                        node.Children.Add(evolutionName);
                }
            }

            // If no parent found, add to root
            if (!EvolutionTree.Values.Any(n => n.Children.Contains(evolutionName)))
            {
                if (!string.IsNullOrEmpty(CurrentEvolution) && EvolutionTree.ContainsKey(CurrentEvolution))
                {
                    EvolutionTree[CurrentEvolution].Children.Add(evolutionName);
                }
            }
        }

        public void CheckAvailableEvolutions()
        {
            var playerController = GetNode<PlayerController>("..");
            var playerStats = GetNode<PlayerStats>("../PlayerStats");
            
            if (playerController == null || playerStats == null) return;

            var previouslyAvailable = new HashSet<string>(AvailableEvolutions);
            AvailableEvolutions.Clear();

            // Check all evolutions that are children of current evolution
            if (!string.IsNullOrEmpty(CurrentEvolution) && EvolutionTree.ContainsKey(CurrentEvolution))
            {
                var currentNode = EvolutionTree[CurrentEvolution];
                
                foreach (string childEvolution in currentNode.Children)
                {
                    if (EvolutionTree.ContainsKey(childEvolution))
                    {
                        var childNode = EvolutionTree[childEvolution];
                        
                        if (CanEvolve(childNode.EvolutionData, playerStats))
                        {
                            AvailableEvolutions.Add(childEvolution);
                            childNode.IsUnlocked = true;

                            // Emit signal if this is newly available
                            if (!previouslyAvailable.Contains(childEvolution))
                            {
                                EmitSignal(SignalName.EvolutionAvailable, childEvolution);
                                GD.Print($"New evolution available: {childEvolution}");
                            }
                        }
                    }
                }
            }
        }

        private bool CanEvolve(EvolutionPath evolutionData, PlayerStats playerStats)
        {
            if (evolutionData?.Requirements == null) return false;

            foreach (var requirement in evolutionData.Requirements)
            {
                if (!CheckEvolutionRequirement(requirement.Key, requirement.Value, playerStats))
                    return false;
            }

            return true;
        }

        private bool CheckEvolutionRequirement(string requirementType, object value, PlayerStats playerStats)
        {
            switch (requirementType.ToLower())
            {
                case "level":
                    return playerStats.Level >= (int)value;
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
                case "corruption":
                    return playerStats.Corruption >= (int)value;
                case "honor":
                    return playerStats.Honor >= (int)value;
                case "skill":
                    var skillManager = GetNode<Skills.SkillManager>("../SkillManager");
                    if (skillManager != null && value is string skillName)
                    {
                        return skillManager.GetSkillLevel(skillName) > 0;
                    }
                    return false;
                case "skill_level":
                    // Format: "SkillName:Level" 
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
                    GD.PrintErr($"Unknown evolution requirement: {requirementType}");
                    return false;
            }
        }

        public bool AttemptEvolution(string evolutionName)
        {
            if (!AvailableEvolutions.Contains(evolutionName))
            {
                GD.Print($"Evolution {evolutionName} is not available");
                return false;
            }

            if (!EvolutionTree.ContainsKey(evolutionName))
            {
                GD.PrintErr($"Evolution {evolutionName} not found in tree");
                return false;
            }

            var evolutionNode = EvolutionTree[evolutionName];
            var playerController = GetNode<PlayerController>("..");
            var playerStats = GetNode<PlayerStats>("../PlayerStats");
            var evolutionSkillManager = GetNode<Skills.SkillManager>("../SkillManager");

            if (playerController == null || playerStats == null || evolutionSkillManager == null)
                return false;

            // Apply evolution effects
            ApplyEvolutionEffects(evolutionNode.EvolutionData, playerStats, evolutionSkillManager);

            // Update current evolution
            string previousEvolution = CurrentEvolution;
            CurrentEvolution = evolutionName;
            playerController.CurrentEvolution = evolutionName;

            // Remove from available evolutions
            AvailableEvolutions.Remove(evolutionName);

            // Check for new evolutions
            CheckAvailableEvolutions();

            EmitSignal(SignalName.EvolutionCompleted, evolutionName);
            GD.Print($"Successfully evolved from {previousEvolution} to {evolutionName}!");

            return true;
        }

        private void ApplyEvolutionEffects(EvolutionPath evolutionData, PlayerStats playerStats, Skills.SkillManager skillManager)
        {
            // Apply stat bonuses
            if (evolutionData.StatBonuses != null)
            {
                foreach (var bonus in evolutionData.StatBonuses)
                {
                    playerStats.ApplyStatBonus(bonus.Key, bonus.Value);
                    GD.Print($"Evolution bonus: +{bonus.Value} {bonus.Key}");
                }
            }

            // Unlock new skills
            if (evolutionData.UnlockedSkills != null)
            {
                foreach (string skillName in evolutionData.UnlockedSkills)
                {
                    skillManager.UnlockSkill(skillName);
                    GD.Print($"Evolution unlocked skill: {skillName}");
                }
            }

            // Recalculate derived stats
            playerStats.CalculateDerivedStats();
        }

        public List<string> GetAvailableEvolutions()
        {
            return new List<string>(AvailableEvolutions);
        }

        public EvolutionNode GetEvolutionNode(string evolutionName)
        {
            return EvolutionTree.ContainsKey(evolutionName) ? EvolutionTree[evolutionName] : null;
        }

        public List<EvolutionNode> GetEvolutionPath()
        {
            var path = new List<EvolutionNode>();
            string current = CurrentEvolution;

            while (!string.IsNullOrEmpty(current) && EvolutionTree.ContainsKey(current))
            {
                path.Insert(0, EvolutionTree[current]);
                
                // Find parent (this is simplified, could be more sophisticated)
                current = FindParentEvolution(current);
            }

            return path;
        }

        private string FindParentEvolution(string evolutionName)
        {
            foreach (var kvp in EvolutionTree)
            {
                if (kvp.Value.Children.Contains(evolutionName))
                    return kvp.Key;
            }
            return null;
        }

        public bool IsFinalEvolution(string evolutionName)
        {
            if (!EvolutionTree.ContainsKey(evolutionName))
                return false;

            var node = EvolutionTree[evolutionName];
            return node.EvolutionData?.IsFinalEvolution == true || node.Children.Count == 0;
        }

        public string GetEvolutionDescription(string evolutionName)
        {
            if (EvolutionTree.ContainsKey(evolutionName))
            {
                return EvolutionTree[evolutionName].EvolutionData?.Description ?? "No description available.";
            }
            return "Evolution not found.";
        }
    }

    public class EvolutionNode
    {
        public string Name { get; set; }
        public EvolutionPath EvolutionData { get; set; }
        public List<string> Children { get; set; }
        public bool IsRoot { get; set; }
        public bool IsUnlocked { get; set; }
        public bool IsCompleted { get; set; }

        public EvolutionNode()
        {
            Children = new List<string>();
        }
    }
}
