using Godot;
using System.Collections.Generic;

namespace NewWorldEvolution.Data
{
    [System.Serializable]
    public class NPCData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Race { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public Dictionary<string, int> Stats { get; set; }
        public string Profession { get; set; }
        public string Settlement { get; set; }
        public string RelationshipStatus { get; set; }
        public string SpouseId { get; set; }
        public List<string> ParentIds { get; set; }
        public List<string> ChildrenIds { get; set; }
        public List<string> PersonalityTraits { get; set; }
        public string EvolutionForm { get; set; }
        public bool IsAlive { get; set; }
        public int BirthDay { get; set; }
        public int BirthYear { get; set; }
        public int DeathDay { get; set; }
        public int DeathYear { get; set; }
        public string DeathCause { get; set; }
        public Vector2 Position { get; set; }
        public Dictionary<string, object> CustomData { get; set; }

        public NPCData()
        {
            Stats = new Dictionary<string, int>();
            ParentIds = new List<string>();
            ChildrenIds = new List<string>();
            PersonalityTraits = new List<string>();
            CustomData = new Dictionary<string, object>();
        }

        public int GetLifespan()
        {
            if (IsAlive)
                return Age;
            else
                return (DeathYear - BirthYear) * 100 + (DeathDay - BirthDay);
        }

        public bool HasTrait(string trait)
        {
            return PersonalityTraits.Contains(trait);
        }

        public int GetStat(string statName)
        {
            return Stats.ContainsKey(statName) ? Stats[statName] : 0;
        }

        public void SetStat(string statName, int value)
        {
            Stats[statName] = value;
        }

        public void ModifyStat(string statName, int modifier)
        {
            if (Stats.ContainsKey(statName))
                Stats[statName] += modifier;
            else
                Stats[statName] = modifier;
        }
    }


    [System.Serializable]
    public class SettlementData
    {
        public string Name { get; set; }
        public Vector2 Position { get; set; }
        public string DominantRace { get; set; }
        public int Population { get; set; }
        public int Prosperity { get; set; }
        public int Defense { get; set; }
        public List<string> NPCIds { get; set; }
        public List<BuildingData> Buildings { get; set; }
        public List<string> TradeRoutes { get; set; }
        public Dictionary<string, int> Resources { get; set; }
        public List<string> Allies { get; set; }
        public List<string> Enemies { get; set; }

        public SettlementData()
        {
            NPCIds = new List<string>();
            Buildings = new List<BuildingData>();
            TradeRoutes = new List<string>();
            Resources = new Dictionary<string, int>();
            Allies = new List<string>();
            Enemies = new List<string>();
        }
    }

    [System.Serializable]
    public class BuildingData
    {
        public string Type { get; set; }
        public int Level { get; set; }
        public string Function { get; set; }
        public string OwnerId { get; set; }
        public Dictionary<string, object> Properties { get; set; }

        public BuildingData()
        {
            Properties = new Dictionary<string, object>();
        }
    }

    [System.Serializable]
    public class WorldEvent
    {
        public string Description { get; set; }
        public int Day { get; set; }
        public int Year { get; set; }
        public string Timestamp { get; set; }
        public string EventType { get; set; }
        public Dictionary<string, object> EventData { get; set; }

        public WorldEvent()
        {
            EventData = new Dictionary<string, object>();
        }
    }
}
