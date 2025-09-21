using Godot;
using System.Collections.Generic;

namespace NewWorldEvolution.Data
{
    [System.Serializable]
    public class SpawnLocationData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Vector2 Position { get; set; }
        public List<string> AllowedRaces { get; set; }
        public string ScenePath { get; set; }
        public Dictionary<string, object> SpawnProperties { get; set; }
        public List<string> NearbyNPCs { get; set; }
        public List<string> AvailableQuests { get; set; }

        public SpawnLocationData()
        {
            AllowedRaces = new List<string>();
            SpawnProperties = new Dictionary<string, object>();
            NearbyNPCs = new List<string>();
            AvailableQuests = new List<string>();
        }
    }

    [System.Serializable]
    public class WorldLocationData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Vector2 Position { get; set; }
        public string ScenePath { get; set; }
        public List<string> ConnectedLocations { get; set; }
        public Dictionary<string, float> RaceAffinities { get; set; }
        public bool IsDiscovered { get; set; }

        public WorldLocationData()
        {
            ConnectedLocations = new List<string>();
            RaceAffinities = new Dictionary<string, float>();
        }
    }
}
