using Godot;
using NewWorldEvolution.Data;

namespace NewWorldEvolution.Entities.Monsters
{
    public partial class Slime : BaseMonster
    {
        [Export] public SlimeType SlimeVariant { get; set; } = SlimeType.Green;
        
        private float _bounceTimer = 0;
        private Vector2 _bounceDirection = Vector2.Zero;
        private bool _isBouncing = false;

        public enum SlimeType
        {
            Green,    // Common, low level
            Blue,     // Water-based, medium level
            Red,      // Fire-based, medium level
            Purple,   // Toxic, higher level
            Golden    // Rare, high level
        }

        protected override void InitializeMonster()
        {
            SetupSlimeStats();
            SetupSlimeAppearance();
            
            MonsterName = GetSlimeName();
            Behavior = GetSlimeBehavior();
            _territoryRadius = 100.0f; // Slimes don't roam far
        }

        private void SetupSlimeStats()
        {
            var baseHealth = 60;
            var baseAttack = 8;
            var baseDefense = 2;
            var baseSpeed = 30f;
            var baseExp = 8;

            // Modify stats based on slime type
            switch (SlimeVariant)
            {
                case SlimeType.Green:
                    // Default values
                    break;
                case SlimeType.Blue:
                    baseHealth += 20;
                    baseAttack += 3;
                    baseDefense += 2;
                    baseExp += 5;
                    break;
                case SlimeType.Red:
                    baseHealth += 10;
                    baseAttack += 8;
                    baseDefense += 1;
                    baseSpeed += 10;
                    baseExp += 7;
                    break;
                case SlimeType.Purple:
                    baseHealth += 30;
                    baseAttack += 12;
                    baseDefense += 5;
                    baseExp += 15;
                    break;
                case SlimeType.Golden:
                    baseHealth += 50;
                    baseAttack += 20;
                    baseDefense += 8;
                    baseSpeed += 20;
                    baseExp += 50;
                    break;
            }

            _stats = new MonsterStats
            {
                Level = Level,
                MaxHealth = baseHealth + Level * 12,
                MaxMana = 20 + Level * 3,
                Attack = baseAttack + Level * 2,
                Defense = baseDefense + Level * 1,
                Speed = (int)baseSpeed,
                AttackSpeed = 0.8f,
                DetectionRange = 80.0f,
                AttackRange = 30.0f,
                ExperienceReward = baseExp + Level * 3
            };
            
            _stats.Health = _stats.MaxHealth;
            _stats.Mana = _stats.MaxMana;
            
            MovementSpeed = _stats.Speed;
        }

        private void SetupSlimeAppearance()
        {
            if (_sprite != null)
            {
                Color slimeColor = GetSlimeColor();
                _sprite.Modulate = slimeColor;
                
                // Create a simple slime texture if none exists
                if (_sprite.Texture == null)
                {
                    var image = Image.CreateEmpty(24, 24, false, Image.Format.Rgba8);
                    
                    // Draw a simple circle for the slime
                    for (int x = 0; x < 24; x++)
                    {
                        for (int y = 0; y < 24; y++)
                        {
                            float distance = Mathf.Sqrt((x - 12) * (x - 12) + (y - 12) * (y - 12));
                            if (distance <= 10)
                            {
                                float alpha = 1.0f - (distance / 10.0f) * 0.3f;
                                image.SetPixel(x, y, new Color(slimeColor.R, slimeColor.G, slimeColor.B, alpha));
                            }
                        }
                    }
                    
                    var texture = ImageTexture.CreateFromImage(image);
                    _sprite.Texture = texture;
                }
            }
        }

        private Color GetSlimeColor()
        {
            return SlimeVariant switch
            {
                SlimeType.Green => new Color(0.3f, 0.8f, 0.3f),
                SlimeType.Blue => new Color(0.3f, 0.3f, 0.8f),
                SlimeType.Red => new Color(0.8f, 0.3f, 0.3f),
                SlimeType.Purple => new Color(0.6f, 0.3f, 0.8f),
                SlimeType.Golden => new Color(1.0f, 0.8f, 0.2f),
                _ => Colors.Green
            };
        }

        private string GetSlimeName()
        {
            return SlimeVariant switch
            {
                SlimeType.Green => "Green Slime",
                SlimeType.Blue => "Frost Slime",
                SlimeType.Red => "Fire Slime",
                SlimeType.Purple => "Toxic Slime",
                SlimeType.Golden => "Golden Slime",
                _ => "Slime"
            };
        }

        private MonsterBehavior GetSlimeBehavior()
        {
            return SlimeVariant switch
            {
                SlimeType.Green => MonsterBehavior.Passive,      // Green slimes are harmless
                SlimeType.Blue => MonsterBehavior.Neutral,       // Blue slimes are defensive
                SlimeType.Red => MonsterBehavior.Aggressive,     // Fire slimes are aggressive
                SlimeType.Purple => MonsterBehavior.Hostile,     // Toxic slimes are dangerous
                SlimeType.Golden => MonsterBehavior.Territorial, // Golden slimes guard treasure
                _ => MonsterBehavior.Neutral
            };
        }

        protected override Color GetNameColor()
        {
            return SlimeVariant switch
            {
                SlimeType.Green => Colors.LightGreen,
                SlimeType.Blue => Colors.LightBlue,
                SlimeType.Red => Colors.LightCoral,
                SlimeType.Purple => Colors.Magenta,
                SlimeType.Golden => Colors.Gold,
                _ => Colors.White
            };
        }

        protected override void HandleIdleState(double delta)
        {
            _bounceTimer -= (float)delta;
            
            if (_bounceTimer <= 0)
            {
                StartBounce();
                _bounceTimer = GD.Randf() * 3.0f + 2.0f; // Range 2-5 seconds
            }
            
            base.HandleIdleState(delta);
        }

        protected override void HandlePatrolState(double delta)
        {
            // Slimes bounce instead of walking
            if (!_isBouncing)
            {
                StartBounce();
            }
            
            if (_isBouncing)
            {
                Velocity = _bounceDirection * MovementSpeed * 1.5f;
            }
            
            // Check if too far from spawn
            if (GlobalPosition.DistanceTo(_spawnPosition) > _territoryRadius)
            {
                _currentState = AIState.Return;
            }
        }

        protected override void HandleChaseState(double delta)
        {
            if (_target == null || !IsInstanceValid(_target))
            {
                _currentState = AIState.Idle;
                return;
            }

            float distanceToTarget = GlobalPosition.DistanceTo(_target.GlobalPosition);
            
            if (distanceToTarget <= _stats.AttackRange)
            {
                _currentState = AIState.Attack;
                return;
            }
            
            // Bounce towards target
            Vector2 direction = (_target.GlobalPosition - GlobalPosition).Normalized();
            if (!_isBouncing)
            {
                StartBounceTowards(direction);
            }
            
            if (_isBouncing)
            {
                Velocity = _bounceDirection * MovementSpeed;
            }
        }

        private void StartBounce()
        {
            var randomDirection = new Vector2(
                GD.Randf() * 2 - 1,
                GD.Randf() * 2 - 1
            ).Normalized();
            
            StartBounceTowards(randomDirection);
        }

        private void StartBounceTowards(Vector2 direction)
        {
            _bounceDirection = direction;
            _isBouncing = true;
            
            // Scale sprite to simulate bounce
            if (_sprite != null)
            {
                var tween = CreateTween();
                tween.TweenProperty(_sprite, "scale", new Vector2(1.2f, 0.8f), 0.1f);
                tween.TweenProperty(_sprite, "scale", Vector2.One, 0.2f);
                tween.TweenCallback(Callable.From(() => _isBouncing = false));
            }
            else
            {
                // Fallback if no sprite
                var timer = GetTree().CreateTimer(0.3);
                timer.Timeout += () => _isBouncing = false;
            }
        }

        protected override void AttackTarget()
        {
            if (_target == null) return;
            
            // Slime bounce attack
            Vector2 attackDirection = (_target.GlobalPosition - GlobalPosition).Normalized();
            StartBounceTowards(attackDirection);
            
            // Apply special effects based on slime type
            int damage = _stats.Attack;
            string attackMessage = $"{MonsterName} bounces at target";
            
            switch (SlimeVariant)
            {
                case SlimeType.Red:
                    damage += 5; // Fire damage
                    attackMessage += " with burning slime";
                    break;
                case SlimeType.Blue:
                    attackMessage += " with freezing slime";
                    break;
                case SlimeType.Purple:
                    damage += 3; // Poison damage
                    attackMessage += " with toxic slime";
                    break;
                case SlimeType.Golden:
                    damage += 10; // Bonus damage
                    attackMessage += " with golden power";
                    break;
            }
            
            GD.Print($"{attackMessage} for {damage} damage!");
            
            if (_target.HasMethod("TakeDamage"))
            {
                _target.Call("TakeDamage", damage);
            }
        }

        protected override void Die()
        {
            // Chance to split into smaller slimes for larger variants
            if (SlimeVariant == SlimeType.Golden && Level > 3)
            {
                SplitSlime();
            }
            
            base.Die();
        }

        private void SplitSlime()
        {
            // Create 2 smaller slimes
            for (int i = 0; i < 2; i++)
            {
                var slimeScene = GD.Load<PackedScene>("res://Scenes/Entities/Monsters/Slime.tscn");
                var smallSlime = slimeScene.Instantiate<Slime>();
                smallSlime.Level = Mathf.Max(1, Level - 2);
                smallSlime.SlimeVariant = SlimeType.Green; // Smaller slimes are green
                smallSlime.GlobalPosition = GlobalPosition + new Vector2(
                    GD.Randf() * 60 - 30,
                    GD.Randf() * 60 - 30
                );
                
                GetParent().AddChild(smallSlime);
            }
            
            GD.Print($"{MonsterName} splits into smaller slimes!");
        }
    }
}
