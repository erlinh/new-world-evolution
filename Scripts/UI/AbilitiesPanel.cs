using Godot;
using NewWorldEvolution.Core;

namespace NewWorldEvolution.UI
{
    public partial class AbilitiesPanel : Panel
    {
        private Button _basicAttackButton;
        private Button _powerStrikeButton;
        private Button _quickSlashButton;
        private Button _spinAttackButton;

        // Ability cooldowns
        private float _basicAttackCooldown = 1.0f;
        private float _powerStrikeCooldown = 3.0f;
        private float _quickSlashCooldown = 2.0f;
        private float _spinAttackCooldown = 5.0f;

        // Last use times
        private float _lastBasicAttack = 0;
        private float _lastPowerStrike = 0;
        private float _lastQuickSlash = 0;
        private float _lastSpinAttack = 0;

        public override void _Ready()
        {
            GetSceneElements();
            ConnectButtons();
        }

        private void GetSceneElements()
        {
            _basicAttackButton = GetNodeOrNull<Button>("Container/AbilitiesGrid/BasicAttackButton");
            _powerStrikeButton = GetNodeOrNull<Button>("Container/AbilitiesGrid/PowerStrikeButton");
            _quickSlashButton = GetNodeOrNull<Button>("Container/AbilitiesGrid/QuickSlashButton");
            _spinAttackButton = GetNodeOrNull<Button>("Container/AbilitiesGrid/SpinAttackButton");
        }

        private void ConnectButtons()
        {
            if (_basicAttackButton != null)
                _basicAttackButton.Pressed += () => UseAbility("BasicAttack");
                
            if (_powerStrikeButton != null)
                _powerStrikeButton.Pressed += () => UseAbility("PowerStrike");
                
            if (_quickSlashButton != null)
                _quickSlashButton.Pressed += () => UseAbility("QuickSlash");
                
            if (_spinAttackButton != null)
                _spinAttackButton.Pressed += () => UseAbility("SpinAttack");
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                switch (keyEvent.Keycode)
                {
                    case Key.Q:
                        UseAbility("BasicAttack");
                        break;
                    case Key.W:
                        UseAbility("PowerStrike");
                        break;
                    case Key.E:
                        UseAbility("QuickSlash");
                        break;
                    case Key.R:
                        UseAbility("SpinAttack");
                        break;
                }
            }
        }

        public override void _Process(double delta)
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            float currentTime = (float)Time.GetUnixTimeFromSystem();
            
            // Update basic attack button
            bool basicReady = currentTime - _lastBasicAttack >= _basicAttackCooldown;
            UpdateButtonState(_basicAttackButton, basicReady, currentTime - _lastBasicAttack, _basicAttackCooldown);
            
            // Update power strike button
            bool powerReady = currentTime - _lastPowerStrike >= _powerStrikeCooldown;
            UpdateButtonState(_powerStrikeButton, powerReady, currentTime - _lastPowerStrike, _powerStrikeCooldown);
            
            // Update quick slash button
            bool quickReady = currentTime - _lastQuickSlash >= _quickSlashCooldown;
            UpdateButtonState(_quickSlashButton, quickReady, currentTime - _lastQuickSlash, _quickSlashCooldown);
            
            // Update spin attack button
            bool spinReady = currentTime - _lastSpinAttack >= _spinAttackCooldown;
            UpdateButtonState(_spinAttackButton, spinReady, currentTime - _lastSpinAttack, _spinAttackCooldown);
        }

        private void UpdateButtonState(Button button, bool ready, float timeSince, float cooldown)
        {
            if (button == null) return;
            
            if (ready)
            {
                button.Disabled = false;
                button.Modulate = Colors.White;
            }
            else
            {
                button.Disabled = true;
                button.Modulate = new Color(0.5f, 0.5f, 0.5f, 1);
                
                // Show cooldown on button text
                float remaining = cooldown - timeSince;
                string originalText = button.Text.Split('\n')[0];
                string keyBind = button.Text.Split('\n')[1];
                button.Text = $"{originalText}\n{keyBind}\n{remaining:F1}s";
            }
        }

        private void UseAbility(string abilityName)
        {
            var player = GameManager.Instance?.CurrentPlayer;
            if (player == null) return;

            float currentTime = (float)Time.GetUnixTimeFromSystem();
            
            switch (abilityName)
            {
                case "BasicAttack":
                    if (currentTime - _lastBasicAttack >= _basicAttackCooldown)
                    {
                        player.Call("UseAbility", "BasicAttack");
                        _lastBasicAttack = currentTime;
                    }
                    break;
                    
                case "PowerStrike":
                    if (currentTime - _lastPowerStrike >= _powerStrikeCooldown)
                    {
                        player.Call("UseAbility", "PowerStrike");
                        _lastPowerStrike = currentTime;
                    }
                    break;
                    
                case "QuickSlash":
                    if (currentTime - _lastQuickSlash >= _quickSlashCooldown)
                    {
                        player.Call("UseAbility", "QuickSlash");
                        _lastQuickSlash = currentTime;
                    }
                    break;
                    
                case "SpinAttack":
                    if (currentTime - _lastSpinAttack >= _spinAttackCooldown)
                    {
                        player.Call("UseAbility", "SpinAttack");
                        _lastSpinAttack = currentTime;
                    }
                    break;
            }
        }

        public bool IsAbilityReady(string abilityName)
        {
            float currentTime = (float)Time.GetUnixTimeFromSystem();
            
            return abilityName switch
            {
                "BasicAttack" => currentTime - _lastBasicAttack >= _basicAttackCooldown,
                "PowerStrike" => currentTime - _lastPowerStrike >= _powerStrikeCooldown,
                "QuickSlash" => currentTime - _lastQuickSlash >= _quickSlashCooldown,
                "SpinAttack" => currentTime - _lastSpinAttack >= _spinAttackCooldown,
                _ => false
            };
        }
    }
}
