using Godot;
using System.Collections.Generic;
using NewWorldEvolution.Data;
using NewWorldEvolution.Core;

namespace NewWorldEvolution.Player
{
    public partial class PlayerController : CharacterBody2D
    {
        [Export] public float Speed = 200.0f;
        [Export] public float ClickMoveThreshold = 10.0f; // Minimum distance to trigger click movement
        [Export] public float PathNodeDistance = 50.0f; // Distance between path nodes for visualization

        private Vector2 _targetPosition;
        private bool _isMovingToTarget = false;
        private List<Vector2> _currentPath = new List<Vector2>();
        private int _currentPathIndex = 0;
        
        // Visual indicators
        private Node2D _visualIndicators;
        private List<Node2D> _pathMarkers = new List<Node2D>();

        public PlayerStats Stats { get; private set; }
        public string PlayerName { get; private set; }
        public string CurrentRace { get; private set; }
        public string CurrentEvolution { get; set; }
        public string CurrentProfession { get; set; }
        public string Gender { get; private set; }

        private Skills.SkillManager _skillManager;
        private Goals.GoalManager _goalManager;

        public override void _Ready()
        {
            Stats = new PlayerStats();
            _skillManager = GetNode<Skills.SkillManager>("SkillManager");
            _goalManager = GetNode<Goals.GoalManager>("GoalManager");

            // Setup visual indicators - defer to avoid parent busy error
            CallDeferred(nameof(SetupVisualIndicators));

            InitializePlayer();
            GameManager.Instance.CurrentPlayer = this;
        }

        private void SetupVisualIndicators()
        {
            _visualIndicators = new Node2D();
            _visualIndicators.Name = "VisualIndicators";
            GetParent().AddChild(_visualIndicators);
        }

        private void InitializePlayer()
        {
            // Try to get race from GameManager's CurrentPlayerRace, fallback to SelectedRace
            CurrentRace = GameManager.Instance.CurrentPlayerRace;
            if (string.IsNullOrEmpty(CurrentRace))
            {
                CurrentRace = GameManager.SelectedRace;
                GameManager.Instance.CurrentPlayerRace = CurrentRace;
            }
            
            if (!string.IsNullOrEmpty(CurrentRace))
            {
                // Use selected name and gender, or generate if not provided
                Gender = !string.IsNullOrEmpty(GameManager.SelectedGender) ? GameManager.SelectedGender : "Male";
                
                if (!string.IsNullOrEmpty(GameManager.SelectedName))
                {
                    PlayerName = GameManager.SelectedName;
                }
                else
                {
                    PlayerName = NameGenerator.GeneratePlayerName(CurrentRace, Gender);
                }
                
                var raceData = GameManager.Instance.GetRaceData(CurrentRace);
                if (raceData != null)
                {
                    Stats.InitializeFromRace(raceData);
                    _skillManager.InitializeStartingSkills(raceData.StartingSkills);
                }
                
                // Update visual representation based on race
                UpdateVisualRepresentation();
                
                GD.Print($"Player created: {PlayerName} ({Gender} {CurrentRace})");
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            Vector2 velocity = Vector2.Zero;

            // Handle manual keyboard movement (takes priority over click movement)
            Vector2 inputDirection = GetKeyboardInput();
            
            if (inputDirection.Length() > 0)
            {
                // Manual movement - cancel click movement
                _isMovingToTarget = false;
                ClearVisualIndicators();
                velocity = inputDirection.Normalized() * Speed;
            }
            else if (_isMovingToTarget)
            {
                // Click-to-move behavior
                velocity = HandleClickMovement();
            }

            Velocity = velocity;
            MoveAndSlide();
        }

        public override void _Input(InputEvent @event)
        {
            // Handle right-click-to-move
            if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Right)
            {
                // Convert screen position to world position
                _targetPosition = GetGlobalMousePosition();
                
                // Only start moving if the target is far enough away
                if (GlobalPosition.DistanceTo(_targetPosition) > ClickMoveThreshold)
                {
                    CalculatePath(_targetPosition);
                    ShowVisualIndicators();
                    _isMovingToTarget = true;
                }
            }
        }

        private Vector2 GetKeyboardInput()
        {
            Vector2 direction = Vector2.Zero;
            
            // Check for Arrow keys (UI actions)
            if (Input.IsActionPressed("ui_left"))
                direction.X -= 1;
            if (Input.IsActionPressed("ui_right"))
                direction.X += 1;
            if (Input.IsActionPressed("ui_up"))
                direction.Y -= 1;
            if (Input.IsActionPressed("ui_down"))
                direction.Y += 1;

            // Check for WASD keys (custom movement actions)
            if (Input.IsActionPressed("move_left"))
                direction.X -= 1;
            if (Input.IsActionPressed("move_right"))
                direction.X += 1;
            if (Input.IsActionPressed("move_up"))
                direction.Y -= 1;
            if (Input.IsActionPressed("move_down"))
                direction.Y += 1;

            return direction;
        }

        private Vector2 HandleClickMovement()
        {
            if (_currentPath.Count == 0)
            {
                _isMovingToTarget = false;
                ClearVisualIndicators();
                return Vector2.Zero;
            }

            // Get current target from path
            Vector2 currentTarget = _currentPath[_currentPathIndex];
            Vector2 toTarget = currentTarget - GlobalPosition;
            float distanceToTarget = toTarget.Length();

            // If we're close enough to current path node, move to next one
            if (distanceToTarget < ClickMoveThreshold)
            {
                _currentPathIndex++;
                
                // If we've reached the end of the path, stop moving
                if (_currentPathIndex >= _currentPath.Count)
                {
                    _isMovingToTarget = false;
                    ClearVisualIndicators();
                    return Vector2.Zero;
                }
                
                // Update current target to next path node
                currentTarget = _currentPath[_currentPathIndex];
                toTarget = currentTarget - GlobalPosition;
            }

            // Move towards the current target
            return toTarget.Normalized() * Speed;
        }

        private void UpdateVisualRepresentation()
        {
            var visualRep = GetNodeOrNull<Node2D>("VisualRepresentation");
            if (visualRep != null)
            {
                var body = visualRep.GetNodeOrNull<ColorRect>("Body");
                var head = visualRep.GetNodeOrNull<ColorRect>("Head");
                
                if (body != null && head != null)
                {
                    // Set colors based on race
                    var raceColors = GetRaceColors(CurrentRace);
                    body.Color = raceColors.Body;
                    head.Color = raceColors.Head;
                }
            }
            
            // Also setup collision shape if it doesn't exist
            var collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
            if (collisionShape != null && collisionShape.Shape == null)
            {
                var rectShape = new RectangleShape2D();
                rectShape.Size = new Vector2(30, 40); // Match the visual representation size
                collisionShape.Shape = rectShape;
            }
        }
        
        private (Color Body, Color Head) GetRaceColors(string race)
        {
            return race switch
            {
                "Human" => (new Color(0.7f, 0.7f, 0.9f), new Color(0.9f, 0.8f, 0.7f)),
                "Goblin" => (new Color(0.4f, 0.7f, 0.3f), new Color(0.5f, 0.8f, 0.4f)),
                "Spider" => (new Color(0.2f, 0.1f, 0.1f), new Color(0.3f, 0.2f, 0.2f)),
                "Demon" => (new Color(0.8f, 0.2f, 0.2f), new Color(0.9f, 0.3f, 0.3f)),
                "Vampire" => (new Color(0.3f, 0.3f, 0.3f), new Color(0.9f, 0.9f, 0.9f)),
                _ => (new Color(0.7f, 0.7f, 0.9f), new Color(0.9f, 0.8f, 0.7f))
            };
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

        private void CalculatePath(Vector2 targetPosition)
        {
            _currentPath.Clear();
            _currentPathIndex = 0;

            // Simple pathfinding - for now just create a straight line with path nodes
            // In a more advanced system, this would use A* or other pathfinding algorithms
            Vector2 startPos = GlobalPosition;
            Vector2 direction = (targetPosition - startPos).Normalized();
            float totalDistance = startPos.DistanceTo(targetPosition);

            // Create path nodes every PathNodeDistance units
            for (float distance = PathNodeDistance; distance < totalDistance; distance += PathNodeDistance)
            {
                Vector2 pathPoint = startPos + direction * distance;
                
                // Basic obstacle avoidance - check if path point is valid
                if (IsValidPathPoint(pathPoint))
                {
                    _currentPath.Add(pathPoint);
                }
                else
                {
                    // Try to find alternate route around obstacle
                    Vector2 altPoint = FindAlternatePathPoint(pathPoint, direction);
                    _currentPath.Add(altPoint);
                }
            }

            // Always add the final target
            _currentPath.Add(targetPosition);
        }

        private bool IsValidPathPoint(Vector2 point)
        {
            // For now, simple check - in a real game this would check for walls, obstacles, etc.
            // You could use raycasting or tile-based collision detection here
            return true; // Placeholder - assume all points are valid for now
        }

        private Vector2 FindAlternatePathPoint(Vector2 blockedPoint, Vector2 direction)
        {
            // Simple obstacle avoidance - try perpendicular directions
            Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
            
            // Try offsets to the side
            Vector2[] offsets = { 
                perpendicular * 30, 
                -perpendicular * 30,
                perpendicular * 60,
                -perpendicular * 60
            };

            foreach (Vector2 offset in offsets)
            {
                Vector2 testPoint = blockedPoint + offset;
                if (IsValidPathPoint(testPoint))
                {
                    return testPoint;
                }
            }

            // If no good alternative found, return original point
            return blockedPoint;
        }

        private void ShowVisualIndicators()
        {
            ClearVisualIndicators();

            if (_visualIndicators == null || _currentPath.Count == 0)
            {
                // If visual indicators not ready yet, defer the call
                if (_visualIndicators == null)
                {
                    CallDeferred(nameof(ShowVisualIndicators));
                }
                return;
            }

            // Create visual markers for the path
            for (int i = 0; i < _currentPath.Count; i++)
            {
                var marker = CreatePathMarker(_currentPath[i], i == _currentPath.Count - 1);
                _pathMarkers.Add(marker);
                _visualIndicators.AddChild(marker);
            }

            // Draw lines between path points
            CreatePathLines();
        }

        private Node2D CreatePathMarker(Vector2 position, bool isTarget)
        {
            var marker = new Node2D();
            marker.Position = position;

            var visual = new ColorRect();
            visual.Size = new Vector2(8, 8);
            visual.Position = new Vector2(-4, -4); // Center the marker
            visual.Color = isTarget ? new Color(1, 0, 0, 0.8f) : new Color(0, 1, 0, 0.6f); // Red for target, green for path nodes
            
            marker.AddChild(visual);
            return marker;
        }

        private void CreatePathLines()
        {
            // Create a simple line renderer for the path
            // This is a basic implementation - could be enhanced with Line2D nodes
            if (_currentPath.Count < 2) return;

            var lineRenderer = new Node2D();
            lineRenderer.Name = "PathLines";
            _visualIndicators.AddChild(lineRenderer);
            _pathMarkers.Add(lineRenderer); // Add to cleanup list
        }

        private void ClearVisualIndicators()
        {
            if (_pathMarkers == null)
                return;

            foreach (Node2D marker in _pathMarkers)
            {
                if (IsInstanceValid(marker))
                {
                    marker.QueueFree();
                }
            }
            _pathMarkers.Clear();
        }
    }
}
