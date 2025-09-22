using Godot;

namespace NewWorldEvolution.UI
{
    public partial class MainMenu : Control
    {
        public override void _Ready()
        {
            // Connect button signals if not connected in scene
        }

        private void _on_start_button_pressed()
        {
            GetTree().ChangeSceneToFile("res://Scenes/Main/CharacterCreation.tscn");
        }

        private void _on_load_button_pressed()
        {
            GD.Print("Load game functionality not yet implemented");
            // TODO: Implement save/load system
        }

        private void _on_exit_button_pressed()
        {
            GetTree().Quit();
        }
    }
}
