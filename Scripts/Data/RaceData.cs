using Godot;
using System.Collections.Generic;

namespace NewWorldEvolution.Data
{
    [System.Serializable]
    public class RaceData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool CanEvolve { get; set; }
        public Dictionary<string, int> BaseStats { get; set; }
        public List<string> StartingSkills { get; set; }
        public List<string> SpawnLocations { get; set; }
        public Dictionary<string, EvolutionPath> EvolutionPaths { get; set; }
        public Dictionary<string, ProfessionPath> ProfessionPaths { get; set; }

        public RaceData()
        {
            BaseStats = new Dictionary<string, int>();
            StartingSkills = new List<string>();
            SpawnLocations = new List<string>();
            EvolutionPaths = new Dictionary<string, EvolutionPath>();
            ProfessionPaths = new Dictionary<string, ProfessionPath>();
        }
    }

    [System.Serializable]
    public class EvolutionPath
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Requirements { get; set; }
        public List<string> NextEvolutions { get; set; }
        public Dictionary<string, int> StatBonuses { get; set; }
        public List<string> UnlockedSkills { get; set; }
        public bool IsFinalEvolution { get; set; }

        public EvolutionPath()
        {
            Requirements = new Dictionary<string, object>();
            NextEvolutions = new List<string>();
            StatBonuses = new Dictionary<string, int>();
            UnlockedSkills = new List<string>();
        }
    }

    [System.Serializable]
    public class ProfessionPath
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Requirements { get; set; }
        public List<string> NextProfessions { get; set; }
        public Dictionary<string, int> StatBonuses { get; set; }
        public List<string> UnlockedSkills { get; set; }
        public bool IsFinalProfession { get; set; }

        public ProfessionPath()
        {
            Requirements = new Dictionary<string, object>();
            NextProfessions = new List<string>();
            StatBonuses = new Dictionary<string, int>();
            UnlockedSkills = new List<string>();
        }
    }
}
