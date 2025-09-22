using Godot;
using NewWorldEvolution.Core;

namespace NewWorldEvolution.UI.SkillBar
{
    public class SkillBarAbility : ISkillBarItem
    {
        public string AbilityName { get; private set; }
        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public string KeyBind { get; private set; }
        public float CooldownTime { get; private set; }
        public int MaxCharges { get; private set; }
        
        private int _currentCharges;
        private float _lastUsedTime;

        public SkillBarAbility(string abilityName, string displayName, string description, string keyBind, float cooldown, int maxCharges = 1)
        {
            AbilityName = abilityName;
            DisplayName = displayName;
            Description = description;
            KeyBind = keyBind;
            CooldownTime = cooldown;
            MaxCharges = maxCharges;
            _currentCharges = MaxCharges;
            _lastUsedTime = 0;
        }

        public string GetId() => AbilityName;
        public string GetDisplayName() => DisplayName;
        public string GetDescription() => Description;

        public Texture2D GetIcon()
        {
            // Generate colored icon based on ability type
            var image = Image.CreateEmpty(32, 32, false, Image.Format.Rgb8);
            var color = GetAbilityColor();
            
            // Create a simple pattern for each ability
            switch (AbilityName)
            {
                case "BasicAttack":
                    CreateBasicAttackIcon(image, color);
                    break;
                case "PowerStrike":
                    CreatePowerStrikeIcon(image, color);
                    break;
                case "QuickSlash":
                    CreateQuickSlashIcon(image, color);
                    break;
                case "SpinAttack":
                    CreateSpinAttackIcon(image, color);
                    break;
                default:
                    image.Fill(color);
                    break;
            }
            
            return ImageTexture.CreateFromImage(image);
        }

        private void CreateBasicAttackIcon(Image image, Color color)
        {
            // Simple sword shape
            for (int x = 14; x <= 18; x++)
            {
                for (int y = 5; y <= 25; y++)
                {
                    image.SetPixel(x, y, color);
                }
            }
            // Crossguard
            for (int x = 10; x <= 22; x++)
            {
                for (int y = 20; y <= 22; y++)
                {
                    image.SetPixel(x, y, color);
                }
            }
        }

        private void CreatePowerStrikeIcon(Image image, Color color)
        {
            // Large impact shape
            for (int x = 8; x <= 24; x++)
            {
                for (int y = 8; y <= 24; y++)
                {
                    float distance = Mathf.Sqrt((x - 16) * (x - 16) + (y - 16) * (y - 16));
                    if (distance <= 8 && distance >= 6)
                    {
                        image.SetPixel(x, y, color);
                    }
                }
            }
        }

        private void CreateQuickSlashIcon(Image image, Color color)
        {
            // Multiple slash lines
            for (int i = 0; i < 3; i++)
            {
                int startX = 6 + i * 3;
                int startY = 8 + i * 2;
                for (int j = 0; j < 16; j++)
                {
                    int x = startX + j;
                    int y = startY + j;
                    if (x < 32 && y < 32)
                    {
                        image.SetPixel(x, y, color);
                        if (x + 1 < 32) image.SetPixel(x + 1, y, color);
                    }
                }
            }
        }

        private void CreateSpinAttackIcon(Image image, Color color)
        {
            // Circular pattern
            for (int angle = 0; angle < 360; angle += 30)
            {
                float rad = angle * Mathf.Pi / 180;
                int x1 = 16 + (int)(8 * Mathf.Cos(rad));
                int y1 = 16 + (int)(8 * Mathf.Sin(rad));
                int x2 = 16 + (int)(12 * Mathf.Cos(rad));
                int y2 = 16 + (int)(12 * Mathf.Sin(rad));
                
                // Draw line from center outward
                for (int t = 0; t <= 10; t++)
                {
                    int x = x1 + (x2 - x1) * t / 10;
                    int y = y1 + (y2 - y1) * t / 10;
                    if (x >= 0 && x < 32 && y >= 0 && y < 32)
                    {
                        image.SetPixel(x, y, color);
                    }
                }
            }
        }

        private Color GetAbilityColor()
        {
            return AbilityName switch
            {
                "BasicAttack" => new Color(1.0f, 1.0f, 0.3f), // Yellow
                "PowerStrike" => new Color(1.0f, 0.3f, 0.3f), // Red
                "QuickSlash" => new Color(0.3f, 1.0f, 1.0f),  // Cyan
                "SpinAttack" => new Color(1.0f, 0.6f, 0.3f),  // Orange
                _ => Colors.Gray
            };
        }

        public bool CanActivate()
        {
            return _currentCharges > 0 && !IsOnCooldown();
        }

        public void Activate()
        {
            if (!CanActivate()) return;

            var player = GameManager.Instance?.CurrentPlayer;
            if (player != null && player.HasMethod("UseAbility"))
            {
                player.Call("UseAbility", AbilityName);
                _currentCharges--;
                _lastUsedTime = (float)Time.GetUnixTimeFromSystem();
                
                // Start charge regeneration
                if (_currentCharges < MaxCharges)
                {
                    RegenerateCharge();
                }
            }
        }

        private void RegenerateCharge()
        {
            // Simple charge regeneration after cooldown
            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree != null)
            {
                var timer = tree.CreateTimer(CooldownTime);
                timer.Timeout += () => {
                    if (_currentCharges < MaxCharges)
                    {
                        _currentCharges++;
                    }
                };
            }
        }

        public float GetCooldownTime() => CooldownTime;

        public bool IsOnCooldown()
        {
            float timeSinceLastUse = (float)Time.GetUnixTimeFromSystem() - _lastUsedTime;
            return timeSinceLastUse < CooldownTime;
        }

        public int GetCurrentCharges() => _currentCharges;
        public int GetMaxCharges() => MaxCharges;

        public Color GetBorderColor()
        {
            if (!CanActivate())
                return Colors.Red;
            return GetAbilityColor();
        }

        public string GetTooltipText()
        {
            string tooltip = $"[center][b]{GetDisplayName()}[/b][/center]\n";
            tooltip += $"[color=gray]{GetDescription()}[/color]\n";
            tooltip += $"[color=yellow]Keybind: {KeyBind}[/color]\n";
            tooltip += $"[color=cyan]Cooldown:[/color] {CooldownTime:F1}s\n";
            
            if (MaxCharges > 1)
            {
                tooltip += $"[color=cyan]Charges:[/color] {GetCurrentCharges()}/{GetMaxCharges()}\n";
            }

            if (IsOnCooldown())
            {
                float remaining = CooldownTime - ((float)Time.GetUnixTimeFromSystem() - _lastUsedTime);
                tooltip += $"[color=orange]Cooldown remaining:[/color] {remaining:F1}s";
            }

            return tooltip;
        }
    }
}
