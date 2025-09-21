using Godot;
using NewWorldEvolution.Core;

namespace NewWorldEvolution.UI
{
    public partial class MainMenu : Control
    {
        private OptionButton _raceSelection;
        private Button _startGameButton;
        private Button _loadGameButton;
        private Button _exitButton;
        private RichTextLabel _raceDescription;
        private VBoxContainer _evolutionPreview;

        private readonly string[] _availableRaces = { "Human", "Goblin", "Spider", "Demon", "Vampire" };
        private string _selectedRace = "Human";

        public override void _Ready()
        {
            SetupUI();
            ConnectSignals();
            UpdateRaceSelection();
        }

        private void SetupUI()
        {
            // This would typically be done in the scene editor, but here's the code structure
            _raceSelection = GetNode<OptionButton>("VBox/RaceContainer/RaceSelection");
            _startGameButton = GetNode<Button>("VBox/ButtonContainer/StartGameButton");
            _loadGameButton = GetNode<Button>("VBox/ButtonContainer/LoadGameButton");
            _exitButton = GetNode<Button>("VBox/ButtonContainer/ExitButton");
            _raceDescription = GetNode<RichTextLabel>("VBox/DescriptionContainer/RaceDescription");
            _evolutionPreview = GetNode<VBoxContainer>("VBox/EvolutionContainer/EvolutionPreview");

            // Populate race selection
            foreach (string race in _availableRaces)
            {
                _raceSelection.AddItem(race);
            }
        }

        private void ConnectSignals()
        {
            _raceSelection.ItemSelected += OnRaceSelected;
            _startGameButton.Pressed += OnStartGamePressed;
            _loadGameButton.Pressed += OnLoadGamePressed;
            _exitButton.Pressed += OnExitPressed;
        }

        private void OnRaceSelected(long index)
        {
            _selectedRace = _availableRaces[index];
            UpdateRaceSelection();
        }

        private void UpdateRaceSelection()
        {
            var raceData = GameManager.Instance?.GetRaceData(_selectedRace);
            if (raceData != null)
            {
                _raceDescription.Text = $"[b]{raceData.Name}[/b]\n\n{raceData.Description}\n\n";
                _raceDescription.Text += "[b]Base Stats:[/b]\n";
                
                foreach (var stat in raceData.BaseStats)
                {
                    _raceDescription.Text += $"{stat.Key}: {stat.Value}\n";
                }

                _raceDescription.Text += $"\n[b]Can Evolve:[/b] {(raceData.CanEvolve ? "Yes" : "No (Uses Professions)")}\n";
                _raceDescription.Text += $"[b]Starting Skills:[/b] {string.Join(", ", raceData.StartingSkills)}";

                UpdateEvolutionPreview(raceData);
            }
        }

        private void UpdateEvolutionPreview(Data.RaceData raceData)
        {
            // Clear existing preview
            foreach (Node child in _evolutionPreview.GetChildren())
            {
                child.QueueFree();
            }

            if (raceData.CanEvolve && raceData.EvolutionPaths.Count > 0)
            {
                var titleLabel = new Label();
                titleLabel.Text = "Evolution Paths:";
                titleLabel.AddThemeStyleboxOverride("normal", new StyleBoxFlat());
                _evolutionPreview.AddChild(titleLabel);

                foreach (var evolution in raceData.EvolutionPaths)
                {
                    var evolutionLabel = new Label();
                    evolutionLabel.Text = $"• {evolution.Key}: {evolution.Value.Description}";
                    evolutionLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                    _evolutionPreview.AddChild(evolutionLabel);
                }
            }
            else if (raceData.ProfessionPaths.Count > 0)
            {
                var titleLabel = new Label();
                titleLabel.Text = "Profession Paths:";
                titleLabel.AddThemeStyleboxOverride("normal", new StyleBoxFlat());
                _evolutionPreview.AddChild(titleLabel);

                foreach (var profession in raceData.ProfessionPaths)
                {
                    var professionLabel = new Label();
                    professionLabel.Text = $"• {profession.Key}: {profession.Value.Description}";
                    professionLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                    _evolutionPreview.AddChild(professionLabel);
                }
            }
        }

        private void OnStartGamePressed()
        {
            GD.Print("Going to character creation...");
            
            // Transition to character creation
            GetTree().ChangeSceneToFile("res://Scenes/Main/CharacterCreation.tscn");
        }

        private void OnLoadGamePressed()
        {
            GD.Print("Load game functionality not yet implemented");
            // TODO: Implement save/load system
        }

        private void OnExitPressed()
        {
            GetTree().Quit();
        }
    }
}
