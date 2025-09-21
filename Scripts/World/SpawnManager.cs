using Godot;
using System.Collections.Generic;
using System.Linq;
using NewWorldEvolution.Data;
using NewWorldEvolution.Core;

namespace NewWorldEvolution.World
{
    public partial class SpawnManager : Node
    {
        [Export] public Vector2 WorldSize = new Vector2(2000, 2000);
        [Export] public float MinSpawnDistance = 100.0f;

        public Dictionary<string, SpawnLocationData> SpawnLocations { get; private set; }
        public string CurrentSpawnLocation { get; private set; }

        public override void _Ready()
        {
            SpawnLocations = new Dictionary<string, SpawnLocationData>();
            InitializeSpawnLocations();
        }

        private void InitializeSpawnLocations()
        {
            SpawnLocations = GameManager.Instance.SpawnDatabase;
            
            if (SpawnLocations.Count == 0)
            {
                CreateDefaultSpawnLocations();
            }
        }

        private void CreateDefaultSpawnLocations()
        {
            // Human spawn locations
            CreateSpawnLocation("HumanVillage", "Peaceful Village", new Vector2(0, 0), 
                new List<string> { "Human" }, "res://Scenes/Environments/Spawns/HumanVillage.tscn");
            
            CreateSpawnLocation("TradingPost", "Bustling Trading Post", new Vector2(200, -50), 
                new List<string> { "Human" }, "res://Scenes/Environments/Spawns/TradingPost.tscn");

            // Goblin spawn locations
            CreateSpawnLocation("GoblinCave", "Dark Underground Cave", new Vector2(-150, 100), 
                new List<string> { "Goblin" }, "res://Scenes/Environments/Spawns/GoblinCave.tscn");
            
            CreateSpawnLocation("ForestClearing", "Hidden Forest Clearing", new Vector2(-80, -120), 
                new List<string> { "Goblin" }, "res://Scenes/Environments/Spawns/ForestClearing.tscn");

            // Spider spawn locations
            CreateSpawnLocation("SpiderNest", "Ancient Web-covered Ruins", new Vector2(150, 150), 
                new List<string> { "Spider" }, "res://Scenes/Environments/Spawns/SpiderNest.tscn");
            
            CreateSpawnLocation("DarkForest", "Shadowy Forest Depths", new Vector2(-200, 0), 
                new List<string> { "Spider" }, "res://Scenes/Environments/Spawns/DarkForest.tscn");

            // Demon spawn locations
            CreateSpawnLocation("DemonRift", "Fiery Dimensional Rift", new Vector2(100, -200), 
                new List<string> { "Demon" }, "res://Scenes/Environments/Spawns/DemonRift.tscn");
            
            CreateSpawnLocation("CorruptedLands", "Twisted Corrupted Wasteland", new Vector2(-100, 200), 
                new List<string> { "Demon" }, "res://Scenes/Environments/Spawns/CorruptedLands.tscn");

            // Vampire spawn locations
            CreateSpawnLocation("VampireCastle", "Gothic Ancient Castle", new Vector2(250, 50), 
                new List<string> { "Vampire" }, "res://Scenes/Environments/Spawns/VampireCastle.tscn");
            
            CreateSpawnLocation("Crypts", "Underground Burial Chambers", new Vector2(-250, -100), 
                new List<string> { "Vampire" }, "res://Scenes/Environments/Spawns/Crypts.tscn");
        }

        private void CreateSpawnLocation(string name, string description, Vector2 position, 
            List<string> allowedRaces, string scenePath)
        {
            var spawnData = new SpawnLocationData
            {
                Name = name,
                Description = description,
                Position = position,
                AllowedRaces = allowedRaces,
                ScenePath = scenePath,
                SpawnProperties = new Dictionary<string, object>(),
                NearbyNPCs = new List<string>(),
                AvailableQuests = new List<string>()
            };

            SpawnLocations[name] = spawnData;
        }

        public string SelectRandomSpawnForRace(string race)
        {
            var availableSpawns = SpawnLocations.Values
                .Where(spawn => spawn.AllowedRaces.Contains(race))
                .ToList();

            if (availableSpawns.Count == 0)
            {
                GD.PrintErr($"No spawn locations found for race: {race}");
                return null;
            }

            var random = new System.Random();
            var selectedSpawn = availableSpawns[random.Next(availableSpawns.Count)];
            
            CurrentSpawnLocation = selectedSpawn.Name;
            return selectedSpawn.Name;
        }

        public Vector2 GetSpawnPosition(string spawnLocationName)
        {
            if (SpawnLocations.ContainsKey(spawnLocationName))
            {
                return SpawnLocations[spawnLocationName].Position;
            }

            GD.PrintErr($"Spawn location not found: {spawnLocationName}");
            return Vector2.Zero;
        }

        public SpawnLocationData GetSpawnData(string spawnLocationName)
        {
            return SpawnLocations.ContainsKey(spawnLocationName) ? SpawnLocations[spawnLocationName] : null;
        }

        public List<SpawnLocationData> GetSpawnsForRace(string race)
        {
            return SpawnLocations.Values
                .Where(spawn => spawn.AllowedRaces.Contains(race))
                .ToList();
        }

        public List<SpawnLocationData> GetNearbySpawns(Vector2 position, float radius)
        {
            return SpawnLocations.Values
                .Where(spawn => spawn.Position.DistanceTo(position) <= radius)
                .ToList();
        }

        public bool IsValidSpawnForRace(string spawnLocationName, string race)
        {
            if (!SpawnLocations.ContainsKey(spawnLocationName))
                return false;

            return SpawnLocations[spawnLocationName].AllowedRaces.Contains(race);
        }

        public void LoadSpawnLocation(string spawnLocationName)
        {
            if (!SpawnLocations.ContainsKey(spawnLocationName))
            {
                GD.PrintErr($"Cannot load spawn location: {spawnLocationName} not found");
                return;
            }

            var spawnData = SpawnLocations[spawnLocationName];
            CurrentSpawnLocation = spawnLocationName;

            // Load the scene
            if (!string.IsNullOrEmpty(spawnData.ScenePath))
            {
                var scene = GD.Load<PackedScene>(spawnData.ScenePath);
                if (scene != null)
                {
                    GetTree().ChangeSceneToPacked(scene);
                }
                else
                {
                    GD.PrintErr($"Failed to load scene: {spawnData.ScenePath}");
                }
            }
        }

        public Dictionary<string, object> GetSpawnProperties(string spawnLocationName)
        {
            if (SpawnLocations.ContainsKey(spawnLocationName))
            {
                return SpawnLocations[spawnLocationName].SpawnProperties;
            }
            return new Dictionary<string, object>();
        }

        public void SetSpawnProperty(string spawnLocationName, string propertyName, object value)
        {
            if (SpawnLocations.ContainsKey(spawnLocationName))
            {
                SpawnLocations[spawnLocationName].SpawnProperties[propertyName] = value;
            }
        }

        public List<string> GetNearbyNPCs(string spawnLocationName)
        {
            if (SpawnLocations.ContainsKey(spawnLocationName))
            {
                return SpawnLocations[spawnLocationName].NearbyNPCs;
            }
            return new List<string>();
        }

        public List<string> GetAvailableQuests(string spawnLocationName)
        {
            if (SpawnLocations.ContainsKey(spawnLocationName))
            {
                return SpawnLocations[spawnLocationName].AvailableQuests;
            }
            return new List<string>();
        }

        public void AddNPCToSpawn(string spawnLocationName, string npcName)
        {
            if (SpawnLocations.ContainsKey(spawnLocationName))
            {
                var spawn = SpawnLocations[spawnLocationName];
                if (!spawn.NearbyNPCs.Contains(npcName))
                {
                    spawn.NearbyNPCs.Add(npcName);
                }
            }
        }

        public void AddQuestToSpawn(string spawnLocationName, string questName)
        {
            if (SpawnLocations.ContainsKey(spawnLocationName))
            {
                var spawn = SpawnLocations[spawnLocationName];
                if (!spawn.AvailableQuests.Contains(questName))
                {
                    spawn.AvailableQuests.Add(questName);
                }
            }
        }

        public Vector2 GetRandomPositionNearSpawn(string spawnLocationName, float radius = 50.0f)
        {
            if (!SpawnLocations.ContainsKey(spawnLocationName))
                return Vector2.Zero;

            var spawnPosition = SpawnLocations[spawnLocationName].Position;
            var random = new System.Random();
            
            float angle = (float)(random.NextDouble() * 2 * Mathf.Pi);
            float distance = (float)(random.NextDouble() * radius);
            
            Vector2 offset = new Vector2(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance
            );

            return spawnPosition + offset;
        }

        public string GetCurrentSpawnLocation()
        {
            return CurrentSpawnLocation;
        }

        public List<string> GetAllSpawnLocationNames()
        {
            return SpawnLocations.Keys.ToList();
        }

        public void UpdateSpawnLocation(string spawnLocationName, SpawnLocationData newData)
        {
            if (SpawnLocations.ContainsKey(spawnLocationName))
            {
                SpawnLocations[spawnLocationName] = newData;
            }
        }
    }
}
