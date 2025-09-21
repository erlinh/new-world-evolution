using Godot;
using NewWorldEvolution.Core;
using NewWorldEvolution.Data;

namespace NewWorldEvolution.UI
{
    public partial class CharacterCreation : Control
    {
        private LineEdit _nameInput;
        private Button _generateNameButton;
        private Button _maleButton;
        private Button _femaleButton;
        private OptionButton _raceSelection;
        private RichTextLabel _raceDescription;
        private Button _backButton;
        private Button _createButton;

        private readonly string[] _availableRaces = { "Human", "Goblin", "Spider", "Demon", "Vampire" };
        private string _selectedRace = "Human";
        private string _selectedGender = "Male";

        public override void _Ready()
        {
            SetupUI();
            ConnectSignals();
            UpdateRaceSelection();
            GenerateRandomName();
        }

        private void SetupUI()
        {
            _nameInput = GetNode<LineEdit>("VBox/NameContainer/NameInput");
            _generateNameButton = GetNode<Button>("VBox/NameContainer/GenerateNameButton");
            _maleButton = GetNode<Button>("VBox/GenderContainer/GenderButtons/MaleButton");
            _femaleButton = GetNode<Button>("VBox/GenderContainer/GenderButtons/FemaleButton");
            _raceSelection = GetNode<OptionButton>("VBox/RaceContainer/RaceSelection");
            _raceDescription = GetNode<RichTextLabel>("VBox/DescriptionContainer/RaceDescription");
            _backButton = GetNode<Button>("VBox/ButtonContainer/BackButton");
            _createButton = GetNode<Button>("VBox/ButtonContainer/CreateButton");

            // Set default gender
            _maleButton.ButtonPressed = true;

            // Populate race selection
            foreach (string race in _availableRaces)
            {
                _raceSelection.AddItem(race);
            }
        }

        private void ConnectSignals()
        {
            _nameInput.TextChanged += OnNameChanged;
            _generateNameButton.Pressed += OnGenerateNamePressed;
            _maleButton.Toggled += OnMaleToggled;
            _femaleButton.Toggled += OnFemaleToggled;
            _raceSelection.ItemSelected += OnRaceSelected;
            _backButton.Pressed += OnBackPressed;
            _createButton.Pressed += OnCreatePressed;
        }

        private void OnNameChanged(string newText)
        {
            UpdateCreateButtonState();
        }

        private void OnGenerateNamePressed()
        {
            GenerateRandomName();
        }

        private void OnMaleToggled(bool pressed)
        {
            if (pressed)
            {
                _selectedGender = "Male";
                GenerateRandomName();
            }
        }

        private void OnFemaleToggled(bool pressed)
        {
            if (pressed)
            {
                _selectedGender = "Female";
                GenerateRandomName();
            }
        }

        private void OnRaceSelected(long index)
        {
            _selectedRace = _availableRaces[index];
            UpdateRaceSelection();
            GenerateRandomName();
        }

        private void GenerateRandomName()
        {
            string generatedName = NameGenerator.GenerateRandomName(_selectedRace, _selectedGender);
            _nameInput.Text = generatedName;
            UpdateCreateButtonState();
        }

        private void UpdateRaceSelection()
        {
            // Initialize GameManager if not already done
            if (GameManager.Instance == null)
            {
                var gameManager = new GameManager();
                gameManager._Ready(); // This will initialize the databases
            }

            var raceData = GameManager.Instance?.GetRaceData(_selectedRace);
            if (raceData != null)
            {
                _raceDescription.Text = $"[b]{raceData.Name}[/b]\n\n{raceData.Description}\n\n";
                _raceDescription.Text += "[b]Base Stats:[/b]\n";
                
                foreach (var stat in raceData.BaseStats)
                {
                    _raceDescription.Text += $"{stat.Key}: {stat.Value}\n";
                }

                _raceDescription.Text += $"\n[b]Progression:[/b] {(raceData.CanEvolve ? "Evolution" : "Professions")}\n";
                _raceDescription.Text += $"[b]Starting Skills:[/b] {string.Join(", ", raceData.StartingSkills)}";
            }
        }

        private void UpdateCreateButtonState()
        {
            _createButton.Disabled = string.IsNullOrWhiteSpace(_nameInput.Text);
        }

        private void OnBackPressed()
        {
            GetTree().ChangeSceneToFile("res://Scenes/Main/MainMenu.tscn");
        }

        private void OnCreatePressed()
        {
            if (string.IsNullOrWhiteSpace(_nameInput.Text))
            {
                GD.Print("Please enter a character name");
                return;
            }

            // Store character creation data
            GameManager.SelectedRace = _selectedRace;
            GameManager.SelectedGender = _selectedGender;
            GameManager.SelectedName = _nameInput.Text.Trim();

            GD.Print($"Creating character: {GameManager.SelectedName} ({GameManager.SelectedGender} {GameManager.SelectedRace})");

            // Transition to game world
            GetTree().ChangeSceneToFile("res://Scenes/Main/GameWorld.tscn");
        }
    }
}
