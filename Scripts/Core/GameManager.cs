using Godot;
using System.Collections.Generic;
using NewWorldEvolution.Data;

namespace NewWorldEvolution.Core
{
    public partial class GameManager : Node
    {
        public static GameManager Instance { get; private set; }
        public static string SelectedRace { get; set; } = "Human";
        public static string SelectedGender { get; set; } = "Male";
        public static string SelectedName { get; set; } = "";

        [Export] public string RaceDataPath = "res://Data/Json/races.json";
        [Export] public string SkillDataPath = "res://Data/Json/skills.json";
        [Export] public string GoalDataPath = "res://Data/Json/goals.json";
        [Export] public string SpawnDataPath = "res://Data/Json/spawns.json";

        public Dictionary<string, RaceData> RaceDatabase { get; private set; }
        public Dictionary<string, SkillData> SkillDatabase { get; private set; }
        public Dictionary<GoalType, GoalData> GoalDatabase { get; private set; }
        public Dictionary<string, SpawnLocationData> SpawnDatabase { get; private set; }

        public Player.PlayerController CurrentPlayer { get; set; }
        public string CurrentPlayerRace { get; set; }
        public string CurrentSpawnLocation { get; set; }

        public override void _Ready()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeDatabases();
                
                // Auto-start game with selected race if one is set
                if (!string.IsNullOrEmpty(SelectedRace))
                {
                    StartNewGame(SelectedRace);
                }
            }
            else
            {
                QueueFree();
            }
        }

        private void InitializeDatabases()
        {
            RaceDatabase = new Dictionary<string, RaceData>();
            SkillDatabase = new Dictionary<string, SkillData>();
            GoalDatabase = new Dictionary<GoalType, GoalData>();
            SpawnDatabase = new Dictionary<string, SpawnLocationData>();

            LoadGameData();
        }

        private void LoadGameData()
        {
            // Load race data
            if (FileAccess.FileExists(RaceDataPath))
            {
                using var file = FileAccess.Open(RaceDataPath, FileAccess.ModeFlags.Read);
                string jsonContent = file.GetAsText();
                // Parse JSON and populate RaceDatabase
                GD.Print("Race data loaded successfully");
            }
            else
            {
                GD.PrintErr($"Race data file not found: {RaceDataPath}");
                CreateDefaultRaceData();
            }

            // Load skill data
            if (FileAccess.FileExists(SkillDataPath))
            {
                using var file = FileAccess.Open(SkillDataPath, FileAccess.ModeFlags.Read);
                string jsonContent = file.GetAsText();
                // Parse JSON and populate SkillDatabase
                GD.Print("Skill data loaded successfully");
            }
            else
            {
                GD.PrintErr($"Skill data file not found: {SkillDataPath}");
                CreateDefaultSkillData();
            }

            // Load goal data
            if (FileAccess.FileExists(GoalDataPath))
            {
                using var file = FileAccess.Open(GoalDataPath, FileAccess.ModeFlags.Read);
                string jsonContent = file.GetAsText();
                // Parse JSON and populate GoalDatabase
                GD.Print("Goal data loaded successfully");
            }
            else
            {
                GD.PrintErr($"Goal data file not found: {GoalDataPath}");
                CreateDefaultGoalData();
            }

            // Load spawn data
            if (FileAccess.FileExists(SpawnDataPath))
            {
                using var file = FileAccess.Open(SpawnDataPath, FileAccess.ModeFlags.Read);
                string jsonContent = file.GetAsText();
                // Parse JSON and populate SpawnDatabase
                GD.Print("Spawn data loaded successfully");
            }
            else
            {
                GD.PrintErr($"Spawn data file not found: {SpawnDataPath}");
                CreateDefaultSpawnData();
            }
        }

        private void CreateDefaultRaceData()
        {
            // Create default race data for testing
            var humanRace = new RaceData
            {
                Name = "Human",
                Description = "Versatile beings who excel through professions rather than evolution.",
                CanEvolve = false,
                BaseStats = new Dictionary<string, int>
                {
                    {"Strength", 10}, {"Intelligence", 10}, {"Dexterity", 10}, 
                    {"Constitution", 10}, {"Wisdom", 10}, {"Charisma", 10}
                },
                StartingSkills = new List<string> {"BasicSwordplay", "BasicMagic"},
                SpawnLocations = new List<string> {"HumanVillage", "TradingPost"}
            };

            var goblinRace = new RaceData
            {
                Name = "Goblin",
                Description = "Small but cunning creatures that evolve into powerful forms.",
                CanEvolve = true,
                BaseStats = new Dictionary<string, int>
                {
                    {"Strength", 8}, {"Intelligence", 12}, {"Dexterity", 14}, 
                    {"Constitution", 8}, {"Wisdom", 10}, {"Charisma", 6}
                },
                StartingSkills = new List<string> {"Stealth", "BasicCrafting"},
                SpawnLocations = new List<string> {"GoblinCave", "ForestClearing"}
            };

            RaceDatabase["Human"] = humanRace;
            RaceDatabase["Goblin"] = goblinRace;
        }

        private void CreateDefaultSkillData()
        {
            // Create default skill data for testing
            var basicSwordplay = new SkillData
            {
                Name = "Basic Swordplay",
                Description = "Fundamental sword combat techniques.",
                Type = SkillType.Active,
                Category = SkillCategory.Combat,
                MaxLevel = 5
            };

            var stealth = new SkillData
            {
                Name = "Stealth",
                Description = "Move unseen and unheard.",
                Type = SkillType.Passive,
                Category = SkillCategory.Stealth,
                MaxLevel = 10
            };

            SkillDatabase["BasicSwordplay"] = basicSwordplay;
            SkillDatabase["Stealth"] = stealth;
        }

        private void CreateDefaultGoalData()
        {
            // Create default goal data for testing
            var demonLordGoal = new GoalData
            {
                Type = GoalType.DemonLord,
                Name = "Demon Lord",
                Description = "Rule over demons and spread darkness across the land.",
                Priority = 1
            };

            var heroGoal = new GoalData
            {
                Type = GoalType.Hero,
                Name = "Hero",
                Description = "Become a legendary hero who saves the innocent.",
                Priority = 1
            };

            GoalDatabase[GoalType.DemonLord] = demonLordGoal;
            GoalDatabase[GoalType.Hero] = heroGoal;
        }

        private void CreateDefaultSpawnData()
        {
            // Create default spawn data for testing
            var humanVillage = new SpawnLocationData
            {
                Name = "Human Village",
                Description = "A peaceful village where humans live and trade.",
                Position = new Vector2(100, 100),
                AllowedRaces = new List<string> {"Human"},
                ScenePath = "res://Scenes/Environments/Spawns/HumanVillage.tscn"
            };

            var goblinCave = new SpawnLocationData
            {
                Name = "Goblin Cave",
                Description = "A dark cave system where goblins make their home.",
                Position = new Vector2(-50, 150),
                AllowedRaces = new List<string> {"Goblin"},
                ScenePath = "res://Scenes/Environments/Spawns/GoblinCave.tscn"
            };

            SpawnDatabase["HumanVillage"] = humanVillage;
            SpawnDatabase["GoblinCave"] = goblinCave;
        }

        public void StartNewGame(string selectedRace)
        {
            CurrentPlayerRace = selectedRace;
            
            if (RaceDatabase.ContainsKey(selectedRace))
            {
                var raceData = RaceDatabase[selectedRace];
                var availableSpawns = raceData.SpawnLocations;
                
                if (availableSpawns.Count > 0)
                {
                    var random = new System.Random();
                    CurrentSpawnLocation = availableSpawns[random.Next(availableSpawns.Count)];
                    
                    LoadSpawnLocation();
                }
            }
        }

        private void LoadSpawnLocation()
        {
            if (SpawnDatabase.ContainsKey(CurrentSpawnLocation))
            {
                var spawnData = SpawnDatabase[CurrentSpawnLocation];
                GetTree().ChangeSceneToFile(spawnData.ScenePath);
            }
        }

        public RaceData GetRaceData(string raceName)
        {
            return RaceDatabase.ContainsKey(raceName) ? RaceDatabase[raceName] : null;
        }

        public SkillData GetSkillData(string skillName)
        {
            return SkillDatabase.ContainsKey(skillName) ? SkillDatabase[skillName] : null;
        }

        public GoalData GetGoalData(GoalType goalType)
        {
            return GoalDatabase.ContainsKey(goalType) ? GoalDatabase[goalType] : null;
        }
    }
}
