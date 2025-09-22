using Godot;
using NewWorldEvolution.Entities;

namespace NewWorldEvolution.UI
{
    public partial class TargetPanel : Panel
    {
        private Label _nameLabel;
        private Label _levelLabel;
        private Label _typeLabel;
        private Label _healthLabel;
        private ProgressBar _healthBar;
        private Label _healthText;
        private Label _statusLabel;

        private BaseMonster _currentTarget;
        private Timer _updateTimer;

        public override void _Ready()
        {
            GetSceneElements();
            SetupUpdateTimer();
            
            // Hide panel initially
            Visible = false;
        }

        private void GetSceneElements()
        {
            _nameLabel = GetNodeOrNull<Label>("Container/TargetInfo/NameLabel");
            _levelLabel = GetNodeOrNull<Label>("Container/TargetInfo/LevelLabel");
            _typeLabel = GetNodeOrNull<Label>("Container/TargetInfo/TypeLabel");
            _healthLabel = GetNodeOrNull<Label>("Container/HealthContainer/HealthLabel");
            _healthBar = GetNodeOrNull<ProgressBar>("Container/HealthContainer/HealthBar");
            _healthText = GetNodeOrNull<Label>("Container/HealthContainer/HealthText");
            _statusLabel = GetNodeOrNull<Label>("Container/StatusLabel");
        }

        private void SetupUpdateTimer()
        {
            _updateTimer = new Timer();
            _updateTimer.WaitTime = 0.1f; // Update 10 times per second
            _updateTimer.Autostart = false;
            _updateTimer.Timeout += UpdateTargetInfo;
            AddChild(_updateTimer);
        }

        public void SetTarget(BaseMonster target)
        {
            _currentTarget = target;
            
            if (target != null)
            {
                Visible = true;
                _updateTimer.Start();
                UpdateTargetInfo();
            }
            else
            {
                ClearTarget();
            }
        }

        public void ClearTarget()
        {
            _currentTarget = null;
            Visible = false;
            _updateTimer.Stop();
        }

        private void UpdateTargetInfo()
        {
            if (_currentTarget == null || !IsInstanceValid(_currentTarget))
            {
                ClearTarget();
                return;
            }

            var stats = _currentTarget.GetStats();
            if (stats == null) return;

            // Update name and level
            if (_nameLabel != null)
                _nameLabel.Text = _currentTarget.MonsterName;
                
            if (_levelLabel != null)
                _levelLabel.Text = $"Level: {stats.Level}";

            // Update type and behavior info
            if (_typeLabel != null)
            {
                string typeInfo = GetMonsterTypeInfo();
                _typeLabel.Text = typeInfo;
            }

            // Update health bar and text
            if (_healthBar != null)
            {
                _healthBar.MaxValue = stats.MaxHealth;
                _healthBar.Value = stats.Health;
                
                // Color code health bar
                float healthPercent = (float)stats.Health / stats.MaxHealth;
                var healthStyle = new StyleBoxFlat();
                
                if (healthPercent > 0.7f)
                    healthStyle.BgColor = Colors.Green;
                else if (healthPercent > 0.3f)
                    healthStyle.BgColor = Colors.Yellow;
                else
                    healthStyle.BgColor = Colors.Red;
                    
                healthStyle.CornerRadiusTopLeft = 4;
                healthStyle.CornerRadiusTopRight = 4;
                healthStyle.CornerRadiusBottomLeft = 4;
                healthStyle.CornerRadiusBottomRight = 4;
                
                _healthBar.AddThemeStyleboxOverride("fill", healthStyle);
            }

            if (_healthText != null)
                _healthText.Text = $"{stats.Health}/{stats.MaxHealth}";

            // Update status
            if (_statusLabel != null)
            {
                string status = GetMonsterStatus();
                _statusLabel.Text = $"State: {status}";
            }
        }

        private string GetMonsterTypeInfo()
        {
            if (_currentTarget == null) return "Unknown";
            
            // Try to determine monster type from class name
            string className = _currentTarget.GetType().Name;
            string behavior = _currentTarget.Behavior.ToString();
            
            string typeInfo = className switch
            {
                "Slime" => "Magical Ooze",
                "Goblin" => "Humanoid",
                "Wolf" => "Beast",
                _ => "Monster"
            };
            
            return $"{typeInfo} - {behavior}";
        }

        private string GetMonsterStatus()
        {
            if (_currentTarget == null) return "Unknown";
            
            var state = _currentTarget.GetCurrentState();
            var stats = _currentTarget.GetStats();
            
            string statusText = state switch
            {
                BaseMonster.AIState.Idle => "Idle",
                BaseMonster.AIState.Patrol => "Patrolling",
                BaseMonster.AIState.Chase => "Chasing",
                BaseMonster.AIState.Attack => "Attacking!",
                BaseMonster.AIState.Return => "Returning",
                BaseMonster.AIState.Dead => "Dead",
                _ => "Unknown"
            };
            
            // Add health status modifier
            if (stats != null)
            {
                float healthPercent = (float)stats.Health / stats.MaxHealth;
                if (healthPercent < 0.3f)
                    statusText += " (Critical)";
                else if (healthPercent < 0.6f)
                    statusText += " (Wounded)";
            }
            
            return statusText;
        }

        public BaseMonster GetCurrentTarget()
        {
            return _currentTarget;
        }

        public bool HasTarget()
        {
            return _currentTarget != null && IsInstanceValid(_currentTarget);
        }
    }
}
