using Godot;
using NewWorldEvolution.Data;

namespace NewWorldEvolution.Entities.Monsters
{
    public partial class Wolf : BaseMonster
    {
        [Export] public WolfType WolfVariant { get; set; } = WolfType.Gray;
        
        private bool _isHowling = false;
        private float _packBonus = 1.0f;
        private bool _isCircling = false;
        private float _circleAngle = 0;
        private Vector2 _circleCenter;

        public enum WolfType
        {
            Gray,       // Common forest wolf
            Black,      // Stronger, more aggressive
            White,      // Arctic wolf, ice attacks
            Dire,       // Large, powerful wolf
            Alpha       // Pack leader, commands others
        }

        protected override void InitializeMonster()
        {
            SetupWolfStats();
            SetupWolfAppearance();
            
            MonsterName = GetWolfName();
            Behavior = GetWolfBehavior();
            _territoryRadius = 250.0f; // Wolves have large territories
        }

        private void SetupWolfStats()
        {
            var baseHealth = 90;
            var baseAttack = 18;
            var baseDefense = 10;
            var baseSpeed = 80f;
            var baseExp = 15;

            // Modify stats based on wolf type
            switch (WolfVariant)
            {
                case WolfType.Gray:
                    // Default stats
                    break;
                case WolfType.Black:
                    baseHealth += 20;
                    baseAttack += 5;
                    baseDefense += 3;
                    baseSpeed += 10;
                    baseExp += 8;
                    break;
                case WolfType.White:
                    baseHealth += 15;
                    baseAttack += 8;
                    baseDefense += 5;
                    baseSpeed += 5;
                    baseExp += 12;
                    break;
                case WolfType.Dire:
                    baseHealth += 40;
                    baseAttack += 15;
                    baseDefense += 8;
                    baseSpeed -= 10; // Larger but slower
                    baseExp += 25;
                    break;
                case WolfType.Alpha:
                    baseHealth += 60;
                    baseAttack += 20;
                    baseDefense += 12;
                    baseSpeed += 15;
                    baseExp += 40;
                    break;
            }

            _stats = new MonsterStats
            {
                Level = Level,
                MaxHealth = baseHealth + Level * 18,
                MaxMana = 40 + Level * 4,
                Attack = baseAttack + Level * 4,
                Defense = baseDefense + Level * 2,
                Speed = (int)baseSpeed,
                AttackSpeed = 1.2f,
                DetectionRange = 130.0f,
                AttackRange = 35.0f,
                ExperienceReward = baseExp + Level * 5
            };
            
            _stats.Health = _stats.MaxHealth;
            _stats.Mana = _stats.MaxMana;
            
            MovementSpeed = _stats.Speed;
        }

        private void SetupWolfAppearance()
        {
            if (_sprite != null)
            {
                Color wolfColor = GetWolfColor();
                _sprite.Modulate = wolfColor;
                
                // Create a simple wolf texture if none exists
                if (_sprite.Texture == null)
                {
                    var image = Image.CreateEmpty(32, 24, false, Image.Format.Rgba8);
                    
                    // Draw a simple wolf shape
                    for (int x = 0; x < 32; x++)
                    {
                        for (int y = 0; y < 24; y++)
                        {
                            // Wolf body shape
                            if ((x >= 8 && x <= 24 && y >= 8 && y <= 16) ||    // Body
                                (x >= 4 && x <= 12 && y >= 6 && y <= 12) ||     // Head
                                (x >= 6 && x <= 8 && y >= 4 && y <= 8) ||       // Snout
                                (y >= 14 && y <= 23 && (x == 10 || x == 14 || x == 18 || x == 22))) // Legs
                            {
                                image.SetPixel(x, y, wolfColor);
                            }
                        }
                    }
                    
                    var texture = ImageTexture.CreateFromImage(image);
                    _sprite.Texture = texture;
                }
                
                // Scale based on wolf type
                float scale = WolfVariant switch
                {
                    WolfType.Dire => 1.3f,
                    WolfType.Alpha => 1.2f,
                    _ => 1.0f
                };
                _sprite.Scale = new Vector2(scale, scale);
            }
        }

        private Color GetWolfColor()
        {
            return WolfVariant switch
            {
                WolfType.Gray => new Color(0.6f, 0.6f, 0.6f),
                WolfType.Black => new Color(0.2f, 0.2f, 0.2f),
                WolfType.White => new Color(0.9f, 0.9f, 1.0f),
                WolfType.Dire => new Color(0.4f, 0.3f, 0.2f),
                WolfType.Alpha => new Color(0.7f, 0.5f, 0.3f),
                _ => Colors.Gray
            };
        }

        private string GetWolfName()
        {
            return WolfVariant switch
            {
                WolfType.Gray => "Gray Wolf",
                WolfType.Black => "Black Wolf",
                WolfType.White => "Arctic Wolf",
                WolfType.Dire => "Dire Wolf",
                WolfType.Alpha => "Alpha Wolf",
                _ => "Wolf"
            };
        }

        private MonsterBehavior GetWolfBehavior()
        {
            return WolfVariant switch
            {
                WolfType.Gray => MonsterBehavior.Neutral,       // Gray wolves avoid humans
                WolfType.Black => MonsterBehavior.Aggressive,   // Black wolves are predators
                WolfType.White => MonsterBehavior.Territorial,  // Arctic wolves defend territory
                WolfType.Dire => MonsterBehavior.Hostile,       // Dire wolves are dangerous
                WolfType.Alpha => MonsterBehavior.Hostile,      // Alpha wolves are pack leaders
                _ => MonsterBehavior.Territorial
            };
        }

        protected override Color GetNameColor()
        {
            return WolfVariant switch
            {
                WolfType.Gray => Colors.LightGray,
                WolfType.Black => Colors.Gray,
                WolfType.White => Colors.White,
                WolfType.Dire => Colors.Orange,
                WolfType.Alpha => Colors.Gold,
                _ => Colors.White
            };
        }

        protected override void UpdateAI(double delta)
        {
            // Check for pack members nearby
            UpdatePackBonus();
            
            base.UpdateAI(delta);
        }

        private void UpdatePackBonus()
        {
            int nearbyWolves = CountNearbyWolves();
            _packBonus = 1.0f + (nearbyWolves * 0.15f); // 15% bonus per nearby wolf
            
            if (nearbyWolves > 0 && WolfVariant == WolfType.Alpha)
            {
                _packBonus += 0.25f; // Alpha gets extra pack bonus
            }
        }

        private int CountNearbyWolves()
        {
            int count = 0;
            var spaceState = GetWorld2D().DirectSpaceState;
            var query = new PhysicsShapeQueryParameters2D();
            
            var circle = new CircleShape2D();
            circle.Radius = 100.0f;
            query.Shape = circle;
            query.Transform = new Transform2D(0, GlobalPosition);
            
            var results = spaceState.IntersectShape(query, 10);
            
            foreach (var result in results)
            {
                if (result.TryGetValue("collider", out var collider) && 
                    collider.Obj is Wolf otherWolf && 
                    otherWolf != this && !otherWolf._isDead)
                {
                    count++;
                }
            }
            
            return count;
        }

        protected override void HandleChaseState(double delta)
        {
            if (_target == null || !IsInstanceValid(_target))
            {
                _currentState = AIState.Idle;
                return;
            }

            float distanceToTarget = GlobalPosition.DistanceTo(_target.GlobalPosition);
            
            // Wolves use pack tactics - circle around the target
            if (CountNearbyWolves() > 0 && !_isCircling)
            {
                StartCircling();
            }
            
            if (_isCircling)
            {
                HandleCircling(delta);
                
                // Attack when in range
                if (distanceToTarget <= _stats.AttackRange)
                {
                    _currentState = AIState.Attack;
                    _isCircling = false;
                }
            }
            else
            {
                // Standard chase behavior
                base.HandleChaseState(delta);
            }
        }

        private void StartCircling()
        {
            _isCircling = true;
            _circleCenter = _target.GlobalPosition;
            _circleAngle = Mathf.Atan2(GlobalPosition.Y - _circleCenter.Y, GlobalPosition.X - _circleCenter.X);
        }

        private void HandleCircling(double delta)
        {
            if (_target == null) return;
            
            _circleCenter = _target.GlobalPosition;
            _circleAngle += (float)delta * 2.0f; // Circle speed
            
            float circleRadius = 60.0f;
            Vector2 targetPosition = _circleCenter + new Vector2(
                Mathf.Cos(_circleAngle) * circleRadius,
                Mathf.Sin(_circleAngle) * circleRadius
            );
            
            Vector2 direction = (targetPosition - GlobalPosition).Normalized();
            Velocity = direction * MovementSpeed;
        }

        protected override void AttackTarget()
        {
            if (_target == null) return;
            
            int damage = Mathf.RoundToInt(_stats.Attack * _packBonus);
            string attackMessage = $"{MonsterName}";
            
            switch (WolfVariant)
            {
                case WolfType.Gray:
                    attackMessage += " bites";
                    break;
                    
                case WolfType.Black:
                    // Chance for bleeding effect
                    if (GD.Randf() < 0.3f)
                    {
                        damage += 5;
                        attackMessage += " delivers a savage bite";
                    }
                    else
                    {
                        attackMessage += " snaps its jaws";
                    }
                    break;
                    
                case WolfType.White:
                    // Ice attack
                    damage += 3;
                    attackMessage += " bites with freezing fangs";
                    break;
                    
                case WolfType.Dire:
                    // Knockback attack
                    damage += 8;
                    attackMessage += " delivers a massive bite";
                    // TODO: Implement knockback effect
                    break;
                    
                case WolfType.Alpha:
                    // Howl to inspire pack
                    if (GD.Randf() < 0.4f && !_isHowling)
                    {
                        Howl();
                        return; // Howl instead of attacking this turn
                    }
                    damage += 6;
                    attackMessage += " strikes with pack leader fury";
                    break;
            }
            
            if (_packBonus > 1.0f)
            {
                attackMessage += $" (Pack bonus: {(_packBonus - 1.0f) * 100:F0}%)";
            }
            
            GD.Print($"{attackMessage} for {damage} damage!");
            
            if (_target.HasMethod("TakeDamage"))
            {
                _target.Call("TakeDamage", damage);
            }
            
            // Play attack animation
            if (_animationPlayer != null && _animationPlayer.HasAnimation("attack"))
            {
                _animationPlayer.Play("attack");
            }
        }

        private void Howl()
        {
            _isHowling = true;
            GD.Print($"{MonsterName} howls, inspiring the pack!");
            
            // Find and buff nearby wolves
            var spaceState = GetWorld2D().DirectSpaceState;
            var query = new PhysicsShapeQueryParameters2D();
            
            var circle = new CircleShape2D();
            circle.Radius = 200.0f;
            query.Shape = circle;
            query.Transform = new Transform2D(0, GlobalPosition);
            
            var results = spaceState.IntersectShape(query, 10);
            
            foreach (var result in results)
            {
                if (result.TryGetValue("collider", out var collider) && 
                    collider.Obj is Wolf otherWolf && 
                    otherWolf != this)
                {
                    // Buff the wolf
                    otherWolf._stats.Attack += 10;
                    otherWolf._stats.Speed += 20;
                    
                    // Alert them to the target
                    if (otherWolf._target == null && _target != null)
                    {
                        otherWolf.SetTarget(_target);
                        otherWolf._currentState = AIState.Chase;
                    }
                    
                    GD.Print($"{otherWolf.MonsterName} is inspired by the howl!");
                    
                    // Remove buffs after 15 seconds
                    var timer = GetTree().CreateTimer(15.0);
                    timer.Timeout += () => {
                        if (IsInstanceValid(otherWolf))
                        {
                            otherWolf._stats.Attack -= 10;
                            otherWolf._stats.Speed -= 20;
                        }
                    };
                }
            }
            
            // Reset howling after 2 seconds
            var howlTimer = GetTree().CreateTimer(2.0);
            howlTimer.Timeout += () => _isHowling = false;
        }

        protected override void HandleReturnState(double delta)
        {
            // Wolves are territorial and persistent
            if (_target != null && 
                GlobalPosition.DistanceTo(_target.GlobalPosition) < _stats.DetectionRange * 2)
            {
                // Continue chasing if target is still nearby
                _currentState = AIState.Chase;
                return;
            }
            
            base.HandleReturnState(delta);
        }

        public override void TakeDamage(int damage)
        {
            base.TakeDamage(damage);
            
            // Wolves call for help when hurt
            if (_stats.Health < _stats.MaxHealth * 0.7f && CountNearbyWolves() == 0)
            {
                CallPack();
            }
        }

        private void CallPack()
        {
            GD.Print($"{MonsterName} calls for the pack!");
            
            // Try to spawn or alert distant wolves
            // This is a simple implementation - in a real game you might have a more sophisticated pack system
            var nearbyWolves = GetTree().GetNodesInGroup("wolves");
            foreach (Node wolf in nearbyWolves)
            {
                if (wolf is Wolf otherWolf && otherWolf != this && 
                    GlobalPosition.DistanceTo(otherWolf.GlobalPosition) < 300.0f)
                {
                    if (otherWolf._target == null && _target != null)
                    {
                        otherWolf.SetTarget(_target);
                        otherWolf._currentState = AIState.Chase;
                    }
                }
            }
        }

        public override void _Ready()
        {
            base._Ready();
            AddToGroup("wolves"); // Add to wolves group for pack behavior
        }
    }
}
