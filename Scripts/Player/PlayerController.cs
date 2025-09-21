using Godot;
using System.Collections.Generic;
using NewWorldEvolution.Data;
using NewWorldEvolution.Core;

namespace NewWorldEvolution.Player
{
    public partial class PlayerController : CharacterBody2D
    {
        [Export] public float Speed = 200.0f;
        [Export] public float JumpVelocity = -300.0f;

        public PlayerStats Stats { get; private set; }
        public string PlayerName { get; private set; }
        public string CurrentRace { get; private set; }
        public string CurrentEvolution { get; set; }
        public string CurrentProfession { get; set; }
        public string Gender { get; private set; }

        private AnimatedSprite2D _animatedSprite;
        private Skills.SkillManager _skillManager;
        private Goals.GoalManager _goalManager;

        public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

        public override void _Ready()
        {
            Stats = new PlayerStats();
            _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
            _skillManager = GetNode<Skills.SkillManager>("SkillManager");
            _goalManager = GetNode<Goals.GoalManager>("GoalManager");

            InitializePlayer();
            GameManager.Instance.CurrentPlayer = this;
        }

        private void InitializePlayer()
        {
            CurrentRace = GameManager.Instance.CurrentPlayerRace;
            
            if (!string.IsNullOrEmpty(CurrentRace))
            {
                // Generate random gender and name
                var random = new System.Random();
                Gender = random.Next(2) == 0 ? "Male" : "Female";
                PlayerName = NameGenerator.GeneratePlayerName(CurrentRace, Gender);
                
                var raceData = GameManager.Instance.GetRaceData(CurrentRace);
                if (raceData != null)
                {
                    Stats.InitializeFromRace(raceData);
                    _skillManager.InitializeStartingSkills(raceData.StartingSkills);
                }
                
                GD.Print($"Player created: {PlayerName} ({Gender} {CurrentRace})");
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            Vector2 velocity = Velocity;

            // Add gravity
            if (!IsOnFloor())
                velocity.Y += gravity * (float)delta;

            // Handle jump
            if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
                velocity.Y = JumpVelocity;

            // Handle movement
            Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
            if (direction != Vector2.Zero)
            {
                velocity.X = direction.X * Speed;
                UpdateAnimation("walk");
            }
            else
            {
                velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
                UpdateAnimation("idle");
            }

            Velocity = velocity;
            MoveAndSlide();
        }

        private void UpdateAnimation(string animationName)
        {
            if (_animatedSprite != null && _animatedSprite.SpriteFrames != null)
            {
                if (_animatedSprite.SpriteFrames.HasAnimation(animationName))
                {
                    _animatedSprite.Play(animationName);
                }
            }
        }

        public void LevelUp()
        {
            Stats.Level++;
            Stats.ExperienceToNext = Stats.CalculateExperienceToNext();
            Stats.CurrentExperience = 0;
            
            // Distribute stat points based on race
            DistributeStatPoints();
            
            // Check for evolution/profession opportunities
            CheckProgressionOpportunities();
            
            GD.Print($"Level up! Now level {Stats.Level}");
        }

        private void DistributeStatPoints()
        {
            // Automatic stat distribution based on race tendencies
            var raceData = GameManager.Instance.GetRaceData(CurrentRace);
            if (raceData != null)
            {
                // Add 3 stat points per level, distributed based on race
                Stats.StatPoints += 3;
                // This could be made more sophisticated with race-specific growth patterns
            }
        }

        private void CheckProgressionOpportunities()
        {
            var raceData = GameManager.Instance.GetRaceData(CurrentRace);
            if (raceData == null) return;

            if (raceData.CanEvolve)
            {
                CheckEvolutionOpportunities(raceData);
            }
            else
            {
                CheckProfessionOpportunities(raceData);
            }
        }

        private void CheckEvolutionOpportunities(RaceData raceData)
        {
            foreach (var evolution in raceData.EvolutionPaths)
            {
                if (CanEvolve(evolution.Value))
                {
                    // Notify player of available evolution
                    GD.Print($"Evolution available: {evolution.Key}");
                    // This would trigger UI to show evolution options
                }
            }
        }

        private void CheckProfessionOpportunities(RaceData raceData)
        {
            foreach (var profession in raceData.ProfessionPaths)
            {
                if (CanChangeProfession(profession.Value))
                {
                    // Notify player of available profession
                    GD.Print($"Profession available: {profession.Key}");
                    // This would trigger UI to show profession options
                }
            }
        }

        private bool CanEvolve(EvolutionPath evolution)
        {
            foreach (var requirement in evolution.Requirements)
            {
                if (!CheckRequirement(requirement.Key, requirement.Value))
                    return false;
            }
            return true;
        }

        private bool CanChangeProfession(ProfessionPath profession)
        {
            foreach (var requirement in profession.Requirements)
            {
                if (!CheckRequirement(requirement.Key, requirement.Value))
                    return false;
            }
            return true;
        }

        private bool CheckRequirement(string requirementType, object value)
        {
            switch (requirementType.ToLower())
            {
                case "level":
                    return Stats.Level >= (int)value;
                case "strength":
                    return Stats.Strength >= (int)value;
                case "intelligence":
                    return Stats.Intelligence >= (int)value;
                case "dexterity":
                    return Stats.Dexterity >= (int)value;
                case "constitution":
                    return Stats.Constitution >= (int)value;
                case "wisdom":
                    return Stats.Wisdom >= (int)value;
                case "charisma":
                    return Stats.Charisma >= (int)value;
                // Add more requirement types as needed
                default:
                    return false;
            }
        }

        public void GainExperience(int amount)
        {
            Stats.CurrentExperience += amount;
            
            while (Stats.CurrentExperience >= Stats.ExperienceToNext)
            {
                LevelUp();
            }
        }

        public void EvolveTo(string evolutionName)
        {
            var raceData = GameManager.Instance.GetRaceData(CurrentRace);
            if (raceData?.EvolutionPaths.ContainsKey(evolutionName) == true)
            {
                var evolution = raceData.EvolutionPaths[evolutionName];
                if (CanEvolve(evolution))
                {
                    CurrentEvolution = evolutionName;
                    ApplyEvolutionBonuses(evolution);
                    UpdateAppearance();
                    GD.Print($"Evolved to {evolutionName}!");
                }
            }
        }

        public void ChangeProfessionTo(string professionName)
        {
            var raceData = GameManager.Instance.GetRaceData(CurrentRace);
            if (raceData?.ProfessionPaths.ContainsKey(professionName) == true)
            {
                var profession = raceData.ProfessionPaths[professionName];
                if (CanChangeProfession(profession))
                {
                    CurrentProfession = professionName;
                    ApplyProfessionBonuses(profession);
                    UpdateAppearance();
                    GD.Print($"Changed profession to {professionName}!");
                }
            }
        }

        private void ApplyEvolutionBonuses(EvolutionPath evolution)
        {
            foreach (var bonus in evolution.StatBonuses)
            {
                Stats.ApplyStatBonus(bonus.Key, bonus.Value);
            }

            foreach (var skill in evolution.UnlockedSkills)
            {
                _skillManager.UnlockSkill(skill);
            }
        }

        private void ApplyProfessionBonuses(ProfessionPath profession)
        {
            foreach (var bonus in profession.StatBonuses)
            {
                Stats.ApplyStatBonus(bonus.Key, bonus.Value);
            }

            foreach (var skill in profession.UnlockedSkills)
            {
                _skillManager.UnlockSkill(skill);
            }
        }

        private void UpdateAppearance()
        {
            // This would update the player's sprite based on current evolution/profession
            // For now, just print the change
            string currentForm = !string.IsNullOrEmpty(CurrentEvolution) ? CurrentEvolution : CurrentProfession;
            if (!string.IsNullOrEmpty(currentForm))
            {
                GD.Print($"Player appearance updated for {currentForm}");
            }
        }
    }
}
