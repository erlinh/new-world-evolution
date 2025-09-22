using Godot;
using System.Collections.Generic;
using NewWorldEvolution.Data;
using NewWorldEvolution.Core;
using NewWorldEvolution.UI;

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
        private OverheadDisplay _overheadDisplay;
        
        // Combat and targeting
        private Node2D _currentTarget;
        private Node2D _targetIndicator;
        private float _lastAttackTime = 0;
        private float _attackCooldown = 1.0f; // 1 second attack cooldown

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
            CallDeferred(nameof(SetupOverheadDisplay));

            InitializePlayer();
            GameManager.Instance.CurrentPlayer = this;
        }

        private void SetupVisualIndicators()
        {
            _visualIndicators = new Node2D();
            _visualIndicators.Name = "VisualIndicators";
            GetParent().AddChild(_visualIndicators);
        }

        private void SetupOverheadDisplay()
        {
            _overheadDisplay = new OverheadDisplay();
            string displayName = !string.IsNullOrEmpty(PlayerName) ? PlayerName : 
                               !string.IsNullOrEmpty(GameManager.SelectedName) ? GameManager.SelectedName : "Player";
            _overheadDisplay.SetEntity(this, displayName, Stats?.Level ?? 1, Colors.Cyan);
            _overheadDisplay.ShowHealthBar(false); // Players don't need health bars by default
            AddChild(_overheadDisplay);
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
            
            // Handle targeting and combat
            HandleTargetingLogic();
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
            {
                if (mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    // Handle left-click targeting/attacking
                    HandleLeftClick(mouseEvent);
                }
                else if (mouseEvent.ButtonIndex == MouseButton.Right)
                {
                    // Handle right-click-to-move
                    HandleRightClick(mouseEvent);
                }
            }
            
            // Handle attack key (spacebar)
            if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Space)
            {
                TryAttack();
            }
        }

        private void HandleLeftClick(InputEventMouseButton mouseEvent)
        {
            // Check if we clicked on a monster
            var clickPos = GetGlobalMousePosition();
            var target = FindTargetAtPosition(clickPos);
            
            if (target != null)
            {
                SetTarget(target);
                GD.Print("Target selected. Use abilities (Q/W/E/R) to attack!");
            }
            else
            {
                // Clear target if clicking on empty space
                ClearTarget();
            }
        }

        private void HandleRightClick(InputEventMouseButton mouseEvent)
        {
            // Convert screen position to world position
            _targetPosition = GetGlobalMousePosition();
            
            // Clear any current target when right-clicking to move
            ClearTarget();
            
            // Only start moving if the target is far enough away
            if (GlobalPosition.DistanceTo(_targetPosition) > ClickMoveThreshold)
            {
                CalculatePath(_targetPosition);
                ShowVisualIndicators();
                _isMovingToTarget = true;
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
            
            GD.Print($"🎉 LEVEL UP! Player is now level {Stats.Level}!");
            
            // Update overhead display
            string displayName = !string.IsNullOrEmpty(PlayerName) ? PlayerName : 
                               !string.IsNullOrEmpty(GameManager.SelectedName) ? GameManager.SelectedName : "Player";
            _overheadDisplay?.SetEntity(this, displayName, Stats.Level, Colors.Cyan);

            // Visual effect
            if (HasNode("Sprite2D"))
            {
                var sprite = GetNode<Sprite2D>("Sprite2D");
                var tween = CreateTween();
                tween.TweenProperty(sprite, "modulate", Colors.Gold, 0.3f);
                tween.TweenProperty(sprite, "modulate", Colors.White, 0.5f);
                tween.SetLoops(3);
            }
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

        public void TakeDamage(int damage)
        {
            if (Stats == null) return;

            Stats.Health -= damage;
            Stats.Health = Mathf.Max(0, Stats.Health); // Don't go below 0

            GD.Print($"Player takes {damage} damage! Health: {Stats.Health}/{Stats.MaxHealth}");

            // Show damage effect
            _overheadDisplay?.ShowDamage(damage);
            _overheadDisplay?.ShowHealthBar(true); // Show health bar when taking damage
            _overheadDisplay?.UpdateHealthBar(Stats.Health, Stats.MaxHealth);

            // Hide health bar after 5 seconds
            var timer = GetTree().CreateTimer(5.0);
            timer.Timeout += () => _overheadDisplay?.ShowHealthBar(false);

            // Flash the player sprite
            if (HasNode("Sprite2D"))
            {
                var sprite = GetNode<Sprite2D>("Sprite2D");
                var tween = CreateTween();
                tween.TweenProperty(sprite, "modulate", Colors.Red, 0.1f);
                tween.TweenProperty(sprite, "modulate", Colors.White, 0.2f);
            }

            // Check for death
            if (Stats.Health <= 0)
            {
                Die();
            }
        }


        private void Die()
        {
            GD.Print("💀 Player has died!");
            
            // TODO: Implement respawn system
            // For now, just respawn at spawn location with full health
            Stats.Health = Stats.MaxHealth;
            var spawnLocation = GameManager.Instance?.CurrentSpawnLocation;
            if (!string.IsNullOrEmpty(spawnLocation))
            {
                // Move to spawn - this is a simple implementation
                GlobalPosition = new Vector2(400, 300); // Default position
                GD.Print("Player respawned!");
            }
        }

        private void HandleTargetingLogic()
        {
            // Clear target if it becomes invalid
            if (_currentTarget != null && !IsInstanceValid(_currentTarget))
            {
                ClearTarget();
            }
        }

        // Targeting and Combat Methods
        private Node2D FindTargetAtPosition(Vector2 worldPosition)
        {
            // Use physics query to find what's at the click position
            var spaceState = GetWorld2D().DirectSpaceState;
            var query = new PhysicsPointQueryParameters2D();
            query.Position = worldPosition;
            query.CollisionMask = 1; // Assuming monsters are on layer 1
            
            var results = spaceState.IntersectPoint(query, 10);
            
            foreach (var result in results)
            {
                if (result.TryGetValue("collider", out var collider))
                {
                    var node = collider.Obj;
                    // Check if it's a monster (has TakeDamage method)
                    if (node is GodotObject godotNode && godotNode.HasMethod("TakeDamage") && node != this)
                    {
                        return (Node2D)node;
                    }
                }
            }
            
            return null;
        }

        private void SetTarget(Node2D target)
        {
            _currentTarget = target;
            CreateTargetIndicator();
            
            // Update HUD target panel
            var hud = GetNode<UI.HUDManager>("/root/GameWorld/UI/HUD");
            if (hud != null && target is Entities.BaseMonster monster)
            {
                hud.SetTarget(monster);
            }
            
            GD.Print($"Target set: {target.Name}");
        }

        private void ClearTarget()
        {
            _currentTarget = null;
            RemoveTargetIndicator();
            
            // Update HUD target panel
            var hud = GetNode<UI.HUDManager>("/root/GameWorld/UI/HUD");
            hud?.ClearTarget();
        }

        private void CreateTargetIndicator()
        {
            RemoveTargetIndicator();
            
            if (_currentTarget == null) return;
            
            _targetIndicator = new Node2D();
            _targetIndicator.Name = "TargetIndicator";
            
            // Create a visual circle around the target
            var circle = new ColorRect();
            circle.Size = new Vector2(60, 60);
            circle.Position = new Vector2(-30, -30);
            circle.Color = new Color(1, 0, 0, 0.3f); // Semi-transparent red
            
            // Make it round by using a circular texture
            var image = Image.CreateEmpty(60, 60, false, Image.Format.Rgba8);
            for (int x = 0; x < 60; x++)
            {
                for (int y = 0; y < 60; y++)
                {
                    float distance = Mathf.Sqrt((x - 30) * (x - 30) + (y - 30) * (y - 30));
                    if (distance >= 25 && distance <= 30) // Ring shape
                    {
                        image.SetPixel(x, y, new Color(1, 0, 0, 0.8f));
                    }
                }
            }
            
            var texture = ImageTexture.CreateFromImage(image);
            var textureRect = new TextureRect();
            textureRect.Texture = texture;
            textureRect.Size = new Vector2(60, 60);
            textureRect.Position = new Vector2(-30, -30);
            
            _targetIndicator.AddChild(textureRect);
            _currentTarget.AddChild(_targetIndicator);
            
            // Animate the indicator
            var tween = _targetIndicator.CreateTween();
            tween.SetLoops();
            tween.TweenProperty(_targetIndicator, "modulate", new Color(1, 1, 1, 0.5f), 0.5f);
            tween.TweenProperty(_targetIndicator, "modulate", Colors.White, 0.5f);
        }

        private void RemoveTargetIndicator()
        {
            if (_targetIndicator != null && IsInstanceValid(_targetIndicator))
            {
                _targetIndicator.QueueFree();
                _targetIndicator = null;
            }
        }

        private bool IsTargetInAttackRange()
        {
            if (_currentTarget == null || !IsInstanceValid(_currentTarget))
                return false;
                
            float attackRange = 50.0f; // Player attack range
            return GlobalPosition.DistanceTo(_currentTarget.GlobalPosition) <= attackRange;
        }

        private void MoveToTarget()
        {
            if (_currentTarget == null || !IsInstanceValid(_currentTarget))
            {
                ClearTarget();
                return;
            }
            
            // Stop any current movement
            _isMovingToTarget = false;
            ClearVisualIndicators();
            
            // Move towards target
            Vector2 targetPos = _currentTarget.GlobalPosition;
            float attackRange = 45.0f; // Get close enough to attack
            Vector2 direction = (targetPos - GlobalPosition).Normalized();
            Vector2 destination = targetPos - direction * attackRange;
            
            CalculatePath(destination);
            ShowVisualIndicators();
            _isMovingToTarget = true;
        }

        private void TryAttack()
        {
            if (_currentTarget == null || !IsInstanceValid(_currentTarget))
                return;
                
            if (!IsTargetInAttackRange())
            {
                GD.Print("Target too far away!");
                return;
            }
            
            float timeSinceLastAttack = (float)Time.GetUnixTimeFromSystem() - _lastAttackTime;
            if (timeSinceLastAttack < _attackCooldown)
            {
                GD.Print($"Attack on cooldown: {_attackCooldown - timeSinceLastAttack:F1}s remaining");
                return;
            }
            
            // Perform attack
            PerformAttack();
            _lastAttackTime = (float)Time.GetUnixTimeFromSystem();
        }

        private void PerformAttack()
        {
            if (_currentTarget == null) return;
            
            // Calculate damage based on player stats
            int baseDamage = 20; // Base player damage
            int totalDamage = baseDamage + (Stats?.Level ?? 1) * 3;
            
            GD.Print($"Player attacks {_currentTarget.Name} for {totalDamage} damage!");
            
            // Apply damage to target
            if (_currentTarget.HasMethod("TakeDamage"))
            {
                _currentTarget.Call("TakeDamage", totalDamage);
            }
            
            // Visual attack effect
            ShowAttackEffect();
            
            // Check if target is still valid after attack
            if (_currentTarget.HasMethod("IsDead") && (bool)_currentTarget.Call("IsDead"))
            {
                ClearTarget();
            }
        }

        private void ShowAttackEffect()
        {
            // Create a simple attack animation
            if (_currentTarget == null) return;
            
            var attackEffect = new Node2D();
            attackEffect.Name = "AttackEffect";
            attackEffect.GlobalPosition = _currentTarget.GlobalPosition;
            
            // Create impact effect
            var impact = new ColorRect();
            impact.Size = new Vector2(20, 20);
            impact.Position = new Vector2(-10, -10);
            impact.Color = Colors.Yellow;
            attackEffect.AddChild(impact);
            
            GetParent().AddChild(attackEffect);
            
            // Animate the effect
            var tween = attackEffect.CreateTween();
            tween.TweenProperty(impact, "scale", Vector2.One * 2, 0.2f);
            tween.Parallel().TweenProperty(impact, "modulate", new Color(1, 1, 0, 0), 0.2f);
            tween.TweenCallback(Callable.From(() => attackEffect.QueueFree()));
            
            // Flash player sprite
            if (HasNode("Sprite2D"))
            {
                var sprite = GetNode<Sprite2D>("Sprite2D");
                var playerTween = CreateTween();
                playerTween.TweenProperty(sprite, "modulate", Colors.Yellow, 0.1f);
                playerTween.TweenProperty(sprite, "modulate", Colors.White, 0.1f);
            }
        }


        // Ability System
        public void UseAbility(string abilityName)
        {
            if (_currentTarget == null || !IsInstanceValid(_currentTarget) || !IsTargetInAttackRange())
            {
                GD.Print($"Cannot use {abilityName}: No target or target out of range");
                return;
            }

            int damage = CalculateAbilityDamage(abilityName);
            string effectDescription = GetAbilityEffect(abilityName);
            
            GD.Print($"Player uses {abilityName} on {_currentTarget.Name} for {damage} damage! {effectDescription}");
            
            // Apply damage
            if (_currentTarget.HasMethod("TakeDamage"))
            {
                _currentTarget.Call("TakeDamage", damage);
            }
            
            // Show special effect
            ShowAbilityEffect(abilityName);
            
            // Check if target died
            if (_currentTarget.HasMethod("IsDead") && (bool)_currentTarget.Call("IsDead"))
            {
                ClearTarget();
            }
        }

        private int CalculateAbilityDamage(string abilityName)
        {
            int baseDamage = 20;
            int levelBonus = (Stats?.Level ?? 1) * 3;
            
            return abilityName switch
            {
                "BasicAttack" => baseDamage + levelBonus,
                "PowerStrike" => (baseDamage + levelBonus) * 2, // Double damage
                "QuickSlash" => (baseDamage + levelBonus) / 2, // Half damage but faster
                "SpinAttack" => baseDamage + levelBonus + 15, // Area damage bonus
                _ => baseDamage + levelBonus
            };
        }

        private string GetAbilityEffect(string abilityName)
        {
            return abilityName switch
            {
                "BasicAttack" => "",
                "PowerStrike" => "(Powerful blow!)",
                "QuickSlash" => "(Lightning fast!)",
                "SpinAttack" => "(Area attack!)",
                _ => ""
            };
        }

        private void ShowAbilityEffect(string abilityName)
        {
            if (_currentTarget == null) return;
            
            Color effectColor = abilityName switch
            {
                "BasicAttack" => Colors.Yellow,
                "PowerStrike" => Colors.Red,
                "QuickSlash" => Colors.Cyan,
                "SpinAttack" => Colors.Orange,
                _ => Colors.White
            };
            
            var attackEffect = new Node2D();
            attackEffect.Name = $"{abilityName}Effect";
            attackEffect.GlobalPosition = _currentTarget.GlobalPosition;
            
            // Create different effects for different abilities
            CreateAbilityVisualEffect(attackEffect, abilityName, effectColor);
            
            GetParent().AddChild(attackEffect);
            
            // Animate the effect
            var tween = attackEffect.CreateTween();
            tween.TweenProperty(attackEffect, "scale", Vector2.One * 2, 0.3f);
            tween.Parallel().TweenProperty(attackEffect, "modulate", new Color(effectColor.R, effectColor.G, effectColor.B, 0), 0.3f);
            tween.TweenCallback(Callable.From(() => attackEffect.QueueFree()));
            
            // Flash player sprite with ability color
            if (HasNode("Sprite2D"))
            {
                var sprite = GetNode<Sprite2D>("Sprite2D");
                var playerTween = CreateTween();
                playerTween.TweenProperty(sprite, "modulate", effectColor, 0.1f);
                playerTween.TweenProperty(sprite, "modulate", Colors.White, 0.2f);
            }
        }

        private void CreateAbilityVisualEffect(Node2D parent, string abilityName, Color color)
        {
            switch (abilityName)
            {
                case "BasicAttack":
                    CreateBasicAttackEffect(parent, color);
                    break;
                case "PowerStrike":
                    CreatePowerStrikeEffect(parent, color);
                    break;
                case "QuickSlash":
                    CreateQuickSlashEffect(parent, color);
                    break;
                case "SpinAttack":
                    CreateSpinAttackEffect(parent, color);
                    break;
            }
        }

        private void CreateBasicAttackEffect(Node2D parent, Color color)
        {
            var impact = new ColorRect();
            impact.Size = new Vector2(20, 20);
            impact.Position = new Vector2(-10, -10);
            impact.Color = color;
            parent.AddChild(impact);
        }

        private void CreatePowerStrikeEffect(Node2D parent, Color color)
        {
            // Larger impact with multiple circles
            for (int i = 0; i < 3; i++)
            {
                var impact = new ColorRect();
                impact.Size = new Vector2(30 + i * 10, 30 + i * 10);
                impact.Position = new Vector2(-(15 + i * 5), -(15 + i * 5));
                impact.Color = new Color(color.R, color.G, color.B, 0.7f - i * 0.2f);
                parent.AddChild(impact);
            }
        }

        private void CreateQuickSlashEffect(Node2D parent, Color color)
        {
            // Multiple small slashes
            for (int i = 0; i < 5; i++)
            {
                var slash = new ColorRect();
                slash.Size = new Vector2(15, 5);
                slash.Position = new Vector2(-7.5f + i * 3, -2.5f);
                slash.Color = color;
                parent.AddChild(slash);
            }
        }

        private void CreateSpinAttackEffect(Node2D parent, Color color)
        {
            // Circular effect
            for (int i = 0; i < 8; i++)
            {
                var blade = new ColorRect();
                blade.Size = new Vector2(25, 8);
                blade.Position = new Vector2(-12.5f, -4);
                blade.Rotation = i * Mathf.Pi / 4;
                blade.Color = color;
                parent.AddChild(blade);
            }
        }

        // Public getters for monsters and other systems
        public PlayerStats GetStats() => Stats;
        public bool IsAlive() => Stats?.Health > 0;
        public Node2D GetCurrentTarget() => _currentTarget;
        public bool HasTarget() => _currentTarget != null && IsInstanceValid(_currentTarget);
    }
}
