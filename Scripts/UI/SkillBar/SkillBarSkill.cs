using Godot;
using NewWorldEvolution.Data;
using NewWorldEvolution.Core;

namespace NewWorldEvolution.UI.SkillBar
{
    public class SkillBarSkill : ISkillBarItem
    {
        public SkillData SkillData { get; private set; }
        public int CurrentLevel { get; set; }
        
        private float _lastUsedTime = 0;
        private int _currentCharges;

        public SkillBarSkill(SkillData skillData, int level = 1)
        {
            SkillData = skillData;
            CurrentLevel = level;
            _currentCharges = GetMaxCharges();
        }

        public string GetDisplayName()
        {
            return SkillData.Name;
        }

        public string GetDescription()
        {
            return SkillData.Description ?? "No description available.";
        }

        public Texture2D GetIcon()
        {
            // Try to load skill icon, fallback to default
            // Note: IconPath property needs to be added to SkillData
            // if (!string.IsNullOrEmpty(SkillData.IconPath))
            // {
            //     var icon = GD.Load<Texture2D>(SkillData.IconPath);
            //     if (icon != null) return icon;
            // }

            // Generate a simple colored square as fallback
            var image = Image.CreateEmpty(32, 32, false, Image.Format.Rgb8);
            var color = GetSkillCategoryColor();
            image.Fill(color);
            
            var texture = ImageTexture.CreateFromImage(image);
            return texture;
        }

        public bool CanActivate()
        {
            // Check cooldown
            if (IsOnCooldown()) return false;

            // Check charges
            if (_currentCharges <= 0) return false;

            // Check if player has required level
            var player = GameManager.Instance?.CurrentPlayer;
            if (player != null)
            {
                var skillManager = player.GetNode<Skills.SkillManager>("SkillManager");
                if (skillManager != null)
                {
                    int playerSkillLevel = skillManager.GetSkillLevel(SkillData.Name);
                    return playerSkillLevel > 0;
                }
            }

            return false;
        }

        public void Activate()
        {
            if (!CanActivate()) return;

            GD.Print($"Activating skill: {SkillData.Name} (Level {CurrentLevel})");

            // Consume charge
            _currentCharges--;
            _lastUsedTime = (float)Time.GetUnixTimeFromSystem();

            // Apply skill effects
            ApplySkillEffects();

            // Start charge regeneration if needed
            if (_currentCharges < GetMaxCharges())
            {
                StartChargeRegeneration();
            }
        }

        public float GetCooldownTime()
        {
            // Base cooldown could be defined in skill data
            // For now, use a simple formula based on skill type
            return SkillData.Category switch
            {
                SkillCategory.Combat => 3.0f,
                SkillCategory.Magic => 5.0f,
                SkillCategory.Crafting => 1.0f,
                SkillCategory.Social => 2.0f,
                SkillCategory.Survival => 4.0f,
                SkillCategory.Unique => 10.0f,
                _ => 2.0f
            };
        }

        public bool IsOnCooldown()
        {
            float timeSinceLastUse = (float)Time.GetUnixTimeFromSystem() - _lastUsedTime;
            return timeSinceLastUse < GetCooldownTime();
        }

        public int GetCurrentCharges()
        {
            return _currentCharges;
        }

        public int GetMaxCharges()
        {
            // Skills could have multiple charges based on level
            return SkillData.Category switch
            {
                SkillCategory.Magic => CurrentLevel / 2 + 1, // 1-3 charges
                SkillCategory.Combat => 1, // Always 1 charge
                SkillCategory.Crafting => CurrentLevel + 2, // 3-7 charges
                _ => 1
            };
        }

        public Color GetBorderColor()
        {
            if (!CanActivate())
                return new Color(0.5f, 0.5f, 0.5f, 0.8f); // Gray for unusable

            if (IsOnCooldown())
                return new Color(1.0f, 0.5f, 0.0f, 0.8f); // Orange for cooldown

            return GetSkillCategoryColor(); // Category color when ready
        }

        public string GetTooltipText()
        {
            string tooltip = $"[b]{SkillData.Name}[/b] (Level {CurrentLevel})\n";
            tooltip += $"{GetDescription()}\n\n";
            
            if (SkillData.Requirements != null && SkillData.Requirements.Count > 0)
            {
                tooltip += "[color=yellow]Requirements:[/color]\n";
                foreach (var req in SkillData.Requirements)
                {
                    tooltip += $"• {req.Key}: {req.Value}\n";
                }
                tooltip += "\n";
            }

            tooltip += $"[color=cyan]Cooldown:[/color] {GetCooldownTime():F1}s\n";
            tooltip += $"[color=cyan]Charges:[/color] {GetCurrentCharges()}/{GetMaxCharges()}\n";

            if (IsOnCooldown())
            {
                float remaining = GetCooldownTime() - ((float)Time.GetUnixTimeFromSystem() - _lastUsedTime);
                tooltip += $"[color=orange]Cooldown remaining:[/color] {remaining:F1}s";
            }

            return tooltip;
        }

        private Color GetSkillCategoryColor()
        {
            return SkillData.Category switch
            {
                SkillCategory.Combat => new Color(0.8f, 0.2f, 0.2f), // Red
                SkillCategory.Magic => new Color(0.2f, 0.2f, 0.8f), // Blue
                SkillCategory.Crafting => new Color(0.8f, 0.6f, 0.2f), // Orange
                SkillCategory.Social => new Color(0.2f, 0.8f, 0.2f), // Green
                SkillCategory.Survival => new Color(0.6f, 0.4f, 0.2f), // Brown
                SkillCategory.Unique => new Color(0.8f, 0.2f, 0.8f), // Purple
                _ => new Color(0.5f, 0.5f, 0.5f) // Gray
            };
        }

        private void ApplySkillEffects()
        {
            var player = GameManager.Instance?.CurrentPlayer;
            if (player == null) return;

            // Apply effects based on skill type
            switch (SkillData.Category)
            {
                case SkillCategory.Combat:
                    ApplyCombatSkillEffect(player);
                    break;
                case SkillCategory.Magic:
                    ApplyMagicSkillEffect(player);
                    break;
                case SkillCategory.Crafting:
                    ApplyCraftingSkillEffect(player);
                    break;
                case SkillCategory.Social:
                    ApplySocialSkillEffect(player);
                    break;
                case SkillCategory.Survival:
                    ApplySurvivalSkillEffect(player);
                    break;
            }
        }

        private void ApplyCombatSkillEffect(Player.PlayerController player)
        {
            // Example combat effects
            switch (SkillData.Name.ToLower())
            {
                case "basicswordplay":
                    GD.Print($"Performed basic sword attack! (Damage +{CurrentLevel * 5})");
                    break;
                case "archery":
                    GD.Print($"Shot an arrow! (Range +{CurrentLevel * 10})");
                    break;
                default:
                    GD.Print($"Used combat skill: {SkillData.Name}");
                    break;
            }
        }

        private void ApplyMagicSkillEffect(Player.PlayerController player)
        {
            // Example magic effects
            switch (SkillData.Name.ToLower())
            {
                case "basicmagic":
                    var manaRegenAmount = CurrentLevel * 10;
                    player.Stats.Mana = Mathf.Min(player.Stats.Mana + manaRegenAmount, player.Stats.MaxMana);
                    GD.Print($"Cast basic magic spell! Restored {manaRegenAmount} mana.");
                    break;
                case "healing":
                    var healAmount = CurrentLevel * 15;
                    player.Stats.Health = Mathf.Min(player.Stats.Health + healAmount, player.Stats.MaxHealth);
                    GD.Print($"Cast healing spell! Restored {healAmount} health.");
                    break;
                default:
                    GD.Print($"Cast magic spell: {SkillData.Name}");
                    break;
            }
        }

        private void ApplyCraftingSkillEffect(Player.PlayerController player)
        {
            switch (SkillData.Name.ToLower())
            {
                case "basiccrafting":
                    GD.Print($"Crafted basic item! (Quality +{CurrentLevel * 2})");
                    break;
                default:
                    GD.Print($"Used crafting skill: {SkillData.Name}");
                    break;
            }
        }

        private void ApplySocialSkillEffect(Player.PlayerController player)
        {
            GD.Print($"Used social skill: {SkillData.Name} (Influence +{CurrentLevel})");
        }

        private void ApplySurvivalSkillEffect(Player.PlayerController player)
        {
            GD.Print($"Used survival skill: {SkillData.Name} (Efficiency +{CurrentLevel * 3})");
        }

        private void StartChargeRegeneration()
        {
            // This would ideally be handled by a game timer system
            // For now, just regenerate charges over time
            var timer = new Timer();
            timer.Timeout += () => {
                _currentCharges = Mathf.Min(_currentCharges + 1, GetMaxCharges());
                if (_currentCharges < GetMaxCharges())
                {
                    timer.Start(5.0f); // Regenerate 1 charge every 5 seconds
                }
                else
                {
                    timer.QueueFree();
                }
            };
            timer.OneShot = true;
            timer.Start(5.0f);
            
            // Add timer to scene tree
            GameManager.Instance?.GetTree().CurrentScene.AddChild(timer);
        }
    }
}
