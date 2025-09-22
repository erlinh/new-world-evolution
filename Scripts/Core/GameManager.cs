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
        [Export] public string GameWorldPath = "res://Scenes/Main/GameWorld.tscn";

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
                
                // Check if we're in the GameWorld scene and need to initialize the player
                if (GetTree().CurrentScene.Name == "GameWorld" && !string.IsNullOrEmpty(SelectedRace))
                {
                    GD.Print($"Initializing game for {SelectedName} ({SelectedGender} {SelectedRace})");
                    CurrentPlayerRace = SelectedRace;
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
                try
                {
                    using var file = FileAccess.Open(RaceDataPath, FileAccess.ModeFlags.Read);
                    string jsonContent = file.GetAsText();
                    var raceDict = Json.ParseString(jsonContent).AsGodotDictionary();
                    
                    foreach (var kvp in raceDict)
                    {
                        string raceName = kvp.Key.AsString();
                        var raceDataDict = kvp.Value.AsGodotDictionary();
                        var raceData = ParseRaceData(raceDataDict);
                        RaceDatabase[raceName] = raceData;
                    }
                    GD.Print($"Race data loaded successfully: {RaceDatabase.Count} races");
                }
                catch (System.Exception e)
                {
                    GD.PrintErr($"Error loading race data: {e.Message}");
                    CreateDefaultRaceData();
                }
            }
            else
            {
                GD.PrintErr($"Race data file not found: {RaceDataPath}");
                CreateDefaultRaceData();
            }

            // Load skill data
            if (FileAccess.FileExists(SkillDataPath))
            {
                try
                {
                    using var file = FileAccess.Open(SkillDataPath, FileAccess.ModeFlags.Read);
                    string jsonContent = file.GetAsText();
                    var skillDict = Json.ParseString(jsonContent).AsGodotDictionary();
                    
                    foreach (var kvp in skillDict)
                    {
                        string skillName = kvp.Key.AsString();
                        var skillDataDict = kvp.Value.AsGodotDictionary();
                        var skillData = ParseSkillData(skillDataDict);
                        SkillDatabase[skillName] = skillData;
                    }
                    GD.Print($"Skill data loaded successfully: {SkillDatabase.Count} skills");
                }
                catch (System.Exception e)
                {
                    GD.PrintErr($"Error loading skill data: {e.Message}");
                    CreateDefaultSkillData();
                }
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

        private RaceData ParseRaceData(Godot.Collections.Dictionary raceDataDict)
        {
            var raceData = new RaceData
            {
                Name = raceDataDict["Name"].AsString(),
                Description = raceDataDict["Description"].AsString(),
                CanEvolve = raceDataDict["CanEvolve"].AsBool(),
                BaseStats = new Dictionary<string, int>(),
                StartingSkills = new List<string>(),
                SpawnLocations = new List<string>(),
                EvolutionPaths = new Dictionary<string, EvolutionPath>(),
                ProfessionPaths = new Dictionary<string, ProfessionPath>()
            };

            // Parse base stats
            var baseStatsDict = raceDataDict["BaseStats"].AsGodotDictionary();
            foreach (var kvp in baseStatsDict)
            {
                raceData.BaseStats[kvp.Key.AsString()] = kvp.Value.AsInt32();
            }

            // Parse starting skills
            var startingSkillsArray = raceDataDict["StartingSkills"].AsGodotArray();
            foreach (var skill in startingSkillsArray)
            {
                raceData.StartingSkills.Add(skill.AsString());
            }

            // Parse spawn locations
            var spawnLocationsArray = raceDataDict["SpawnLocations"].AsGodotArray();
            foreach (var spawn in spawnLocationsArray)
            {
                raceData.SpawnLocations.Add(spawn.AsString());
            }

            // Parse evolution paths (if any)
            if (raceDataDict.TryGetValue("EvolutionPaths", out var evolutionPathsVar))
            {
                var evolutionPathsDict = evolutionPathsVar.AsGodotDictionary();
                foreach (var kvp in evolutionPathsDict)
                {
                    string evolutionName = kvp.Key.AsString();
                    var evolutionDict = kvp.Value.AsGodotDictionary();
                    raceData.EvolutionPaths[evolutionName] = ParseEvolutionPath(evolutionDict);
                }
            }

            // Parse profession paths (if any)
            if (raceDataDict.TryGetValue("ProfessionPaths", out var professionPathsVar))
            {
                var professionPathsDict = professionPathsVar.AsGodotDictionary();
                foreach (var kvp in professionPathsDict)
                {
                    string professionName = kvp.Key.AsString();
                    var professionDict = kvp.Value.AsGodotDictionary();
                    raceData.ProfessionPaths[professionName] = ParseProfessionPath(professionDict);
                }
            }

            return raceData;
        }

        private EvolutionPath ParseEvolutionPath(Godot.Collections.Dictionary evolutionDict)
        {
            var evolution = new EvolutionPath
            {
                Name = evolutionDict["Name"].AsString(),
                Description = evolutionDict["Description"].AsString(),
                Requirements = new Dictionary<string, object>(),
                NextEvolutions = new List<string>(),
                StatBonuses = new Dictionary<string, int>(),
                UnlockedSkills = new List<string>(),
                IsFinalEvolution = evolutionDict.GetValueOrDefault("IsFinalEvolution", false).AsBool()
            };

            // Parse requirements
            if (evolutionDict.TryGetValue("Requirements", out var requirementsVar))
            {
                var requirementsDict = requirementsVar.AsGodotDictionary();
                foreach (var kvp in requirementsDict)
                {
                    evolution.Requirements[kvp.Key.AsString()] = kvp.Value.AsInt32();
                }
            }

            // Parse next evolutions
            if (evolutionDict.TryGetValue("NextEvolutions", out var nextEvolutionsVar))
            {
                var nextEvolutionsArray = nextEvolutionsVar.AsGodotArray();
                foreach (var nextEvolution in nextEvolutionsArray)
                {
                    evolution.NextEvolutions.Add(nextEvolution.AsString());
                }
            }

            // Parse stat bonuses
            if (evolutionDict.TryGetValue("StatBonuses", out var statBonusesVar))
            {
                var statBonusesDict = statBonusesVar.AsGodotDictionary();
                foreach (var kvp in statBonusesDict)
                {
                    evolution.StatBonuses[kvp.Key.AsString()] = kvp.Value.AsInt32();
                }
            }

            // Parse unlocked skills
            if (evolutionDict.TryGetValue("UnlockedSkills", out var unlockedSkillsVar))
            {
                var unlockedSkillsArray = unlockedSkillsVar.AsGodotArray();
                foreach (var skill in unlockedSkillsArray)
                {
                    evolution.UnlockedSkills.Add(skill.AsString());
                }
            }

            return evolution;
        }

        private ProfessionPath ParseProfessionPath(Godot.Collections.Dictionary professionDict)
        {
            var profession = new ProfessionPath
            {
                Name = professionDict["Name"].AsString(),
                Description = professionDict["Description"].AsString(),
                Requirements = new Dictionary<string, object>(),
                NextProfessions = new List<string>(),
                StatBonuses = new Dictionary<string, int>(),
                UnlockedSkills = new List<string>(),
                IsFinalProfession = professionDict.GetValueOrDefault("IsFinalProfession", false).AsBool()
            };

            // Parse requirements
            if (professionDict.TryGetValue("Requirements", out var requirementsVar))
            {
                var requirementsDict = requirementsVar.AsGodotDictionary();
                foreach (var kvp in requirementsDict)
                {
                    // Handle mixed requirement types (int and string)
                    var value = kvp.Value;
                    if (value.VariantType == Variant.Type.Int)
                    {
                        profession.Requirements[kvp.Key.AsString()] = value.AsInt32();
                    }
                    else
                    {
                        profession.Requirements[kvp.Key.AsString()] = value.AsString();
                    }
                }
            }

            // Parse next professions
            if (professionDict.TryGetValue("NextProfessions", out var nextProfessionsVar))
            {
                var nextProfessionsArray = nextProfessionsVar.AsGodotArray();
                foreach (var nextProfession in nextProfessionsArray)
                {
                    profession.NextProfessions.Add(nextProfession.AsString());
                }
            }

            // Parse stat bonuses
            if (professionDict.TryGetValue("StatBonuses", out var statBonusesVar))
            {
                var statBonusesDict = statBonusesVar.AsGodotDictionary();
                foreach (var kvp in statBonusesDict)
                {
                    profession.StatBonuses[kvp.Key.AsString()] = kvp.Value.AsInt32();
                }
            }

            // Parse unlocked skills
            if (professionDict.TryGetValue("UnlockedSkills", out var unlockedSkillsVar))
            {
                var unlockedSkillsArray = unlockedSkillsVar.AsGodotArray();
                foreach (var skill in unlockedSkillsArray)
                {
                    profession.UnlockedSkills.Add(skill.AsString());
                }
            }

            return profession;
        }

        private SkillData ParseSkillData(Godot.Collections.Dictionary skillDataDict)
        {
            var skillData = new SkillData
            {
                Name = skillDataDict["Name"].AsString(),
                Description = skillDataDict["Description"].AsString(),
                Type = System.Enum.Parse<SkillType>(skillDataDict["Type"].AsString()),
                Category = System.Enum.Parse<SkillCategory>(skillDataDict["Category"].AsString()),
                MaxLevel = skillDataDict["MaxLevel"].AsInt32(),
                Requirements = new Dictionary<string, object>(),
                Prerequisites = new List<string>(),
                LevelData = new Dictionary<int, SkillLevelData>(),
                IsUnique = skillDataDict.GetValueOrDefault("IsUnique", false).AsBool(),
                RestrictedToRaces = new List<string>(),
                RestrictedToProfessions = new List<string>()
            };

            // Parse requirements
            if (skillDataDict.TryGetValue("Requirements", out var requirementsVar))
            {
                var requirementsDict = requirementsVar.AsGodotDictionary();
                foreach (var kvp in requirementsDict)
                {
                    skillData.Requirements[kvp.Key.AsString()] = kvp.Value.AsInt32();
                }
            }

            // Parse prerequisites
            if (skillDataDict.TryGetValue("Prerequisites", out var prerequisitesVar))
            {
                var prerequisitesArray = prerequisitesVar.AsGodotArray();
                foreach (var prerequisite in prerequisitesArray)
                {
                    skillData.Prerequisites.Add(prerequisite.AsString());
                }
            }

            // Parse level data
            if (skillDataDict.TryGetValue("LevelData", out var levelDataVar))
            {
                var levelDataDict = levelDataVar.AsGodotDictionary();
                foreach (var kvp in levelDataDict)
                {
                    int level = kvp.Key.AsInt32();
                    var levelDict = kvp.Value.AsGodotDictionary();
                    skillData.LevelData[level] = ParseSkillLevelData(levelDict);
                }
            }

            // Parse restricted races
            if (skillDataDict.TryGetValue("RestrictedToRaces", out var restrictedRacesVar))
            {
                var restrictedRacesArray = restrictedRacesVar.AsGodotArray();
                foreach (var race in restrictedRacesArray)
                {
                    skillData.RestrictedToRaces.Add(race.AsString());
                }
            }

            // Parse restricted professions
            if (skillDataDict.TryGetValue("RestrictedToProfessions", out var restrictedProfessionsVar))
            {
                var restrictedProfessionsArray = restrictedProfessionsVar.AsGodotArray();
                foreach (var profession in restrictedProfessionsArray)
                {
                    skillData.RestrictedToProfessions.Add(profession.AsString());
                }
            }

            return skillData;
        }

        private SkillLevelData ParseSkillLevelData(Godot.Collections.Dictionary levelDict)
        {
            var levelData = new SkillLevelData
            {
                Level = levelDict["Level"].AsInt32(),
                Description = levelDict["Description"].AsString(),
                ManaCost = levelDict["ManaCost"].AsInt32(),
                Cooldown = levelDict["Cooldown"].AsInt32(),
                EffectValue = levelDict["EffectValue"].AsSingle(),
                StatBonuses = new Dictionary<string, float>()
            };

            // Parse stat bonuses
            if (levelDict.TryGetValue("StatBonuses", out var statBonusesVar))
            {
                var statBonusesDict = statBonusesVar.AsGodotDictionary();
                foreach (var kvp in statBonusesDict)
                {
                    levelData.StatBonuses[kvp.Key.AsString()] = kvp.Value.AsSingle();
                }
            }

            return levelData;
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
                    
                    GD.Print($"Selected spawn location for {selectedRace}: {CurrentSpawnLocation}");
                    LoadSpawnLocation();
                }
            }
        }

        private void LoadSpawnLocation()
        {
            if (SpawnDatabase.ContainsKey(CurrentSpawnLocation))
            {
                var spawnData = SpawnDatabase[CurrentSpawnLocation];
                GD.Print($"Loading spawn location: {CurrentSpawnLocation}");
                // Always load the main GameWorld scene, not individual spawn scenes
                GetTree().ChangeSceneToFile(GameWorldPath);
            }
        }

        public RaceData GetRaceData(string raceName)
        {
            if (string.IsNullOrEmpty(raceName) || RaceDatabase == null)
                return null;
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
