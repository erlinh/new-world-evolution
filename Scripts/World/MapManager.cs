using Godot;
using System.Collections.Generic;
using NewWorldEvolution.Core;

namespace NewWorldEvolution.World
{
    public partial class MapManager : Node2D
    {
        // Map landmark positions that correspond to spawn locations
        private readonly Dictionary<string, Vector2> _landmarkPositions = new Dictionary<string, Vector2>
        {
            {"HumanVillage", new Vector2(100, 100)},
            {"TradingPost", new Vector2(250, 250)},
            {"GoblinCave", new Vector2(-450, 150)},
            {"ForestClearing", new Vector2(-600, -100)},
            {"SpiderNest", new Vector2(-600, -200)},
            {"DarkForest", new Vector2(-500, -300)},
            {"DemonRift", new Vector2(550, 250)},
            {"CorruptedLands", new Vector2(600, 350)},
            {"VampireCastle", new Vector2(400, -350)},
            {"Crypts", new Vector2(300, -400)}
        };

        public override void _Ready()
        {
            // Position the player at the appropriate spawn location
            PositionPlayerAtSpawn();
        }

        private void PositionPlayerAtSpawn()
        {
            var gameManager = GameManager.Instance;
            if (gameManager?.CurrentSpawnLocation != null)
            {
                string spawnLocation = gameManager.CurrentSpawnLocation;
                
                if (_landmarkPositions.ContainsKey(spawnLocation))
                {
                    Vector2 spawnPosition = _landmarkPositions[spawnLocation];
                    
                    // Find the player in the scene
                    var player = GetNode<NewWorldEvolution.Player.PlayerController>("../Player");
                    if (player != null)
                    {
                        player.GlobalPosition = spawnPosition;
                        GD.Print($"Player positioned at {spawnLocation}: {spawnPosition}");
                    }
                }
                else
                {
                    GD.Print($"Unknown spawn location: {spawnLocation}, using default position");
                }
            }
            else
            {
                // Default spawn position (center of grasslands)
                var player = GetNode<NewWorldEvolution.Player.PlayerController>("../Player");
                if (player != null)
                {
                    player.GlobalPosition = Vector2.Zero;
                }
            }
        }

        public Vector2 GetLandmarkPosition(string landmarkName)
        {
            return _landmarkPositions.ContainsKey(landmarkName) ? _landmarkPositions[landmarkName] : Vector2.Zero;
        }

        public List<string> GetAllLandmarks()
        {
            return new List<string>(_landmarkPositions.Keys);
        }

        public string GetNearestLandmark(Vector2 position)
        {
            string nearest = "";
            float shortestDistance = float.MaxValue;

            foreach (var landmark in _landmarkPositions)
            {
                float distance = position.DistanceTo(landmark.Value);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearest = landmark.Key;
                }
            }

            return nearest;
        }
    }
}
