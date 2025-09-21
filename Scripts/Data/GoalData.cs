using Godot;
using System.Collections.Generic;

namespace NewWorldEvolution.Data
{
    public enum GoalType
    {
        DemonLord,
        Hero,
        King,
        Queen,
        God,
        MasterCraftsman,
        ShadowMaster,
        ArcaneScholar
    }

    public enum ConditionType
    {
        Stat,
        Skill,
        KillCount,
        QuestComplete,
        Achievement,
        ItemOwned,
        LocationVisited,
        NPCRelationship
    }

    [System.Serializable]
    public class GoalData
    {
        public GoalType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<GoalUnlockCondition> UnlockConditions { get; set; }
        public List<GoalReward> Rewards { get; set; }
        public bool IsUnlocked { get; set; }
        public bool IsCompleted { get; set; }
        public int Priority { get; set; }

        public GoalData()
        {
            UnlockConditions = new List<GoalUnlockCondition>();
            Rewards = new List<GoalReward>();
        }
    }

    [System.Serializable]
    public class GoalUnlockCondition
    {
        public ConditionType Type { get; set; }
        public string Target { get; set; }
        public object Value { get; set; }
        public string Operator { get; set; } // ">=", "==", "<=", etc.
    }

    [System.Serializable]
    public class GoalReward
    {
        public string Type { get; set; } // "skill", "stat", "title", "item"
        public string Target { get; set; }
        public object Value { get; set; }
    }
}
