using Godot;
using NewWorldEvolution.Data;

namespace NewWorldEvolution.Entities.Monsters
{
    public partial class Goblin : BaseMonster
    {
        [Export] public GoblinType GoblinVariant { get; set; } = GoblinType.Scout;
        
        private float _lastShoutTime = 0;
        private bool _hasCalledForHelp = false;
        private Vector2 _lastKnownPlayerPosition;

        public enum GoblinType
        {
            Scout,      // Fast, weak, calls for help
            Warrior,    // Balanced stats, aggressive
            Shaman,     // Magic attacks, support spells
            Chief,      // Strong, commands others
            Berserker   // High damage, low defense
        }

        protected override void InitializeMonster()
        {
            SetupGoblinStats();
            SetupGoblinAppearance();
            
            MonsterName = GetGoblinName();
            Behavior = GetGoblinBehavior();
            _territoryRadius = 200.0f; // Goblins patrol wider areas
        }

        private void SetupGoblinStats()
        {
            var baseHealth = 80;
            var baseAttack = 15;
            var baseDefense = 8;
            var baseSpeed = 60f;
            var baseExp = 12;

            // Modify stats based on goblin type
            switch (GoblinVariant)
            {
                case GoblinType.Scout:
                    baseHealth = 60;
                    baseAttack = 10;
                    baseDefense = 5;
                    baseSpeed = 80f;
                    baseExp = 8;
                    break;
                case GoblinType.Warrior:
                    // Default balanced stats
                    break;
                case GoblinType.Shaman:
                    baseHealth = 70;
                    baseAttack = 20; // Magic damage
                    baseDefense = 6;
                    baseSpeed = 50f;
                    baseExp = 18;
                    break;
                case GoblinType.Chief:
                    baseHealth = 120;
                    baseAttack = 25;
                    baseDefense = 15;
                    baseSpeed = 65f;
                    baseExp = 35;
                    break;
                case GoblinType.Berserker:
                    baseHealth = 90;
                    baseAttack = 30;
                    baseDefense = 3;
                    baseSpeed = 70f;
                    baseExp = 20;
                    break;
            }

            _stats = new MonsterStats
            {
                Level = Level,
                MaxHealth = baseHealth + Level * 15,
                MaxMana = (GoblinVariant == GoblinType.Shaman ? 80 : 30) + Level * 5,
                Attack = baseAttack + Level * 3,
                Defense = baseDefense + Level * 2,
                Speed = (int)baseSpeed,
                AttackSpeed = GoblinVariant == GoblinType.Berserker ? 1.3f : 1.0f,
                DetectionRange = GoblinVariant == GoblinType.Scout ? 120.0f : 100.0f,
                AttackRange = GoblinVariant == GoblinType.Shaman ? 80.0f : 40.0f,
                ExperienceReward = baseExp + Level * 4
            };
            
            _stats.Health = _stats.MaxHealth;
            _stats.Mana = _stats.MaxMana;
            
            MovementSpeed = _stats.Speed;
        }

        private void SetupGoblinAppearance()
        {
            if (_sprite != null)
            {
                Color goblinColor = GetGoblinColor();
                _sprite.Modulate = goblinColor;
                
                // Create a simple goblin texture if none exists
                if (_sprite.Texture == null)
                {
                    var image = Image.CreateEmpty(20, 28, false, Image.Format.Rgba8);
                    
                    // Draw a simple goblin shape
                    for (int x = 0; x < 20; x++)
                    {
                        for (int y = 0; y < 28; y++)
                        {
                            // Simple humanoid shape
                            if ((x >= 8 && x <= 12 && y >= 4 && y <= 20) || // Body
                                (x >= 6 && x <= 14 && y >= 0 && y <= 8) ||   // Head
                                (y >= 16 && y <= 27 && (x == 6 || x == 14))) // Legs
                            {
                                image.SetPixel(x, y, goblinColor);
                            }
                        }
                    }
                    
                    var texture = ImageTexture.CreateFromImage(image);
                    _sprite.Texture = texture;
                }
            }
        }

        private Color GetGoblinColor()
        {
            return GoblinVariant switch
            {
                GoblinType.Scout => new Color(0.6f, 0.8f, 0.4f),      // Light green
                GoblinType.Warrior => new Color(0.4f, 0.6f, 0.3f),   // Dark green
                GoblinType.Shaman => new Color(0.6f, 0.4f, 0.8f),    // Purple
                GoblinType.Chief => new Color(0.8f, 0.6f, 0.2f),     // Gold
                GoblinType.Berserker => new Color(0.8f, 0.3f, 0.3f), // Red
                _ => Colors.Green
            };
        }

        private string GetGoblinName()
        {
            return GoblinVariant switch
            {
                GoblinType.Scout => "Goblin Scout",
                GoblinType.Warrior => "Goblin Warrior", 
                GoblinType.Shaman => "Goblin Shaman",
                GoblinType.Chief => "Goblin Chief",
                GoblinType.Berserker => "Goblin Berserker",
                _ => "Goblin"
            };
        }

        private MonsterBehavior GetGoblinBehavior()
        {
            return GoblinVariant switch
            {
                GoblinType.Scout => MonsterBehavior.Neutral,     // Scouts avoid combat unless cornered
                GoblinType.Warrior => MonsterBehavior.Aggressive, // Warriors seek combat
                GoblinType.Shaman => MonsterBehavior.Territorial, // Shamans defend their area
                GoblinType.Chief => MonsterBehavior.Hostile,     // Chiefs are always dangerous
                GoblinType.Berserker => MonsterBehavior.Aggressive, // Berserkers love combat
                _ => MonsterBehavior.Aggressive
            };
        }

        protected override Color GetNameColor()
        {
            return GoblinVariant switch
            {
                GoblinType.Scout => Colors.White,
                GoblinType.Warrior => Colors.LightGreen,
                GoblinType.Shaman => Colors.Magenta,
                GoblinType.Chief => Colors.Orange,
                GoblinType.Berserker => Colors.Red,
                _ => Colors.White
            };
        }

        protected override void HandleChaseState(double delta)
        {
            // Call for help if this is a scout and hasn't called yet
            if (GoblinVariant == GoblinType.Scout && !_hasCalledForHelp)
            {
                CallForHelp();
                _hasCalledForHelp = true;
            }
            
            // Remember player position for tactics
            if (_target != null)
            {
                _lastKnownPlayerPosition = _target.GlobalPosition;
            }
            
            base.HandleChaseState(delta);
        }

        protected override void AttackTarget()
        {
            if (_target == null) return;
            
            int damage = _stats.Attack;
            string attackMessage = $"{MonsterName}";
            
            switch (GoblinVariant)
            {
                case GoblinType.Scout:
                    // Quick, weak attacks
                    attackMessage += " makes a quick strike";
                    break;
                    
                case GoblinType.Warrior:
                    // Standard melee attack
                    attackMessage += " swings their weapon";
                    break;
                    
                case GoblinType.Shaman:
                    // Magic attack
                    if (_stats.Mana >= 10)
                    {
                        damage += 10;
                        _stats.Mana -= 10;
                        attackMessage += " casts a dark bolt";
                        
                        // Visual effect for magic
                        ShowMagicEffect();
                    }
                    else
                    {
                        attackMessage += " swipes with their staff";
                    }
                    break;
                    
                case GoblinType.Chief:
                    // Powerful attack that inspires nearby goblins
                    damage += 5;
                    attackMessage += " strikes with authority";
                    InspireNearbyGoblins();
                    break;
                    
                case GoblinType.Berserker:
                    // High damage, chance for double attack
                    damage += 8;
                    if (GD.Randf() < 0.3f) // 30% chance
                    {
                        damage *= 2;
                        attackMessage += " goes into a rage";
                        
                        // Take damage from berserking
                        TakeDamage(5);
                    }
                    else
                    {
                        attackMessage += " attacks wildly";
                    }
                    break;
            }
            
            GD.Print($"{attackMessage} for {damage} damage!");
            
            if (_target.HasMethod("TakeDamage"))
            {
                _target.Call("TakeDamage", damage);
            }
            
            // Play attack animation if available
            if (_animationPlayer != null && _animationPlayer.HasAnimation("attack"))
            {
                _animationPlayer.Play("attack");
            }
        }

        private void CallForHelp()
        {
            GD.Print($"{MonsterName} calls for help!");
            
            // Find nearby goblins and alert them
            var spaceState = GetWorld2D().DirectSpaceState;
            var query = new PhysicsShapeQueryParameters2D();
            
            var circle = new CircleShape2D();
            circle.Radius = 150.0f;
            query.Shape = circle;
            query.Transform = new Transform2D(0, GlobalPosition);
            query.CollisionMask = 1; // Assuming monsters are on layer 1
            
            var results = spaceState.IntersectShape(query, 10);
            
            foreach (var result in results)
            {
                if (result.TryGetValue("collider", out var collider) && 
                    collider.Obj is Goblin otherGoblin && 
                    otherGoblin != this)
                {
                    // Alert the other goblin
                    if (otherGoblin._target == null && _target != null)
                    {
                        otherGoblin.SetTarget(_target);
                        otherGoblin._currentState = AIState.Chase;
                        GD.Print($"{otherGoblin.MonsterName} responds to the call!");
                    }
                }
            }
        }

        private void ShowMagicEffect()
        {
            // Create a simple particle effect for magic
            // This is a placeholder - in a real game you'd have proper particle systems
            GD.Print("✨ Magic effect! ✨");
        }

        private void InspireNearbyGoblins()
        {
            // Buff nearby goblins
            var spaceState = GetWorld2D().DirectSpaceState;
            var query = new PhysicsShapeQueryParameters2D();
            
            var circle = new CircleShape2D();
            circle.Radius = 100.0f;
            query.Shape = circle;
            query.Transform = new Transform2D(0, GlobalPosition);
            
            var results = spaceState.IntersectShape(query, 5);
            
            foreach (var result in results)
            {
                if (result.TryGetValue("collider", out var collider) && 
                    collider.Obj is Goblin otherGoblin && 
                    otherGoblin != this)
                {
                    // Temporary damage boost
                    otherGoblin._stats.Attack += 5;
                    GD.Print($"{otherGoblin.MonsterName} is inspired by the chief!");
                    
                    // Remove the buff after 10 seconds
                    var timer = GetTree().CreateTimer(10.0);
                    timer.Timeout += () => {
                        if (IsInstanceValid(otherGoblin))
                        {
                            otherGoblin._stats.Attack -= 5;
                        }
                    };
                }
            }
        }

        protected override void HandleReturnState(double delta)
        {
            // Goblins are more persistent than other monsters
            if (GoblinVariant == GoblinType.Scout && _target != null)
            {
                // Scouts try to track the player longer
                float distanceToLastKnownPosition = GlobalPosition.DistanceTo(_lastKnownPlayerPosition);
                if (distanceToLastKnownPosition > 20.0f)
                {
                    Vector2 direction = (_lastKnownPlayerPosition - GlobalPosition).Normalized();
                    Velocity = direction * MovementSpeed * 0.7f;
                    return;
                }
            }
            
            base.HandleReturnState(delta);
        }

        public override void TakeDamage(int damage)
        {
            base.TakeDamage(damage);
            
            // Goblins become more aggressive when hurt
            if (_stats.Health < _stats.MaxHealth * 0.5f)
            {
                Behavior = MonsterBehavior.Hostile;
                _stats.AttackSpeed += 0.2f; // Attack faster when wounded
            }
        }
    }
}
