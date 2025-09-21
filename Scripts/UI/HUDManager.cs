using Godot;
using NewWorldEvolution.Core;
using NewWorldEvolution.World;

namespace NewWorldEvolution.UI
{
    public partial class HUDManager : Control
    {
        private Label _statsLabel;
        private Label _worldInfoLabel;
        private VBoxContainer _eventList;
        
        public override void _Ready()
        {
            _statsLabel = GetNode<Label>("HUD/StatsPanel/StatsLabel");
            _worldInfoLabel = GetNode<Label>("HUD/WorldInfoPanel/WorldInfoLabel");
            _eventList = GetNode<VBoxContainer>("HUD/EventLog/EventScrollContainer/EventList");
            
            // Connect to world simulation events
            if (WorldSimulation.Instance != null)
            {
                WorldSimulation.Instance.DayPassed += OnDayPassed;
                WorldSimulation.Instance.WorldEvent += OnWorldEvent;
            }
            
            // Start updating the HUD
            var timer = new Timer();
            timer.WaitTime = 1.0f;
            timer.Timeout += UpdateHUD;
            timer.Autostart = true;
            AddChild(timer);
        }
        
        private void UpdateHUD()
        {
            UpdateStatsPanel();
            UpdateWorldInfo();
        }
        
        private void UpdateStatsPanel()
        {
            var player = GameManager.Instance?.CurrentPlayer;
            if (player != null && _statsLabel != null)
            {
                var stats = player.Stats;
                string statsText = $"Player Stats\n" +
                    $"Name: {player.PlayerName ?? "Unknown"}\n" +
                    $"Race: {player.CurrentRace ?? "Unknown"}\n" +
                    $"Gender: {player.Gender ?? "Unknown"}\n" +
                    $"Level: {stats?.Level ?? 1}\n" +
                    $"Health: {stats?.Health ?? 100}/{stats?.MaxHealth ?? 100}\n" +
                    $"Mana: {stats?.Mana ?? 50}/{stats?.MaxMana ?? 50}";
                
                if (!string.IsNullOrEmpty(player.CurrentEvolution))
                {
                    statsText += $"\nEvolution: {player.CurrentEvolution}";
                }
                
                if (!string.IsNullOrEmpty(player.CurrentProfession))
                {
                    statsText += $"\nProfession: {player.CurrentProfession}";
                }
                
                _statsLabel.Text = statsText;
            }
        }
        
        private void UpdateWorldInfo()
        {
            var worldSim = WorldSimulation.Instance;
            if (worldSim != null && _worldInfoLabel != null)
            {
                int totalPop = worldSim.GetTotalPopulation();
                var popByRace = worldSim.GetPopulationByRace();
                
                string worldText = $"World Info\n" +
                    $"Day: {worldSim.CurrentDay}, Year: {worldSim.CurrentYear}\n" +
                    $"Total Population: {totalPop}\n";
                
                foreach (var race in popByRace)
                {
                    worldText += $"{race.Key}: {race.Value}\n";
                }
                
                if (worldSim.IsWorldDestroyed())
                {
                    worldText += "\n[color=red]WORLD DESTROYED![/color]";
                }
                
                _worldInfoLabel.Text = worldText;
            }
        }
        
        private void OnDayPassed(int day, int year)
        {
            UpdateWorldInfo();
        }
        
        private void OnWorldEvent(string eventDescription)
        {
            AddEventToLog(eventDescription);
        }
        
        private void AddEventToLog(string eventText)
        {
            if (_eventList != null)
            {
                var eventLabel = new Label();
                eventLabel.Text = $"[{WorldSimulation.Instance?.CurrentDay}/{WorldSimulation.Instance?.CurrentYear}] {eventText}";
                eventLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                eventLabel.AddThemeStyleboxOverride("normal", new StyleBoxFlat());
                
                _eventList.AddChild(eventLabel);
                
                // Limit the number of events shown (keep last 20)
                while (_eventList.GetChildCount() > 20)
                {
                    _eventList.GetChild(0).QueueFree();
                }
                
                // Auto-scroll to bottom
                var scrollContainer = _eventList.GetParent<ScrollContainer>();
                if (scrollContainer != null)
                {
                    CallDeferred(nameof(ScrollToBottom), scrollContainer);
                }
            }
        }
        
        private void ScrollToBottom(ScrollContainer scrollContainer)
        {
            scrollContainer.ScrollVertical = (int)scrollContainer.GetVScrollBar().MaxValue;
        }
    }
}
