using Godot;
using System.Collections.Generic;
using System.Linq;
using NewWorldEvolution.Data;
using NewWorldEvolution.World;

namespace NewWorldEvolution.Systems
{
    public partial class EconomySystem : Node
    {
        public static EconomySystem Instance { get; private set; }

        public Dictionary<string, ShopData> AllShops { get; private set; }
        public Dictionary<string, ItemData> AllItems { get; private set; }
        public Dictionary<string, float> MarketPrices { get; private set; }

        [Export] public float PriceFluctuationRate = 0.1f;
        [Export] public float SupplyDemandInfluence = 0.2f;

        [Signal] public delegate void ShopOpenedEventHandler(string shopId, string settlementName);
        [Signal] public delegate void ShopClosedEventHandler(string shopId, string reason);
        [Signal] public delegate void PriceChangedEventHandler(string itemName, float oldPrice, float newPrice);

        public override void _Ready()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeEconomy();
            }
            else
            {
                QueueFree();
            }
        }

        private void InitializeEconomy()
        {
            AllShops = new Dictionary<string, ShopData>();
            AllItems = new Dictionary<string, ItemData>();
            MarketPrices = new Dictionary<string, float>();

            CreateBaseItems();
            CreateInitialShops();
            SetupPriceUpdateTimer();
        }

        private void CreateBaseItems()
        {
            var items = new[]
            {
                new ItemData { Name = "Bread", Category = "Food", BasePrice = 5.0f, Rarity = "Common" },
                new ItemData { Name = "Iron Sword", Category = "Weapon", BasePrice = 50.0f, Rarity = "Common" },
                new ItemData { Name = "Health Potion", Category = "Consumable", BasePrice = 25.0f, Rarity = "Common" },
                new ItemData { Name = "Magic Staff", Category = "Weapon", BasePrice = 150.0f, Rarity = "Uncommon" },
                new ItemData { Name = "Dragon Scale", Category = "Material", BasePrice = 500.0f, Rarity = "Rare" },
                new ItemData { Name = "Ancient Tome", Category = "Book", BasePrice = 200.0f, Rarity = "Uncommon" },
                new ItemData { Name = "Goblin Ear", Category = "Trophy", BasePrice = 10.0f, Rarity = "Common" },
                new ItemData { Name = "Spider Silk", Category = "Material", BasePrice = 30.0f, Rarity = "Common" },
                new ItemData { Name = "Demon Horn", Category = "Material", BasePrice = 100.0f, Rarity = "Uncommon" },
                new ItemData { Name = "Vampire Fang", Category = "Material", BasePrice = 75.0f, Rarity = "Uncommon" }
            };

            foreach (var item in items)
            {
                AllItems[item.Name] = item;
                MarketPrices[item.Name] = item.BasePrice;
            }
        }

        private void CreateInitialShops()
        {
            var settlements = WorldSimulation.Instance?.AllSettlements;
            if (settlements == null) return;

            foreach (var settlement in settlements.Values)
            {
                CreateShopsForSettlement(settlement);
            }
        }

        private void CreateShopsForSettlement(SettlementData settlement)
        {
            // General store
            CreateShop("General Store", settlement.Name, "General", new[] { "Bread", "Health Potion", "Iron Sword" });

            // Race-specific shops
            switch (settlement.DominantRace)
            {
                case "Human":
                    CreateShop("Blacksmith", settlement.Name, "Weapons", new[] { "Iron Sword", "Magic Staff" });
                    CreateShop("Alchemist", settlement.Name, "Potions", new[] { "Health Potion", "Ancient Tome" });
                    break;
                case "Goblin":
                    CreateShop("Scrap Trader", settlement.Name, "Materials", new[] { "Goblin Ear", "Spider Silk" });
                    break;
                case "Spider":
                    CreateShop("Silk Weaver", settlement.Name, "Textiles", new[] { "Spider Silk" });
                    break;
                case "Demon":
                    CreateShop("Dark Merchant", settlement.Name, "Dark Items", new[] { "Demon Horn", "Magic Staff" });
                    break;
                case "Vampire":
                    CreateShop("Blood Bank", settlement.Name, "Vampire Goods", new[] { "Vampire Fang", "Ancient Tome" });
                    break;
            }
        }

        private void CreateShop(string name, string settlement, string type, string[] items)
        {
            string shopId = $"{settlement}_{name}_{System.Guid.NewGuid().ToString()[..8]}";
            
            var shop = new ShopData
            {
                Id = shopId,
                Name = name,
                Settlement = settlement,
                Type = type,
                OwnerId = GetRandomNPCInSettlement(settlement),
                Inventory = new Dictionary<string, ShopInventoryItem>(),
                IsOpen = true,
                Reputation = 50.0f
            };

            foreach (string itemName in items)
            {
                shop.Inventory[itemName] = new ShopInventoryItem
                {
                    ItemName = itemName,
                    Quantity = new System.Random().Next(5, 20),
                    LocalPriceModifier = 1.0f + ((float)new System.Random().NextDouble() - 0.5f) * 0.4f
                };
            }

            AllShops[shopId] = shop;
            EmitSignal(SignalName.ShopOpened, shopId, settlement);
        }

        private string GetRandomNPCInSettlement(string settlement)
        {
            var npcs = WorldSimulation.Instance?.GetNPCsInSettlement(settlement);
            if (npcs?.Count > 0)
            {
                var merchants = npcs.Where(n => n.Profession.Contains("Merchant") || n.Profession.Contains("Trader")).ToList();
                if (merchants.Count > 0)
                {
                    return merchants[new System.Random().Next(merchants.Count)].Id;
                }
                return npcs[new System.Random().Next(npcs.Count)].Id;
            }
            return null;
        }

        private void SetupPriceUpdateTimer()
        {
            var timer = new Timer();
            timer.WaitTime = 10.0f; // Update prices every 10 seconds
            timer.Timeout += UpdateMarketPrices;
            timer.Autostart = true;
            AddChild(timer);
        }

        private void UpdateMarketPrices()
        {
            foreach (var itemName in AllItems.Keys.ToList())
            {
                float oldPrice = MarketPrices[itemName];
                float newPrice = CalculateNewPrice(itemName);
                
                if (Mathf.Abs(newPrice - oldPrice) > 0.1f)
                {
                    MarketPrices[itemName] = newPrice;
                    EmitSignal(SignalName.PriceChanged, itemName, oldPrice, newPrice);
                }
            }

            UpdateShopInventories();
        }

        private float CalculateNewPrice(string itemName)
        {
            var item = AllItems[itemName];
            float currentPrice = MarketPrices[itemName];
            
            // Calculate supply and demand
            float totalSupply = GetTotalSupply(itemName);
            float totalDemand = GetTotalDemand(itemName);
            
            // Price fluctuation based on supply/demand
            float supplyDemandRatio = totalDemand / Mathf.Max(1.0f, totalSupply);
            float priceModifier = 1.0f + (supplyDemandRatio - 1.0f) * SupplyDemandInfluence;
            
            // Random market fluctuation
            float randomFactor = 1.0f + ((float)new System.Random().NextDouble() - 0.5f) * PriceFluctuationRate;
            
            float newPrice = currentPrice * priceModifier * randomFactor;
            
            // Keep prices within reasonable bounds
            newPrice = Mathf.Clamp(newPrice, item.BasePrice * 0.3f, item.BasePrice * 3.0f);
            
            return newPrice;
        }

        private float GetTotalSupply(string itemName)
        {
            return AllShops.Values
                .Where(shop => shop.IsOpen && shop.Inventory.ContainsKey(itemName))
                .Sum(shop => shop.Inventory[itemName].Quantity);
        }

        private float GetTotalDemand(string itemName)
        {
            // Base demand calculation - could be enhanced with more sophisticated logic
            var item = AllItems[itemName];
            float baseDemand = item.Category switch
            {
                "Food" => 50.0f,
                "Weapon" => 20.0f,
                "Consumable" => 30.0f,
                "Material" => 15.0f,
                "Book" => 10.0f,
                _ => 25.0f
            };

            // Modify demand based on current world state
            float worldPopulation = WorldSimulation.Instance?.GetTotalPopulation() ?? 100;
            return baseDemand * (worldPopulation / 100.0f);
        }

        private void UpdateShopInventories()
        {
            foreach (var shop in AllShops.Values.Where(s => s.IsOpen))
            {
                // Restock items randomly
                foreach (var inventoryItem in shop.Inventory.Values)
                {
                    if (new System.Random().NextDouble() < 0.3) // 30% chance to restock
                    {
                        inventoryItem.Quantity += new System.Random().Next(1, 5);
                        inventoryItem.Quantity = Mathf.Min(inventoryItem.Quantity, 50); // Max stock
                    }
                }

                // Shop closure chance if settlement is struggling
                var settlement = WorldSimulation.Instance?.AllSettlements?[shop.Settlement];
                if (settlement?.Prosperity < 30 && new System.Random().NextDouble() < 0.1)
                {
                    CloseShop(shop.Id, "Economic hardship");
                }
            }
        }

        public void CloseShop(string shopId, string reason)
        {
            if (AllShops.ContainsKey(shopId))
            {
                AllShops[shopId].IsOpen = false;
                EmitSignal(SignalName.ShopClosed, shopId, reason);
                GD.Print($"Shop {AllShops[shopId].Name} in {AllShops[shopId].Settlement} has closed due to {reason}");
            }
        }

        public List<ShopData> GetShopsInSettlement(string settlement)
        {
            return AllShops.Values
                .Where(shop => shop.Settlement == settlement && shop.IsOpen)
                .ToList();
        }

        public float GetCurrentPrice(string itemName, string shopId = null)
        {
            float basePrice = MarketPrices.ContainsKey(itemName) ? MarketPrices[itemName] : 0.0f;
            
            if (shopId != null && AllShops.ContainsKey(shopId))
            {
                var shop = AllShops[shopId];
                if (shop.Inventory.ContainsKey(itemName))
                {
                    return basePrice * shop.Inventory[itemName].LocalPriceModifier;
                }
            }
            
            return basePrice;
        }

        public bool PurchaseItem(string shopId, string itemName, int quantity)
        {
            if (!AllShops.ContainsKey(shopId) || !AllShops[shopId].IsOpen)
                return false;

            var shop = AllShops[shopId];
            if (!shop.Inventory.ContainsKey(itemName) || shop.Inventory[itemName].Quantity < quantity)
                return false;

            shop.Inventory[itemName].Quantity -= quantity;
            shop.Reputation += 0.1f; // Successful trade improves reputation

            return true;
        }

        public List<ItemData> GetAvailableItems()
        {
            return AllItems.Values.ToList();
        }
    }

    [System.Serializable]
    public class ShopData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Settlement { get; set; }
        public string Type { get; set; }
        public string OwnerId { get; set; }
        public Dictionary<string, ShopInventoryItem> Inventory { get; set; }
        public bool IsOpen { get; set; }
        public float Reputation { get; set; }

        public ShopData()
        {
            Inventory = new Dictionary<string, ShopInventoryItem>();
        }
    }

    [System.Serializable]
    public class ShopInventoryItem
    {
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public float LocalPriceModifier { get; set; }
    }

    [System.Serializable]
    public class ItemData
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public float BasePrice { get; set; }
        public string Rarity { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Properties { get; set; }

        public ItemData()
        {
            Properties = new Dictionary<string, object>();
        }
    }
}
