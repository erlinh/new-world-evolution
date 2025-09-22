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
		private VBoxContainer _evolutionPreview;
		private Button _exitButton;
		private Button _createButton;

		private readonly string[] _availableRaces = { "Human", "Goblin", "Spider" };
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
			_nameInput = GetNodeOrNull<LineEdit>("VBox/NameContainer/NameInput");
			_generateNameButton = GetNodeOrNull<Button>("VBox/NameContainer/GenerateNameButton");
			_maleButton = GetNodeOrNull<Button>("VBox/GenderContainer/GenderButtons/MaleButton");
			_femaleButton = GetNodeOrNull<Button>("VBox/GenderContainer/GenderButtons/FemaleButton");
			_raceSelection = GetNodeOrNull<OptionButton>("VBox/RaceContainer/RaceSelection");
			_raceDescription = GetNodeOrNull<RichTextLabel>("VBox/DescriptionContainer/RaceDescription");
			_evolutionPreview = GetNodeOrNull<VBoxContainer>("VBox/EvolutionContainer/EvolutionPreview");
			_exitButton = GetNodeOrNull<Button>("VBox/ButtonContainer/ExitButton");
			_createButton = GetNodeOrNull<Button>("VBox/ButtonContainer/CreateButton");

			// Try fallback paths for buttons if the new ones don't exist
			if (_exitButton == null)
				_exitButton = GetNodeOrNull<Button>("VBox/ButtonContainer/BackButton");

			if (_maleButton != null)
				_maleButton.ButtonPressed = true;

			// Populate race selection if it exists
			if (_raceSelection != null)
			{
				foreach (string race in _availableRaces)
				{
					_raceSelection.AddItem(race);
				}
			}

			// Log warnings for missing nodes
			if (_evolutionPreview == null) GD.Print("Warning: EvolutionPreview container not found - evolution preview disabled");
			if (_exitButton == null) GD.Print("Warning: ExitButton not found - using fallback or disabling exit functionality");
		}

		private void ConnectSignals()
		{
			if (_nameInput != null)
				_nameInput.TextChanged += OnNameChanged;
			if (_generateNameButton != null)
				_generateNameButton.Pressed += OnGenerateNamePressed;
			if (_maleButton != null)
				_maleButton.Toggled += OnMaleToggled;
			if (_femaleButton != null)
				_femaleButton.Toggled += OnFemaleToggled;
			if (_raceSelection != null)
				_raceSelection.ItemSelected += OnRaceSelected;
			if (_exitButton != null)
				_exitButton.Pressed += OnExitPressed;
			if (_createButton != null)
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
			if (_nameInput != null)
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
			if (raceData != null && _raceDescription != null)
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

		private void UpdateCreateButtonState()
		{
			if (_createButton != null && _nameInput != null)
				_createButton.Disabled = string.IsNullOrWhiteSpace(_nameInput.Text);
		}

		private void UpdateEvolutionPreview(RaceData raceData)
		{
			// Skip if evolution preview container is not available
			if (_evolutionPreview == null)
				return;

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

		private void OnExitPressed()
		{
			GetTree().Quit();
		}

		private void OnCreatePressed()
		{
			if (_nameInput == null || string.IsNullOrWhiteSpace(_nameInput.Text))
			{
				GD.Print("Please enter a character name");
				return;
			}

			// Store character creation data
			GameManager.SelectedRace = _selectedRace;
			GameManager.SelectedGender = _selectedGender;
			GameManager.SelectedName = _nameInput.Text.Trim();

			GD.Print($"Creating character: {GameManager.SelectedName} ({GameManager.SelectedGender} {GameManager.SelectedRace})");

			// Disable the button to prevent multiple clicks
			if (_createButton != null)
				_createButton.Disabled = true;

			// Simply transition to game world - let the GameWorld scene handle initialization
			GetTree().ChangeSceneToFile("res://Scenes/Main/GameWorld.tscn");
		}
	}
}
