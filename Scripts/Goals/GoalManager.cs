using Godot;
using System.Collections.Generic;
using System.Linq;
using NewWorldEvolution.Data;
using NewWorldEvolution.Core;

namespace NewWorldEvolution.Goals
{
    public partial class GoalManager : Node
    {
        public Dictionary<GoalType, Goal> ActiveGoals { get; private set; }
        public Dictionary<GoalType, Goal> UnlockedGoals { get; private set; }
        public Dictionary<GoalType, Goal> CompletedGoals { get; private set; }
        
        [Export] public int MaxActiveGoals = 3;

        [Signal] public delegate void GoalUnlockedEventHandler(GoalType goalType);
        [Signal] public delegate void GoalCompletedEventHandler(GoalType goalType);
        [Signal] public delegate void GoalProgressUpdatedEventHandler(GoalType goalType, float progress);

        public override void _Ready()
        {
            ActiveGoals = new Dictionary<GoalType, Goal>();
            UnlockedGoals = new Dictionary<GoalType, Goal>();
            CompletedGoals = new Dictionary<GoalType, Goal>();

            InitializeGoals();
        }

        private void InitializeGoals()
        {
            // Check for initially available goals
            CheckGoalUnlocks();
            
            // Set up periodic checking
            var timer = new Timer();
            timer.WaitTime = 5.0; // Check every 5 seconds
            timer.Timeout += CheckGoalUnlocks;
            timer.Autostart = true;
            AddChild(timer);
        }

        public void CheckGoalUnlocks()
        {
            foreach (var goalData in GameManager.Instance.GoalDatabase.Values)
            {
                if (!UnlockedGoals.ContainsKey(goalData.Type) && 
                    !ActiveGoals.ContainsKey(goalData.Type) && 
                    !CompletedGoals.ContainsKey(goalData.Type))
                {
                    if (CanUnlockGoal(goalData))
                    {
                        UnlockGoal(goalData.Type);
                    }
                }
            }

            // Check progress on active goals
            foreach (var activeGoal in ActiveGoals.Values.ToList())
            {
                UpdateGoalProgress(activeGoal);
            }
        }

        private bool CanUnlockGoal(GoalData goalData)
        {
            var playerStats = GetNode<Player.PlayerStats>("../PlayerStats");
            if (playerStats == null) return false;

            foreach (var condition in goalData.UnlockConditions)
            {
                if (!CheckUnlockCondition(condition, playerStats))
                    return false;
            }

            return true;
        }

        private bool CheckUnlockCondition(GoalUnlockCondition condition, Player.PlayerStats playerStats)
        {
            switch (condition.Type)
            {
                case ConditionType.Stat:
                    int statValue = playerStats.GetStatValue(condition.Target);
                    return CompareValues(statValue, condition.Value, condition.Operator);

                case ConditionType.Skill:
                    var skillManager = GetNode<Skills.SkillManager>("../SkillManager");
                    if (skillManager != null)
                    {
                        int skillLevel = skillManager.GetSkillLevel(condition.Target);
                        return CompareValues(skillLevel, condition.Value, condition.Operator);
                    }
                    return false;

                case ConditionType.KillCount:
                    // This would need to be tracked in a separate system
                    // For now, return false as placeholder
                    return false;

                case ConditionType.QuestComplete:
                    // This would need integration with quest system
                    return false;

                case ConditionType.Achievement:
                    // This would need integration with achievement system
                    return false;

                case ConditionType.ItemOwned:
                    // This would need integration with inventory system
                    return false;

                case ConditionType.LocationVisited:
                    // This would need integration with world exploration system
                    return false;

                case ConditionType.NPCRelationship:
                    // This would need integration with NPC relationship system
                    return false;

                default:
                    return false;
            }
        }

        private bool CompareValues(object actual, object expected, string operatorType)
        {
            if (actual is int actualInt && expected is int expectedInt)
            {
                switch (operatorType)
                {
                    case ">=": return actualInt >= expectedInt;
                    case "<=": return actualInt <= expectedInt;
                    case "==": return actualInt == expectedInt;
                    case ">": return actualInt > expectedInt;
                    case "<": return actualInt < expectedInt;
                    case "!=": return actualInt != expectedInt;
                    default: return false;
                }
            }

            // Add support for other types as needed
            return false;
        }

        public void UnlockGoal(GoalType goalType)
        {
            var goalData = GameManager.Instance.GetGoalData(goalType);
            if (goalData == null) return;

            var goal = new Goal(goalData);
            UnlockedGoals[goalType] = goal;

            EmitSignal(SignalName.GoalUnlocked, (int)goalType);
            GD.Print($"Goal unlocked: {goalData.Name}");
        }

        public bool ActivateGoal(GoalType goalType)
        {
            if (!UnlockedGoals.ContainsKey(goalType))
            {
                GD.Print($"Goal {goalType} is not unlocked");
                return false;
            }

            if (ActiveGoals.Count >= MaxActiveGoals)
            {
                GD.Print($"Cannot activate goal - maximum active goals reached ({MaxActiveGoals})");
                return false;
            }

            var goal = UnlockedGoals[goalType];
            UnlockedGoals.Remove(goalType);
            ActiveGoals[goalType] = goal;

            GD.Print($"Goal activated: {goal.Data.Name}");
            return true;
        }

        public void DeactivateGoal(GoalType goalType)
        {
            if (!ActiveGoals.ContainsKey(goalType)) return;

            var goal = ActiveGoals[goalType];
            ActiveGoals.Remove(goalType);
            UnlockedGoals[goalType] = goal;

            GD.Print($"Goal deactivated: {goal.Data.Name}");
        }

        private void UpdateGoalProgress(Goal goal)
        {
            float totalConditions = goal.Data.UnlockConditions.Count;
            if (totalConditions == 0) return;

            float metConditions = 0;
            var playerStats = GetNode<Player.PlayerStats>("../PlayerStats");
            
            if (playerStats != null)
            {
                foreach (var condition in goal.Data.UnlockConditions)
                {
                    if (CheckUnlockCondition(condition, playerStats))
                    {
                        metConditions++;
                    }
                }
            }

            float newProgress = metConditions / totalConditions;
            
            if (newProgress != goal.Progress)
            {
                goal.Progress = newProgress;
                EmitSignal(SignalName.GoalProgressUpdated, (int)goal.Data.Type, newProgress);

                if (newProgress >= 1.0f)
                {
                    CompleteGoal(goal.Data.Type);
                }
            }
        }

        public void CompleteGoal(GoalType goalType)
        {
            Goal goal = null;
            
            if (ActiveGoals.ContainsKey(goalType))
            {
                goal = ActiveGoals[goalType];
                ActiveGoals.Remove(goalType);
            }
            else if (UnlockedGoals.ContainsKey(goalType))
            {
                goal = UnlockedGoals[goalType];
                UnlockedGoals.Remove(goalType);
            }

            if (goal == null) return;

            goal.IsCompleted = true;
            goal.CompletionTime = Time.GetDatetimeStringFromSystem();
            CompletedGoals[goalType] = goal;

            // Apply rewards
            ApplyGoalRewards(goal.Data);

            EmitSignal(SignalName.GoalCompleted, (int)goalType);
            GD.Print($"Goal completed: {goal.Data.Name}!");
        }

        private void ApplyGoalRewards(GoalData goalData)
        {
            var playerStats = GetNode<Player.PlayerStats>("../PlayerStats");
            var skillManager = GetNode<Skills.SkillManager>("../SkillManager");

            foreach (var reward in goalData.Rewards)
            {
                switch (reward.Type.ToLower())
                {
                    case "stat":
                        if (playerStats != null)
                        {
                            playerStats.ApplyStatBonus(reward.Target, (int)reward.Value);
                            GD.Print($"Stat reward: +{reward.Value} {reward.Target}");
                        }
                        break;

                    case "skill":
                        if (skillManager != null)
                        {
                            skillManager.UnlockSkill(reward.Target);
                            GD.Print($"Skill reward: {reward.Target}");
                        }
                        break;

                    case "title":
                        // This would integrate with a title system
                        GD.Print($"Title reward: {reward.Target}");
                        break;

                    case "item":
                        // This would integrate with inventory system
                        GD.Print($"Item reward: {reward.Target}");
                        break;

                    default:
                        GD.Print($"Unknown reward type: {reward.Type}");
                        break;
                }
            }
        }

        public List<Goal> GetActiveGoals()
        {
            return ActiveGoals.Values.ToList();
        }

        public List<Goal> GetUnlockedGoals()
        {
            return UnlockedGoals.Values.ToList();
        }

        public List<Goal> GetCompletedGoals()
        {
            return CompletedGoals.Values.ToList();
        }

        public Goal GetGoal(GoalType goalType)
        {
            if (ActiveGoals.ContainsKey(goalType))
                return ActiveGoals[goalType];
            if (UnlockedGoals.ContainsKey(goalType))
                return UnlockedGoals[goalType];
            if (CompletedGoals.ContainsKey(goalType))
                return CompletedGoals[goalType];

            return null;
        }

        public bool IsGoalActive(GoalType goalType)
        {
            return ActiveGoals.ContainsKey(goalType);
        }

        public bool IsGoalUnlocked(GoalType goalType)
        {
            return UnlockedGoals.ContainsKey(goalType) || ActiveGoals.ContainsKey(goalType);
        }

        public bool IsGoalCompleted(GoalType goalType)
        {
            return CompletedGoals.ContainsKey(goalType);
        }
    }

    public class Goal
    {
        public GoalData Data { get; set; }
        public float Progress { get; set; }
        public bool IsCompleted { get; set; }
        public string UnlockTime { get; set; }
        public string CompletionTime { get; set; }

        public Goal(GoalData data)
        {
            Data = data;
            Progress = 0.0f;
            IsCompleted = false;
            UnlockTime = Time.GetDatetimeStringFromSystem();
        }
    }
}
