using Godot;
using NewWorldEvolution.Data;
using NewWorldEvolution.Core;
using NewWorldEvolution.UI;

namespace NewWorldEvolution.Entities
{
    public partial class BaseMonster : CharacterBody2D
    {
        [Export] public string MonsterName { get; set; } = "Monster";
        [Export] public int Level { get; set; } = 1;
        [Export] public MonsterBehavior Behavior { get; set; } = MonsterBehavior.Neutral;
        [Export] public float MovementSpeed { get; set; } = 50.0f;

        protected MonsterData _monsterData;
        protected MonsterStats _stats;
        protected Node2D _target;
        protected float _lastAttackTime = 0;
        protected bool _isDead = false;
        protected Vector2 _spawnPosition;
        protected float _territoryRadius = 150.0f;

        // Visual components
        protected Sprite2D _sprite;
        protected AnimationPlayer _animationPlayer;
        protected Area2D _detectionArea;
        protected CollisionShape2D _detectionShape;
        protected OverheadDisplay _overheadDisplay;

        // AI States
        public enum AIState
        {
            Idle,
            Patrol,
            Chase,
            Attack,
            Return,
            Dead
        }
        protected AIState _currentState = AIState.Idle;

        public override void _Ready()
        {
            GetSceneComponents();
            InitializeMonster();
            SetupOverheadDisplay();
            SetupDetectionArea();
            
            _spawnPosition = GlobalPosition;
            
            GD.Print($"Monster {MonsterName} (Level {Level}) spawned at {GlobalPosition}");
        }

        protected virtual void GetSceneComponents()
        {
            _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
            _animationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
            _detectionArea = GetNodeOrNull<Area2D>("DetectionArea");
            _detectionShape = _detectionArea?.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        }

        protected virtual void InitializeMonster()
        {
            // This will be overridden by specific monster classes
            _stats = new MonsterStats
            {
                Level = Level,
                MaxHealth = 100 + Level * 15,
                MaxMana = 50 + Level * 5,
                Attack = 10 + Level * 3,
                Defense = 5 + Level * 2,
                Speed = (int)MovementSpeed,
                DetectionRange = 100.0f,
                AttackRange = 50.0f,
                ExperienceReward = 10 + Level * 5
            };
            
            _stats.Health = _stats.MaxHealth;
            _stats.Mana = _stats.MaxMana;
        }

        protected virtual void SetupOverheadDisplay()
        {
            _overheadDisplay = new OverheadDisplay();
            _overheadDisplay.SetEntity(this, MonsterName, Level, GetNameColor());
            AddChild(_overheadDisplay);
        }

        protected virtual void SetupDetectionArea()
        {
            if (_detectionArea != null && _detectionShape != null)
            {
                _detectionArea.BodyEntered += OnBodyEntered;
                _detectionArea.BodyExited += OnBodyExited;
                
                // Set detection radius
                var shape = _detectionShape.Shape as CircleShape2D;
                if (shape != null)
                {
                    shape.Radius = _stats.DetectionRange;
                }
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_isDead) return;

            UpdateAI(delta);
            MoveAndSlide();
            
            // Update overhead display position
            _overheadDisplay?.UpdatePosition();
        }

        protected virtual void UpdateAI(double delta)
        {
            // Check if target is still valid and alive
            if (_target != null && IsInstanceValid(_target))
            {
                // Check if target is dead
                if (_target.HasMethod("IsAlive") && !(bool)_target.Call("IsAlive"))
                {
                    _target = null;
                    _currentState = AIState.Idle;
                    return;
                }
            }
            
            switch (_currentState)
            {
                case AIState.Idle:
                    HandleIdleState(delta);
                    break;
                case AIState.Patrol:
                    HandlePatrolState(delta);
                    break;
                case AIState.Chase:
                    HandleChaseState(delta);
                    break;
                case AIState.Attack:
                    HandleAttackState(delta);
                    break;
                case AIState.Return:
                    HandleReturnState(delta);
                    break;
            }
        }

        protected virtual void HandleIdleState(double delta)
        {
            Velocity = Vector2.Zero;
            
            // Only aggressive and hostile monsters actively look for targets
            if (Behavior == MonsterBehavior.Aggressive || Behavior == MonsterBehavior.Hostile)
            {
                var nearbyPlayer = FindNearbyPlayer();
                if (nearbyPlayer != null)
                {
                    SetTarget(nearbyPlayer);
                    _currentState = AIState.Chase;
                    GD.Print($"{MonsterName} spotted player and is now aggressive!");
                }
            }
            // Passive and neutral monsters just wait
            else if (Behavior == MonsterBehavior.Passive || Behavior == MonsterBehavior.Neutral)
            {
                // Maybe patrol randomly if neutral
                if (Behavior == MonsterBehavior.Neutral && GD.Randf() < 0.1f) // 10% chance per frame
                {
                    _currentState = AIState.Patrol;
                }
            }
        }

        protected virtual void HandlePatrolState(double delta)
        {
            // Simple random movement within territory
            if (Velocity.Length() < 10)
            {
                var randomDirection = new Vector2(
                    GD.Randf() * 2 - 1,
                    GD.Randf() * 2 - 1
                ).Normalized();
                
                Velocity = randomDirection * MovementSpeed * 0.3f;
            }
            
            // Check if too far from spawn
            if (GlobalPosition.DistanceTo(_spawnPosition) > _territoryRadius)
            {
                _currentState = AIState.Return;
            }
        }

        protected virtual void HandleChaseState(double delta)
        {
            if (_target == null || !IsInstanceValid(_target))
            {
                _currentState = AIState.Idle;
                return;
            }

            float distanceToTarget = GlobalPosition.DistanceTo(_target.GlobalPosition);
            
            // Check if in attack range
            if (distanceToTarget <= _stats.AttackRange)
            {
                _currentState = AIState.Attack;
                return;
            }
            
            // Check if target is too far away or out of territory
            if (distanceToTarget > _stats.DetectionRange * 1.5f || 
                GlobalPosition.DistanceTo(_spawnPosition) > _territoryRadius * 2)
            {
                _target = null;
                _currentState = AIState.Return;
                return;
            }
            
            // Move towards target
            Vector2 direction = (_target.GlobalPosition - GlobalPosition).Normalized();
            Velocity = direction * MovementSpeed;
        }

        protected virtual void HandleAttackState(double delta)
        {
            if (_target == null || !IsInstanceValid(_target))
            {
                _currentState = AIState.Idle;
                return;
            }

            float distanceToTarget = GlobalPosition.DistanceTo(_target.GlobalPosition);
            
            // If target moved away, chase again
            if (distanceToTarget > _stats.AttackRange * 1.2f)
            {
                _currentState = AIState.Chase;
                return;
            }
            
            // Stop moving and attack
            Velocity = Vector2.Zero;
            
            // Attack if cooldown is ready
            float timeSinceLastAttack = (float)Time.GetUnixTimeFromSystem() - _lastAttackTime;
            if (timeSinceLastAttack >= (1.0f / _stats.AttackSpeed))
            {
                AttackTarget();
                _lastAttackTime = (float)Time.GetUnixTimeFromSystem();
            }
            
            // Stay in attack state as long as target is in range and alive
            // Only switch states if target moves away or dies
        }

        protected virtual void HandleReturnState(double delta)
        {
            Vector2 directionToSpawn = (_spawnPosition - GlobalPosition).Normalized();
            Velocity = directionToSpawn * MovementSpeed;
            
            // If close enough to spawn, switch to idle
            if (GlobalPosition.DistanceTo(_spawnPosition) < 20.0f)
            {
                _currentState = AIState.Idle;
            }
        }

        protected virtual void AttackTarget()
        {
            if (_target == null) return;
            
            GD.Print($"{MonsterName} attacks target for {_stats.Attack} damage!");
            
            // Try to damage the target if it's a player
            if (_target.HasMethod("TakeDamage"))
            {
                _target.Call("TakeDamage", _stats.Attack);
            }
            
            // Play attack animation if available
            if (_animationPlayer != null && _animationPlayer.HasAnimation("attack"))
            {
                _animationPlayer.Play("attack");
            }
        }

        protected virtual Node2D FindNearbyPlayer()
        {
            var player = GameManager.Instance?.CurrentPlayer;
            if (player != null)
            {
                float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
                if (distance <= _stats.DetectionRange)
                {
                    return player;
                }
            }
            return null;
        }

        protected virtual void SetTarget(Node2D target)
        {
            _target = target;
        }

        protected virtual void OnBodyEntered(Node2D body)
        {
            // Only react to players for now
            if (body.HasMethod("TakeDamage")) // Simple way to identify players
            {
                switch (Behavior)
                {
                    case MonsterBehavior.Aggressive:
                    case MonsterBehavior.Hostile:
                        SetTarget(body);
                        _currentState = AIState.Chase;
                        GD.Print($"{MonsterName} ({Behavior}) detected player and attacks!");
                        break;
                        
                    case MonsterBehavior.Territorial:
                        // Only attack if player is in territory
                        if (GlobalPosition.DistanceTo(_spawnPosition) < _territoryRadius)
                        {
                            SetTarget(body);
                            _currentState = AIState.Chase;
                            GD.Print($"{MonsterName} defends territory from intruder!");
                        }
                        break;
                        
                    case MonsterBehavior.Neutral:
                        // Neutral monsters don't attack unless attacked first
                        GD.Print($"{MonsterName} notices player but remains neutral");
                        break;
                        
                    case MonsterBehavior.Passive:
                        // Passive monsters never attack
                        GD.Print($"{MonsterName} sees player but remains passive");
                        break;
                }
            }
        }

        protected virtual void OnBodyExited(Node2D body)
        {
            if (body == _target)
            {
                // Don't immediately lose target, but start a timer
                // This prevents rapid target switching
            }
        }

        public virtual void TakeDamage(int damage)
        {
            if (_isDead) return;
            
            _stats.Health -= damage;
            GD.Print($"{MonsterName} takes {damage} damage! Health: {_stats.Health}/{_stats.MaxHealth}");
            
            // Update overhead display
            _overheadDisplay?.ShowDamage(damage);
            _overheadDisplay?.UpdateHealthBar(_stats.Health, _stats.MaxHealth);
            
            // React to being attacked based on behavior
            if (_target == null)
            {
                var attacker = FindNearbyPlayer();
                if (attacker != null)
                {
                    switch (Behavior)
                    {
                        case MonsterBehavior.Passive:
                            // Passive monsters flee when attacked
                            GD.Print($"{MonsterName} is passive and flees from combat!");
                            _currentState = AIState.Return;
                            break;
                            
                        case MonsterBehavior.Neutral:
                            // Neutral monsters fight back when attacked
                            GD.Print($"{MonsterName} was neutral but fights back when attacked!");
                            SetTarget(attacker);
                            _currentState = AIState.Chase;
                            break;
                            
                        case MonsterBehavior.Territorial:
                        case MonsterBehavior.Aggressive:
                        case MonsterBehavior.Hostile:
                            // Already aggressive types
                            SetTarget(attacker);
                            _currentState = AIState.Chase;
                            break;
                    }
                }
            }
            
            if (_stats.Health <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            if (_isDead) return;
            
            _isDead = true;
            _currentState = AIState.Dead;
            
            GD.Print($"{MonsterName} has died!");
            
            // Award experience to nearby players
            AwardExperience();
            
            // Drop loot
            DropLoot();
            
            // Play death animation
            if (_animationPlayer != null && _animationPlayer.HasAnimation("death"))
            {
                _animationPlayer.Play("death");
                _animationPlayer.AnimationFinished += OnDeathAnimationFinished;
            }
            else
            {
                // No death animation, remove immediately
                CallDeferred(nameof(RemoveFromScene));
            }
        }

        protected virtual void AwardExperience()
        {
            var player = GameManager.Instance?.CurrentPlayer;
            if (player != null && player.HasMethod("GainExperience"))
            {
                player.Call("GainExperience", _stats.ExperienceReward);
                GD.Print($"Player gained {_stats.ExperienceReward} experience!");
            }
        }

        protected virtual void DropLoot()
        {
            // TODO: Implement loot dropping system
            GD.Print($"{MonsterName} drops loot!");
        }

        protected virtual void OnDeathAnimationFinished(StringName animName)
        {
            if (animName == "death")
            {
                RemoveFromScene();
            }
        }

        protected virtual void RemoveFromScene()
        {
            QueueFree();
        }

        protected virtual Color GetNameColor()
        {
            return Level switch
            {
                <= 5 => Colors.White,
                <= 10 => Colors.Green,
                <= 15 => Colors.Blue,
                <= 20 => Colors.Purple,
                _ => Colors.Orange
            };
        }

        // Public getters for external access
        public MonsterStats GetStats() => _stats;
        public bool IsDead() => _isDead;
        public AIState GetCurrentState() => _currentState;
    }
}
