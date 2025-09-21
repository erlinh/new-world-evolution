using Godot;
using NewWorldEvolution.Core;

namespace NewWorldEvolution.Core
{
    public partial class SceneManager : Node
    {
        public static SceneManager Instance { get; private set; }

        [Export] public string MainMenuPath = "res://Scenes/Main/MainMenu.tscn";
        [Export] public string GameWorldPath = "res://Scenes/Main/GameWorld.tscn";
        [Export] public string CharacterCreationPath = "res://Scenes/Main/CharacterCreation.tscn";

        public override void _Ready()
        {
            if (Instance == null)
            {
                Instance = this;
                ProcessMode = ProcessModeEnum.Always;
            }
            else
            {
                QueueFree();
            }
        }

        public void LoadMainMenu()
        {
            GetTree().ChangeSceneToFile(MainMenuPath);
        }

        public void LoadCharacterCreation()
        {
            GetTree().ChangeSceneToFile(CharacterCreationPath);
        }

        public void LoadGameWorld()
        {
            GetTree().ChangeSceneToFile(GameWorldPath);
        }

        public void LoadSpawnLocation(string spawnLocationName)
        {
            var spawnManager = GameManager.Instance?.GetNode<World.SpawnManager>("SpawnManager");
            if (spawnManager != null)
            {
                spawnManager.LoadSpawnLocation(spawnLocationName);
            }
        }

        public void QuitGame()
        {
            GetTree().Quit();
        }
    }
}
