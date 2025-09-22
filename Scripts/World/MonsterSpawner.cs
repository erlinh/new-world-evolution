using Godot;
using System.Collections.Generic;
using NewWorldEvolution.Entities;
using NewWorldEvolution.Entities.Monsters;
using NewWorldEvolution.Core;

namespace NewWorldEvolution.World
{
    public partial class MonsterSpawner : Node2D
    {
        [Export] public int MaxMonsters = 10;
        [Export] public float SpawnRadius = 200.0f;
        [Export] public float SpawnInterval = 5.0f;
        [Export] public bool AutoSpawn = true;

        private List<BaseMonster> _spawnedMonsters = new List<BaseMonster>();
        private Timer _spawnTimer;
        private PackedScene _slimeScene;
        private PackedScene _goblinScene;
        private PackedScene _wolfScene;

        public override void _Ready()
        {
            LoadMonsterScenes();
            SetupSpawnTimer();
            
            if (AutoSpawn)
            {
                // Spawn initial monsters
                CallDeferred(nameof(SpawnInitialMonsters));
            }
        }

        private void LoadMonsterScenes()
        {
            _slimeScene = GD.Load<PackedScene>("res://Scenes/Entities/Monsters/Slime.tscn");
            _goblinScene = GD.Load<PackedScene>("res://Scenes/Entities/Monsters/Goblin.tscn");
            _wolfScene = GD.Load<PackedScene>("res://Scenes/Entities/Monsters/Wolf.tscn");
        }

        private void SetupSpawnTimer()
        {
            _spawnTimer = new Timer();
            _spawnTimer.WaitTime = SpawnInterval;
            _spawnTimer.Autostart = AutoSpawn;
            _spawnTimer.Timeout += OnSpawnTimer;
            AddChild(_spawnTimer);
        }

        private void SpawnInitialMonsters()
        {
            // Spawn a few monsters to start with
            for (int i = 0; i < 3; i++)
            {
                SpawnRandomMonster();
            }
        }

        private void OnSpawnTimer()
        {
            // Clean up dead monsters
            CleanupDeadMonsters();
            
            // Spawn new monsters if under the limit
            if (_spawnedMonsters.Count < MaxMonsters)
            {
                SpawnRandomMonster();
            }
        }

        private void CleanupDeadMonsters()
        {
            for (int i = _spawnedMonsters.Count - 1; i >= 0; i--)
            {
                if (!IsInstanceValid(_spawnedMonsters[i]) || _spawnedMonsters[i].IsDead())
                {
                    _spawnedMonsters.RemoveAt(i);
                }
            }
        }

        private void SpawnRandomMonster()
        {
            var player = GameManager.Instance?.CurrentPlayer;
            if (player == null) return;

            // Don't spawn too close to the player
            Vector2 playerPos = player.GlobalPosition;
            Vector2 spawnPos;
            
            int attempts = 0;
            do
            {
                float angle = GD.Randf() * Mathf.Pi * 2;
                float distance = GD.Randf() * (SpawnRadius - 100.0f) + 100.0f;
                spawnPos = GlobalPosition + new Vector2(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance
                );
                attempts++;
            } 
            while (spawnPos.DistanceTo(playerPos) < 80.0f && attempts < 10);

            // Choose monster type based on player level and randomness
            BaseMonster monster = CreateRandomMonster(player.Stats?.Level ?? 1);
            if (monster != null)
            {
                monster.GlobalPosition = spawnPos;
                GetParent().AddChild(monster);
                _spawnedMonsters.Add(monster);
                
                GD.Print($"Spawned {monster.MonsterName} at {spawnPos}");
            }
        }

        private BaseMonster CreateRandomMonster(int playerLevel)
        {
            // Choose monster type based on weights
            float random = GD.Randf();
            
            // Adjust spawn chances based on player level
            if (random < 0.5f) // 50% chance for slimes
            {
                return CreateSlime(playerLevel);
            }
            else if (random < 0.8f) // 30% chance for goblins
            {
                return CreateGoblin(playerLevel);
            }
            else // 20% chance for wolves
            {
                return CreateWolf(playerLevel);
            }
        }

        private Slime CreateSlime(int playerLevel)
        {
            if (_slimeScene == null) return null;
            
            var slime = _slimeScene.Instantiate<Slime>();
            
            // Set level close to player level
            slime.Level = Mathf.Max(1, playerLevel + GD.RandRange(-1, 2));
            
            // Choose slime type based on level
            if (slime.Level >= 8 && GD.Randf() < 0.1f)
                slime.SlimeVariant = Slime.SlimeType.Golden;
            else if (slime.Level >= 5 && GD.Randf() < 0.2f)
                slime.SlimeVariant = Slime.SlimeType.Purple;
            else if (GD.Randf() < 0.3f)
                slime.SlimeVariant = GD.Randf() < 0.5f ? Slime.SlimeType.Blue : Slime.SlimeType.Red;
            else
                slime.SlimeVariant = Slime.SlimeType.Green;
                
            return slime;
        }

        private Goblin CreateGoblin(int playerLevel)
        {
            if (_goblinScene == null) return null;
            
            var goblin = _goblinScene.Instantiate<Goblin>();
            
            // Set level close to player level
            goblin.Level = Mathf.Max(1, playerLevel + GD.RandRange(-1, 3));
            
            // Choose goblin type based on level and chance
            if (goblin.Level >= 10 && GD.Randf() < 0.1f)
                goblin.GoblinVariant = Goblin.GoblinType.Chief;
            else if (goblin.Level >= 6 && GD.Randf() < 0.15f)
                goblin.GoblinVariant = Goblin.GoblinType.Berserker;
            else if (goblin.Level >= 4 && GD.Randf() < 0.2f)
                goblin.GoblinVariant = Goblin.GoblinType.Shaman;
            else if (GD.Randf() < 0.4f)
                goblin.GoblinVariant = Goblin.GoblinType.Warrior;
            else
                goblin.GoblinVariant = Goblin.GoblinType.Scout;
                
            return goblin;
        }

        private Wolf CreateWolf(int playerLevel)
        {
            if (_wolfScene == null) return null;
            
            var wolf = _wolfScene.Instantiate<Wolf>();
            
            // Set level close to player level
            wolf.Level = Mathf.Max(1, playerLevel + GD.RandRange(0, 4));
            
            // Choose wolf type based on level and chance
            if (wolf.Level >= 12 && GD.Randf() < 0.1f)
                wolf.WolfVariant = Wolf.WolfType.Alpha;
            else if (wolf.Level >= 8 && GD.Randf() < 0.15f)
                wolf.WolfVariant = Wolf.WolfType.Dire;
            else if (wolf.Level >= 5 && GD.Randf() < 0.2f)
                wolf.WolfVariant = GD.Randf() < 0.5f ? Wolf.WolfType.White : Wolf.WolfType.Black;
            else
                wolf.WolfVariant = Wolf.WolfType.Gray;
                
            return wolf;
        }

        public void SpawnSpecificMonster(string monsterType, int level, Vector2 position)
        {
            BaseMonster monster = null;
            
            switch (monsterType.ToLower())
            {
                case "slime":
                    monster = CreateSlime(level);
                    break;
                case "goblin":
                    monster = CreateGoblin(level);
                    break;
                case "wolf":
                    monster = CreateWolf(level);
                    break;
            }
            
            if (monster != null)
            {
                monster.Level = level;
                monster.GlobalPosition = position;
                GetParent().AddChild(monster);
                _spawnedMonsters.Add(monster);
            }
        }

        public void SetSpawnRate(float interval)
        {
            SpawnInterval = interval;
            if (_spawnTimer != null)
            {
                _spawnTimer.WaitTime = interval;
            }
        }

        public void StopSpawning()
        {
            AutoSpawn = false;
            if (_spawnTimer != null)
            {
                _spawnTimer.Stop();
            }
        }

        public void StartSpawning()
        {
            AutoSpawn = true;
            if (_spawnTimer != null)
            {
                _spawnTimer.Start();
            }
        }

        public int GetActiveMonsterCount()
        {
            CleanupDeadMonsters();
            return _spawnedMonsters.Count;
        }

        public List<BaseMonster> GetActiveMonsters()
        {
            CleanupDeadMonsters();
            return new List<BaseMonster>(_spawnedMonsters);
        }
    }
}
