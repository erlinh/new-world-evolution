using Godot;
using System.Collections.Generic;
using System.Linq;
using NewWorldEvolution.Data;
using NewWorldEvolution.Core;

namespace NewWorldEvolution.World
{
    public partial class WorldSimulation : Node
    {
        public static WorldSimulation Instance { get; private set; }

        [Export] public float TimeScale = 1.0f;
        [Export] public float DayDuration = 120.0f; // Real seconds per game day
        [Export] public int DaysPerYear = 100;
        [Export] public float SimulationTickInterval = 5.0f;

        public int CurrentDay { get; private set; } = 1;
        public int CurrentYear { get; private set; } = 1;
        public float CurrentDayProgress { get; private set; } = 0.0f;

        public Dictionary<string, NPCData> AllNPCs { get; private set; }
        public Dictionary<string, MonsterData> AllMonsters { get; private set; }
        public Dictionary<string, SettlementData> AllSettlements { get; private set; }
        public List<WorldEvent> RecentEvents { get; private set; }

        [Signal] public delegate void DayPassedEventHandler(int day, int year);
        [Signal] public delegate void YearPassedEventHandler(int year);
        [Signal] public delegate void NPCBornEventHandler(string npcId, string parentId1, string parentId2);
        [Signal] public delegate void NPCDiedEventHandler(string npcId, string cause);
        [Signal] public delegate void WorldEventEventHandler(string eventDescription);

        private Timer _dayTimer;
        private Timer _simulationTimer;
        private System.Random _random;

        public override void _Ready()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeWorld();
            }
            else
            {
                QueueFree();
            }
        }

        private void InitializeWorld()
        {
            _random = new System.Random();
            AllNPCs = new Dictionary<string, NPCData>();
            AllMonsters = new Dictionary<string, MonsterData>();
            AllSettlements = new Dictionary<string, SettlementData>();
            RecentEvents = new List<WorldEvent>();

            SetupTimers();
            CreateInitialPopulation();
            CreateInitialSettlements();
        }

        private void SetupTimers()
        {
            _dayTimer = new Timer();
            _dayTimer.WaitTime = DayDuration / TimeScale;
            _dayTimer.Timeout += OnDayPassed;
            _dayTimer.Autostart = true;
            AddChild(_dayTimer);

            _simulationTimer = new Timer();
            _simulationTimer.WaitTime = SimulationTickInterval;
            _simulationTimer.Timeout += OnSimulationTick;
            _simulationTimer.Autostart = true;
            AddChild(_simulationTimer);
        }

        private void CreateInitialPopulation()
        {
            var raceNames = new[] { "Human", "Goblin", "Spider", "Demon", "Vampire" };
            
            foreach (string raceName in raceNames)
            {
                int basePopulation = GetInitialPopulationForRace(raceName);
                
                for (int i = 0; i < basePopulation; i++)
                {
                    CreateRandomNPC(raceName, isInitial: true);
                }
            }

            GD.Print($"Created initial population: {AllNPCs.Count} NPCs");
        }

        private int GetInitialPopulationForRace(string race)
        {
            return race switch
            {
                "Human" => _random.Next(50, 100),
                "Goblin" => _random.Next(30, 60),
                "Spider" => _random.Next(20, 40),
                "Demon" => _random.Next(15, 30),
                "Vampire" => _random.Next(10, 20),
                _ => 10
            };
        }

        private void CreateInitialSettlements()
        {
            CreateSettlement("New Haven", "Human", new Vector2(0, 0), 25);
            CreateSettlement("Goblin Warren", "Goblin", new Vector2(-150, 100), 15);
            CreateSettlement("Spider Sanctuary", "Spider", new Vector2(150, 150), 10);
            CreateSettlement("Infernal Citadel", "Demon", new Vector2(100, -200), 8);
            CreateSettlement("Moonlight Manor", "Vampire", new Vector2(250, 50), 6);
        }

        private void CreateSettlement(string name, string dominantRace, Vector2 position, int initialPopulation)
        {
            var settlement = new SettlementData
            {
                Name = name,
                Position = position,
                DominantRace = dominantRace,
                Population = initialPopulation,
                Prosperity = _random.Next(50, 100),
                Defense = _random.Next(20, 80),
                NPCIds = new List<string>(),
                Buildings = new List<BuildingData>(),
                TradeRoutes = new List<string>()
            };

            // Add some basic buildings
            settlement.Buildings.Add(new BuildingData { Type = "Inn", Level = 1, Function = "Rest" });
            settlement.Buildings.Add(new BuildingData { Type = "Market", Level = 1, Function = "Trade" });
            settlement.Buildings.Add(new BuildingData { Type = "Guard Post", Level = 1, Function = "Defense" });

            AllSettlements[name] = settlement;
        }

        private string CreateRandomNPC(string race, bool isInitial = false)
        {
            string npcId = System.Guid.NewGuid().ToString();
            
            string gender = _random.Next(2) == 0 ? "Male" : "Female";
            
            var npc = new NPCData
            {
                Id = npcId,
                Name = NameGenerator.GenerateRandomName(race, gender),
                Race = race,
                Age = isInitial ? _random.Next(18, 60) : 0,
                Gender = gender,
                Stats = GenerateRandomStats(race),
                Profession = GetRandomProfession(race),
                Settlement = GetRandomSettlementForRace(race),
                RelationshipStatus = "Single",
                ParentIds = new List<string>(),
                ChildrenIds = new List<string>(),
                PersonalityTraits = GeneratePersonalityTraits(),
                IsAlive = true,
                BirthDay = CurrentDay,
                BirthYear = CurrentYear
            };

            AllNPCs[npcId] = npc;

            // Add to settlement
            if (AllSettlements.ContainsKey(npc.Settlement))
            {
                AllSettlements[npc.Settlement].NPCIds.Add(npcId);
                if (!isInitial)
                {
                    AllSettlements[npc.Settlement].Population++;
                }
            }

            return npcId;
        }

        private string GenerateRandomName(string race)
        {
            return NameGenerator.GenerateRandomName(race);
        }

        private Dictionary<string, int> GenerateRandomStats(string race)
        {
            var raceData = GameManager.Instance?.GetRaceData(race);
            var stats = new Dictionary<string, int>();

            if (raceData?.BaseStats != null)
            {
                foreach (var baseStat in raceData.BaseStats)
                {
                    stats[baseStat.Key] = baseStat.Value + _random.Next(-3, 4);
                }
            }

            return stats;
        }

        private string GetRandomProfession(string race)
        {
            var professions = race switch
            {
                "Human" => new[] { "Farmer", "Merchant", "Guard", "Priest", "Blacksmith", "Scholar" },
                "Goblin" => new[] { "Scavenger", "Tinkerer", "Scout", "Shaman", "Warrior" },
                "Spider" => new[] { "Weaver", "Hunter", "Venomancer", "Silk Merchant" },
                "Demon" => new[] { "Corruptor", "Warrior", "Sorcerer", "Tempter" },
                "Vampire" => new[] { "Noble", "Blood Dealer", "Shadow Assassin", "Ancient Scholar" },
                _ => new[] { "Wanderer" }
            };

            return professions[_random.Next(professions.Length)];
        }

        private string GetRandomSettlementForRace(string race)
        {
            var raceSettlements = AllSettlements.Values
                .Where(s => s.DominantRace == race)
                .ToList();

            if (raceSettlements.Count > 0)
            {
                return raceSettlements[_random.Next(raceSettlements.Count)].Name;
            }

            return AllSettlements.Keys.FirstOrDefault() ?? "Wilderness";
        }

        private List<string> GeneratePersonalityTraits()
        {
            var allTraits = new[] { "Brave", "Cowardly", "Greedy", "Generous", "Aggressive", "Peaceful", 
                                   "Intelligent", "Simple", "Charismatic", "Reclusive", "Loyal", "Treacherous" };
            
            var numTraits = _random.Next(2, 5);
            var selectedTraits = new List<string>();
            
            for (int i = 0; i < numTraits; i++)
            {
                string trait = allTraits[_random.Next(allTraits.Length)];
                if (!selectedTraits.Contains(trait))
                {
                    selectedTraits.Add(trait);
                }
            }

            return selectedTraits;
        }

        private void OnDayPassed()
        {
            CurrentDay++;
            CurrentDayProgress = 0.0f;

            if (CurrentDay > DaysPerYear)
            {
                CurrentDay = 1;
                CurrentYear++;
                OnYearPassed();
            }

            EmitSignal(SignalName.DayPassed, CurrentDay, CurrentYear);
            GD.Print($"Day {CurrentDay}, Year {CurrentYear}");
        }

        private void OnYearPassed()
        {
            ProcessYearlyEvents();
            EmitSignal(SignalName.YearPassed, CurrentYear);
            GD.Print($"=== Year {CurrentYear} has begun ===");
        }

        private void ProcessYearlyEvents()
        {
            // Age all NPCs
            foreach (var npc in AllNPCs.Values.Where(n => n.IsAlive))
            {
                npc.Age++;
                
                // Check for death by old age
                if (ShouldDieOfOldAge(npc))
                {
                    KillNPC(npc.Id, "Old Age");
                }
            }

            // Process births, marriages, etc.
            ProcessMarriages();
            ProcessBirths();
            ProcessNPCEvolution();
            ProcessSettlementGrowth();
        }

        private void OnSimulationTick()
        {
            CurrentDayProgress = (float)((_dayTimer.WaitTime - _dayTimer.TimeLeft) / _dayTimer.WaitTime);
            
            // Random events during the day
            if (_random.NextDouble() < 0.1f) // 10% chance per tick
            {
                ProcessRandomEvent();
            }

            // Update settlements
            UpdateSettlements();
        }

        private bool ShouldDieOfOldAge(NPCData npc)
        {
            int maxAge = npc.Race switch
            {
                "Human" => 80,
                "Goblin" => 60,
                "Spider" => 40,
                "Demon" => 200,
                "Vampire" => 1000,
                _ => 70
            };

            if (npc.Age > maxAge)
            {
                return _random.NextDouble() < 0.3f; // 30% chance per year past max age
            }

            return false;
        }

        private void ProcessMarriages()
        {
            var eligibleNPCs = AllNPCs.Values
                .Where(n => n.IsAlive && n.Age >= 18 && n.RelationshipStatus == "Single")
                .ToList();

            foreach (var npc in eligibleNPCs)
            {
                if (_random.NextDouble() < 0.2f) // 20% chance per year
                {
                    var potentialPartners = eligibleNPCs
                        .Where(p => p.Id != npc.Id && p.Race == npc.Race && p.Settlement == npc.Settlement)
                        .ToList();

                    if (potentialPartners.Count > 0)
                    {
                        var partner = potentialPartners[_random.Next(potentialPartners.Count)];
                        
                        npc.RelationshipStatus = "Married";
                        npc.SpouseId = partner.Id;
                        partner.RelationshipStatus = "Married";
                        partner.SpouseId = npc.Id;

                        CreateWorldEvent($"{npc.Name} and {partner.Name} got married in {npc.Settlement}!");
                    }
                }
            }
        }

        private void ProcessBirths()
        {
            var marriedCouples = AllNPCs.Values
                .Where(n => n.IsAlive && n.RelationshipStatus == "Married" && !string.IsNullOrEmpty(n.SpouseId))
                .GroupBy(n => new { Id1 = n.Id, Id2 = n.SpouseId }.Id1.CompareTo(new { Id1 = n.Id, Id2 = n.SpouseId }.Id2) < 0 ? n.Id : n.SpouseId)
                .Select(g => g.First())
                .ToList();

            foreach (var parent1 in marriedCouples)
            {
                if (_random.NextDouble() < 0.3f) // 30% chance per year
                {
                    var parent2 = AllNPCs[parent1.SpouseId];
                    string childId = CreateChildNPC(parent1, parent2);
                    
                    parent1.ChildrenIds.Add(childId);
                    parent2.ChildrenIds.Add(childId);

                    CreateWorldEvent($"{parent1.Name} and {parent2.Name} had a child in {parent1.Settlement}!");
                    EmitSignal(SignalName.NPCBorn, childId, parent1.Id, parent2.Id);
                }
            }
        }

        private string CreateChildNPC(NPCData parent1, NPCData parent2)
        {
            string childId = System.Guid.NewGuid().ToString();
            string childGender = _random.Next(2) == 0 ? "Male" : "Female";
            
            var child = new NPCData
            {
                Id = childId,
                Name = NameGenerator.GenerateRandomName(parent1.Race, childGender),
                Race = parent1.Race, // Children inherit race from parents
                Age = 0,
                Gender = childGender,
                Stats = InheritStats(parent1, parent2),
                Profession = "Child",
                Settlement = parent1.Settlement,
                RelationshipStatus = "Single",
                ParentIds = new List<string> { parent1.Id, parent2.Id },
                ChildrenIds = new List<string>(),
                PersonalityTraits = InheritPersonalityTraits(parent1, parent2),
                IsAlive = true,
                BirthDay = CurrentDay,
                BirthYear = CurrentYear
            };

            AllNPCs[childId] = child;

            // Add to settlement
            if (AllSettlements.ContainsKey(child.Settlement))
            {
                AllSettlements[child.Settlement].NPCIds.Add(childId);
                AllSettlements[child.Settlement].Population++;
            }

            return childId;
        }

        private Dictionary<string, int> InheritStats(NPCData parent1, NPCData parent2)
        {
            var stats = new Dictionary<string, int>();
            
            foreach (var stat in parent1.Stats.Keys)
            {
                int parent1Stat = parent1.Stats[stat];
                int parent2Stat = parent2.Stats.ContainsKey(stat) ? parent2.Stats[stat] : parent1Stat;
                
                // Average of parents with some random variation
                int inheritedStat = (parent1Stat + parent2Stat) / 2 + _random.Next(-2, 3);
                stats[stat] = Mathf.Max(1, inheritedStat);
            }

            return stats;
        }

        private List<string> InheritPersonalityTraits(NPCData parent1, NPCData parent2)
        {
            var traits = new List<string>();
            var allParentTraits = parent1.PersonalityTraits.Concat(parent2.PersonalityTraits).Distinct().ToList();
            
            // 50% chance to inherit each parent's trait
            foreach (var trait in allParentTraits)
            {
                if (_random.NextDouble() < 0.5f)
                {
                    traits.Add(trait);
                }
            }

            // Add a random new trait sometimes
            if (_random.NextDouble() < 0.3f)
            {
                var newTraits = new[] { "Ambitious", "Creative", "Stubborn", "Curious", "Patient", "Impulsive" };
                string newTrait = newTraits[_random.Next(newTraits.Length)];
                if (!traits.Contains(newTrait))
                {
                    traits.Add(newTrait);
                }
            }

            return traits;
        }

        private void ProcessNPCEvolution()
        {
            var evolvableNPCs = AllNPCs.Values
                .Where(n => n.IsAlive && n.Age >= 25 && n.Race != "Human" && string.IsNullOrEmpty(n.EvolutionForm))
                .ToList();

            foreach (var npc in evolvableNPCs)
            {
                if (_random.NextDouble() < 0.1f) // 10% chance per year
                {
                    var raceData = GameManager.Instance?.GetRaceData(npc.Race);
                    if (raceData?.EvolutionPaths?.Count > 0)
                    {
                        var availableEvolutions = raceData.EvolutionPaths.Keys.ToList();
                        string evolution = availableEvolutions[_random.Next(availableEvolutions.Count)];
                        
                        npc.EvolutionForm = evolution;
                        npc.Name = $"{npc.Name} the {evolution}";
                        
                        CreateWorldEvent($"{npc.Name} evolved into a {evolution}!");
                    }
                }
            }
        }

        private void ProcessSettlementGrowth()
        {
            foreach (var settlement in AllSettlements.Values)
            {
                // Population growth based on prosperity
                if (settlement.Prosperity > 70 && _random.NextDouble() < 0.3f)
                {
                    settlement.Prosperity += _random.Next(1, 5);
                }

                // Building construction
                if (settlement.Population > settlement.Buildings.Count * 5 && _random.NextDouble() < 0.4f)
                {
                    AddRandomBuilding(settlement);
                }

                // Trade route establishment
                if (settlement.Prosperity > 80 && settlement.TradeRoutes.Count < 3 && _random.NextDouble() < 0.2f)
                {
                    EstablishTradeRoute(settlement);
                }
            }
        }

        private void ProcessRandomEvent()
        {
            var eventTypes = new[] { "Raid", "Festival", "Plague", "Discovery", "Merchant", "Hero" };
            string eventType = eventTypes[_random.Next(eventTypes.Length)];

            switch (eventType)
            {
                case "Raid":
                    ProcessRaidEvent();
                    break;
                case "Festival":
                    ProcessFestivalEvent();
                    break;
                case "Plague":
                    ProcessPlagueEvent();
                    break;
                case "Discovery":
                    ProcessDiscoveryEvent();
                    break;
                case "Merchant":
                    ProcessMerchantEvent();
                    break;
                case "Hero":
                    ProcessHeroEvent();
                    break;
            }
        }

        private void ProcessRaidEvent()
        {
            var settlements = AllSettlements.Values.ToList();
            if (settlements.Count > 0)
            {
                var target = settlements[_random.Next(settlements.Count)];
                int casualties = _random.Next(1, Mathf.Max(1, target.Population / 10));
                
                KillRandomNPCsInSettlement(target.Name, casualties);
                target.Prosperity -= _random.Next(10, 30);
                target.Prosperity = Mathf.Max(0, target.Prosperity);
                
                CreateWorldEvent($"{target.Name} was raided! {casualties} casualties reported.");
            }
        }

        private void ProcessFestivalEvent()
        {
            var settlements = AllSettlements.Values.ToList();
            if (settlements.Count > 0)
            {
                var host = settlements[_random.Next(settlements.Count)];
                host.Prosperity += _random.Next(5, 15);
                
                CreateWorldEvent($"{host.Name} is hosting a grand festival! Prosperity increases.");
            }
        }

        private void ProcessPlagueEvent()
        {
            var settlements = AllSettlements.Values.ToList();
            if (settlements.Count > 0)
            {
                var affected = settlements[_random.Next(settlements.Count)];
                int casualties = _random.Next(2, Mathf.Max(2, affected.Population / 5));
                
                KillRandomNPCsInSettlement(affected.Name, casualties);
                
                CreateWorldEvent($"A plague strikes {affected.Name}! {casualties} have perished.");
            }
        }

        private void ProcessDiscoveryEvent()
        {
            var settlements = AllSettlements.Values.ToList();
            if (settlements.Count > 0)
            {
                var discoverer = settlements[_random.Next(settlements.Count)];
                discoverer.Prosperity += _random.Next(15, 25);
                
                CreateWorldEvent($"{discoverer.Name} discovered valuable resources! Great prosperity follows.");
            }
        }

        private void ProcessMerchantEvent()
        {
            CreateWorldEvent("A traveling merchant caravan has arrived, bringing exotic goods!");
        }

        private void ProcessHeroEvent()
        {
            string heroGender = _random.Next(2) == 0 ? "Male" : "Female";
            string heroName = NameGenerator.GenerateRandomName("Human", heroGender);
            CreateWorldEvent($"A hero named {heroName} has emerged, tales of their deeds spread far and wide!");
        }

        private void KillRandomNPCsInSettlement(string settlementName, int count)
        {
            var settlement = AllSettlements[settlementName];
            var livingNPCs = settlement.NPCIds
                .Select(id => AllNPCs[id])
                .Where(npc => npc.IsAlive)
                .ToList();

            int actualCasualties = Mathf.Min(count, livingNPCs.Count);
            
            for (int i = 0; i < actualCasualties; i++)
            {
                var victim = livingNPCs[_random.Next(livingNPCs.Count)];
                KillNPC(victim.Id, "Violence");
                livingNPCs.Remove(victim);
            }

            settlement.Population -= actualCasualties;
        }

        private void KillNPC(string npcId, string cause)
        {
            if (AllNPCs.ContainsKey(npcId))
            {
                var npc = AllNPCs[npcId];
                npc.IsAlive = false;
                npc.DeathDay = CurrentDay;
                npc.DeathYear = CurrentYear;
                npc.DeathCause = cause;

                EmitSignal(SignalName.NPCDied, npcId, cause);
                
                // Update settlement population
                if (AllSettlements.ContainsKey(npc.Settlement))
                {
                    AllSettlements[npc.Settlement].Population--;
                }
            }
        }

        private void AddRandomBuilding(SettlementData settlement)
        {
            var buildingTypes = new[] { "House", "Shop", "Temple", "Workshop", "Tavern", "Library", "Barracks" };
            string buildingType = buildingTypes[_random.Next(buildingTypes.Length)];
            
            settlement.Buildings.Add(new BuildingData 
            { 
                Type = buildingType, 
                Level = 1, 
                Function = GetBuildingFunction(buildingType) 
            });

            CreateWorldEvent($"A new {buildingType} was built in {settlement.Name}!");
        }

        private string GetBuildingFunction(string buildingType)
        {
            return buildingType switch
            {
                "House" => "Housing",
                "Shop" => "Commerce",
                "Temple" => "Religion",
                "Workshop" => "Crafting",
                "Tavern" => "Social",
                "Library" => "Knowledge",
                "Barracks" => "Defense",
                _ => "General"
            };
        }

        private void EstablishTradeRoute(SettlementData settlement)
        {
            var otherSettlements = AllSettlements.Values
                .Where(s => s.Name != settlement.Name && !settlement.TradeRoutes.Contains(s.Name))
                .ToList();

            if (otherSettlements.Count > 0)
            {
                var partner = otherSettlements[_random.Next(otherSettlements.Count)];
                settlement.TradeRoutes.Add(partner.Name);
                partner.TradeRoutes.Add(settlement.Name);

                CreateWorldEvent($"Trade route established between {settlement.Name} and {partner.Name}!");
            }
        }

        private void UpdateSettlements()
        {
            foreach (var settlement in AllSettlements.Values)
            {
                // Check if settlement is empty
                if (settlement.Population <= 0)
                {
                    CreateWorldEvent($"{settlement.Name} has become a ghost town - completely abandoned!");
                    // Settlement persists but becomes abandoned
                }
            }
        }

        private void CreateWorldEvent(string description)
        {
            var worldEvent = new WorldEvent
            {
                Description = description,
                Day = CurrentDay,
                Year = CurrentYear,
                Timestamp = Time.GetDatetimeStringFromSystem()
            };

            RecentEvents.Add(worldEvent);
            
            // Keep only recent events (last 50)
            if (RecentEvents.Count > 50)
            {
                RecentEvents.RemoveAt(0);
            }

            EmitSignal(SignalName.WorldEvent, description);
            GD.Print($"[World Event] {description}");
        }

        public int GetTotalPopulation()
        {
            return AllNPCs.Values.Count(npc => npc.IsAlive);
        }

        public bool IsWorldDestroyed()
        {
            return GetTotalPopulation() == 0;
        }

        public Dictionary<string, int> GetPopulationByRace()
        {
            return AllNPCs.Values
                .Where(npc => npc.IsAlive)
                .GroupBy(npc => npc.Race)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public List<NPCData> GetNPCsInSettlement(string settlementName)
        {
            if (!AllSettlements.ContainsKey(settlementName))
                return new List<NPCData>();

            return AllSettlements[settlementName].NPCIds
                .Where(id => AllNPCs.ContainsKey(id) && AllNPCs[id].IsAlive)
                .Select(id => AllNPCs[id])
                .ToList();
        }
    }
}
