using Godot;
using System.Linq;
using NewWorldEvolution.Core;
using NewWorldEvolution.World;

namespace NewWorldEvolution.UI
{
    public partial class HUDManager : Control
    {
        // Player Stats Elements
        private Label _nameLabel;
        private Label _raceLabel;
        private Label _genderLabel;
        private Label _levelLabel;
        private ProgressBar _healthBar;
        private Label _healthText;
        private ProgressBar _manaBar;
        private Label _manaText;
        private Label _progressionLabel;

        // World Info Elements
        private Label _timeLabel;
        private Label _populationLabel;
        private Label _locationLabel;

        // Event Log Elements
        private VBoxContainer _eventList;
        private ScrollContainer _eventScrollContainer;

        // Minimap Elements
        private ColorRect _minimapDisplay;
        private ColorRect _playerDot;

        // Update Timer
        private Timer _refreshTimer;
        
        // Target Panel
        private TargetPanel _targetPanel;

        public override void _Ready()
        {
            GetSceneElements();
            ConnectSignals();
            
            // Initial update
            UpdateAllUI();
            
            GD.Print("Dynamic HUD initialized successfully");
        }

        private void GetSceneElements()
        {
            // Player Stats
            _nameLabel = GetNodeOrNull<Label>("TopLeft/PlayerStatsPanel/StatsContainer/PlayerInfo/NameLabel");
            _raceLabel = GetNodeOrNull<Label>("TopLeft/PlayerStatsPanel/StatsContainer/PlayerInfo/RaceLabel");
            _genderLabel = GetNodeOrNull<Label>("TopLeft/PlayerStatsPanel/StatsContainer/PlayerInfo/GenderLabel");
            _levelLabel = GetNodeOrNull<Label>("TopLeft/PlayerStatsPanel/StatsContainer/PlayerInfo/LevelLabel");
            _healthBar = GetNodeOrNull<ProgressBar>("TopLeft/PlayerStatsPanel/StatsContainer/VitalStats/HealthContainer/HealthBar");
            _healthText = GetNodeOrNull<Label>("TopLeft/PlayerStatsPanel/StatsContainer/VitalStats/HealthContainer/HealthText");
            _manaBar = GetNodeOrNull<ProgressBar>("TopLeft/PlayerStatsPanel/StatsContainer/VitalStats/ManaContainer/ManaBar");
            _manaText = GetNodeOrNull<Label>("TopLeft/PlayerStatsPanel/StatsContainer/VitalStats/ManaContainer/ManaText");
            _progressionLabel = GetNodeOrNull<Label>("TopLeft/PlayerStatsPanel/StatsContainer/ProgressionLabel");

            // World Info
            _timeLabel = GetNodeOrNull<Label>("TopRight/WorldInfoPanel/WorldContainer/TimeLabel");
            _populationLabel = GetNodeOrNull<Label>("TopRight/WorldInfoPanel/WorldContainer/PopulationLabel");
            _locationLabel = GetNodeOrNull<Label>("TopRight/WorldInfoPanel/WorldContainer/LocationLabel");

            // Event Log
            _eventList = GetNodeOrNull<VBoxContainer>("BottomRight/EventLogPanel/EventContainer/EventScrollContainer/EventList");
            _eventScrollContainer = GetNodeOrNull<ScrollContainer>("BottomRight/EventLogPanel/EventContainer/EventScrollContainer");

            // Minimap
            _minimapDisplay = GetNodeOrNull<ColorRect>("MinimapArea/MinimapPanel/MinimapContainer/MinimapDisplay");
            _playerDot = GetNodeOrNull<ColorRect>("MinimapArea/MinimapPanel/MinimapContainer/MinimapDisplay/PlayerDot");

            // Timer
            _refreshTimer = GetNodeOrNull<Timer>("RefreshTimer");
            
            // Target panel
            _targetPanel = GetNodeOrNull<TargetPanel>("TargetPanel");
        }

        private void ConnectSignals()
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Timeout += OnRefreshTimer;
            }

            // Connect to world simulation events if available
            var worldSim = WorldSimulation.Instance;
            if (worldSim != null)
            {
                worldSim.DayPassed += OnDayPassed;
                // Note: EventOccurred event needs to be implemented in WorldSimulation
                // worldSim.EventOccurred += OnEventOccurred;
            }
        }

        private void OnRefreshTimer()
        {
            UpdateAllUI();
        }

        private void UpdateAllUI()
        {
            UpdatePlayerStats();
            UpdateWorldInfo();
            UpdateMinimap();
        }

        private void UpdatePlayerStats()
        {
            var player = GameManager.Instance?.CurrentPlayer;
            if (player != null)
            {
                var stats = player.Stats;
                
                // Get actual data with fallbacks
                string playerName = !string.IsNullOrEmpty(player.PlayerName) ? player.PlayerName : 
                                  !string.IsNullOrEmpty(GameManager.SelectedName) ? GameManager.SelectedName : "Unknown";
                
                string playerRace = !string.IsNullOrEmpty(player.CurrentRace) ? player.CurrentRace : 
                                   !string.IsNullOrEmpty(GameManager.SelectedRace) ? GameManager.SelectedRace : "Unknown";
                
                string playerGender = !string.IsNullOrEmpty(player.Gender) ? player.Gender : 
                                     !string.IsNullOrEmpty(GameManager.SelectedGender) ? GameManager.SelectedGender : "Unknown";

                // Update labels with animated effects
                UpdateLabelWithEffect(_nameLabel, $"⚡ {playerName}");
                UpdateLabelWithEffect(_raceLabel, $"🏛️ {playerRace}");
                UpdateLabelWithEffect(_genderLabel, $"👤 {playerGender}");
                UpdateLabelWithEffect(_levelLabel, $"⭐ Level {stats?.Level ?? 1}");

                // Update health and mana bars with smooth animation
                if (stats != null)
                {
                    UpdateVitalBar(_healthBar, _healthText, stats.Health, stats.MaxHealth, "❤️");
                    UpdateVitalBar(_manaBar, _manaText, stats.Mana, stats.MaxMana, "💙");
                }

                // Update progression info
                string progressionText = GetProgressionText(player, playerRace);
                UpdateLabelWithEffect(_progressionLabel, progressionText);
            }
            else
            {
                // No player found - show loading state
                UpdateLabelWithEffect(_nameLabel, "⚡ Loading...");
                UpdateLabelWithEffect(_raceLabel, "🏛️ Loading...");
                UpdateLabelWithEffect(_genderLabel, "👤 Loading...");
                UpdateLabelWithEffect(_levelLabel, "⭐ Level 1");
                UpdateLabelWithEffect(_progressionLabel, "🔮 Initializing...");
            }
        }

        private void UpdateVitalBar(ProgressBar bar, Label text, int current, int max, string icon)
        {
            if (bar == null || text == null) return;

            // Smooth bar animation
            var tween = CreateTween();
            tween.TweenProperty(bar, "value", current, 0.3f);
            
            // Update max value
            bar.MaxValue = max;
            
            // Update text with icon and values
            text.Text = $"{current}/{max}";
            
            // Color coding based on percentage
            float percentage = (float)current / max;
            if (percentage > 0.7f)
                bar.Modulate = Colors.White;
            else if (percentage > 0.3f)
                bar.Modulate = Colors.Yellow;
            else
                bar.Modulate = Colors.Red;
        }

        private string GetProgressionText(Player.PlayerController player, string race)
        {
            if (!string.IsNullOrEmpty(player.CurrentEvolution))
            {
                return $"🧬 Evolution: {player.CurrentEvolution}";
            }
            else if (!string.IsNullOrEmpty(player.CurrentProfession))
            {
                return $"⚔️ Profession: {player.CurrentProfession}";
            }
            else
            {
                // Show available progression type
                var raceData = GameManager.Instance?.GetRaceData(race);
                if (raceData != null)
                {
                    return raceData.CanEvolve ? "🧬 Path: Evolution" : "⚔️ Path: Professions";
                }
                return "🔮 Progression: Unknown";
            }
        }

        private void UpdateWorldInfo()
        {
            var worldSim = WorldSimulation.Instance;
            if (worldSim != null)
            {
                try
                {
                    // Update time with fancy formatting
                    UpdateLabelWithEffect(_timeLabel, $"🌅 Day {worldSim.CurrentDay}, Year {worldSim.CurrentYear}");

                    // Update population with formatting
                    int totalPop = worldSim.GetTotalPopulation();
                    var popByRace = worldSim.GetPopulationByRace();
                    
                    string popText = $"👥 Total: {totalPop:N0}";
                    if (popByRace.Any())
                    {
                        var topRace = popByRace.OrderByDescending(x => x.Value).First();
                        popText += $"\n🏆 {topRace.Key}: {topRace.Value:N0}";
                    }
                    UpdateLabelWithEffect(_populationLabel, popText);

                    // Update location
                    var currentLocation = GameManager.Instance?.CurrentSpawnLocation;
                    string locationText = !string.IsNullOrEmpty(currentLocation) ? 
                                        $"🗺️ {currentLocation}" : "🗺️ Unknown Location";
                    UpdateLabelWithEffect(_locationLabel, locationText);

                    // Check for world destruction
                    if (worldSim.IsWorldDestroyed())
                    {
                        ShowCriticalWarning("💀 WORLD DESTROYED! 💀");
                    }
                }
                catch (System.Exception ex)
                {
                    UpdateLabelWithEffect(_timeLabel, "🌅 Time: Error");
                    UpdateLabelWithEffect(_populationLabel, "👥 Population: Error");
                    UpdateLabelWithEffect(_locationLabel, "🗺️ Location: Error");
                    GD.PrintErr($"World info update error: {ex.Message}");
                }
            }
            else
            {
                UpdateLabelWithEffect(_timeLabel, "🌅 Day 1, Year 1");
                UpdateLabelWithEffect(_populationLabel, "👥 Population: Loading...");
                UpdateLabelWithEffect(_locationLabel, "🗺️ Location: Initializing...");
            }
        }

        private void UpdateMinimap()
        {
            if (_playerDot == null || _minimapDisplay == null) return;

            var player = GameManager.Instance?.CurrentPlayer;
            if (player != null)
            {
                // Simple minimap - show player position relative to world
                var playerPos = player.GlobalPosition;
                var mapSize = _minimapDisplay.Size;
                
                // Normalize player position to map (simple approach)
                float normalizedX = (playerPos.X % 1000) / 1000.0f; // Wrap around every 1000 units
                float normalizedY = (playerPos.Y % 1000) / 1000.0f;
                
                // Position player dot
                _playerDot.Position = new Vector2(
                    normalizedX * mapSize.X - 2,
                    normalizedY * mapSize.Y - 2
                );
                
                // Pulse effect for player dot
                var tween = CreateTween();
                tween.TweenProperty(_playerDot, "modulate", new Color(1, 1, 1, 0.7f), 0.5f);
                tween.TweenProperty(_playerDot, "modulate", Colors.White, 0.5f);
                tween.SetLoops();
            }
        }

        private void UpdateLabelWithEffect(Label label, string text)
        {
            if (label == null) return;
            
            if (label.Text != text)
            {
                // Simple fade effect for text changes
                var tween = CreateTween();
                tween.TweenProperty(label, "modulate", new Color(1, 1, 1, 0.5f), 0.1f);
                tween.TweenCallback(Callable.From(() => { label.Text = text; }));
                tween.TweenProperty(label, "modulate", Colors.White, 0.2f);
            }
        }

        private void OnDayPassed(int day, int year)
        {
            // Add visual notification for day change
            AddEventToLog($"🌅 Day {day} begins...", Colors.Yellow);
            
            // Flash time label
            if (_timeLabel != null)
            {
                var tween = CreateTween();
                tween.TweenProperty(_timeLabel, "modulate", Colors.Yellow, 0.2f);
                tween.TweenProperty(_timeLabel, "modulate", Colors.White, 0.3f);
            }
        }

        private void OnEventOccurred(string eventText)
        {
            AddEventToLog($"⚡ {eventText}", Colors.Cyan);
        }

        private void AddEventToLog(string eventText, Color color)
        {
            if (_eventList == null) return;

            var eventLabel = new Label();
            eventLabel.Text = eventText;
            eventLabel.AddThemeColorOverride("font_color", color);
            eventLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            
            _eventList.AddChild(eventLabel);
            
            // Limit event log size
            while (_eventList.GetChildCount() > 20)
            {
                _eventList.GetChild(0).QueueFree();
            }
            
            // Auto-scroll to bottom
            if (_eventScrollContainer != null)
            {
                CallDeferred(nameof(ScrollToBottom));
            }
        }

        private void ScrollToBottom()
        {
            if (_eventScrollContainer != null)
            {
                _eventScrollContainer.ScrollVertical = (int)_eventScrollContainer.GetVScrollBar().MaxValue;
            }
        }

        private void ShowCriticalWarning(string message)
        {
            // Flash the entire HUD red
            var tween = CreateTween();
            tween.TweenProperty(this, "modulate", new Color(1, 0.5f, 0.5f, 1), 0.2f);
            tween.TweenProperty(this, "modulate", Colors.White, 0.3f);
            tween.SetLoops(3);
            
            AddEventToLog(message, Colors.Red);
        }

        // Public method for external events
        public void AddEvent(string eventText, Color? color = null)
        {
            AddEventToLog(eventText, color ?? Colors.White);
        }

        public void FlashHealthBar()
        {
            if (_healthBar != null)
            {
                var tween = CreateTween();
                tween.TweenProperty(_healthBar, "modulate", Colors.Red, 0.1f);
                tween.TweenProperty(_healthBar, "modulate", Colors.White, 0.2f);
            }
        }

        public void FlashManaBar()
        {
            if (_manaBar != null)
            {
                var tween = CreateTween();
                tween.TweenProperty(_manaBar, "modulate", Colors.Blue, 0.1f);
                tween.TweenProperty(_manaBar, "modulate", Colors.White, 0.2f);
            }
        }
        
        // Target panel management
        public void SetTarget(Entities.BaseMonster target)
        {
            _targetPanel?.SetTarget(target);
        }
        
        public void ClearTarget()
        {
            _targetPanel?.ClearTarget();
        }
        
        public bool HasTarget()
        {
            return _targetPanel?.HasTarget() ?? false;
        }
    }
}