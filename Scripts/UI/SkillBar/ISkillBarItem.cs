using Godot;

namespace NewWorldEvolution.UI.SkillBar
{
    public interface ISkillBarItem
    {
        string GetDisplayName();
        string GetDescription();
        Texture2D GetIcon();
        bool CanActivate();
        void Activate();
        float GetCooldownTime();
        bool IsOnCooldown();
        int GetCurrentCharges();
        int GetMaxCharges();
        Color GetBorderColor();
        string GetTooltipText();
    }
}
