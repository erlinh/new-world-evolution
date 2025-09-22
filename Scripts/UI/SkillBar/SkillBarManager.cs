using Godot;
using System.Collections.Generic;
using System.Linq;
using NewWorldEvolution.Data;
using NewWorldEvolution.Core;

namespace NewWorldEvolution.UI.SkillBar
{
    public partial class SkillBarManager : Control
    {
        [Export] public int DefaultSlotCount = 12;
        [Export] public Vector2 SlotSize = new Vector2(40, 40);
        [Export] public float SlotSpacing = 5.0f;

        private List<SkillBarSlot> _skillSlots = new List<SkillBarSlot>();
        private Dictionary<int, ISkillBarItem> _slottedItems = new Dictionary<int, ISkillBarItem>();
        private HBoxContainer _skillBarContainer;

        // Keybind mappings (slot index to key)
        private readonly Dictionary<int, Key> _defaultKeybinds = new Dictionary<int, Key>
        {
            {0, Key.Key1}, {1, Key.Key2}, {2, Key.Key3}, {3, Key.Key4},
            {4, Key.Key5}, {5, Key.Key6}, {6, Key.Key7}, {7, Key.Key8},
            {8, Key.Key9}, {9, Key.Key0}, {10, Key.Minus}, {11, Key.Equal}
        };

        public override void _Ready()
        {
            SetupSkillBar();
            ConnectToPlayerEvents();
        }

        private void SetupSkillBar()
        {
            // Get existing slots from the scene
            _skillBarContainer = GetNode<HBoxContainer>("MainContainer");
            
            // Initialize skill slots from scene children
            for (int i = 1; i <= 12; i++)
            {
                var slotNode = _skillBarContainer.GetNodeOrNull<SkillBarSlot>($"Slot{i}");
                if (slotNode != null)
                {
                    slotNode.SlotIndex = i - 1; // 0-based indexing
                    
                    // Set keybind text
                    if (_defaultKeybinds.ContainsKey(i - 1))
                    {
                        slotNode.KeybindText = _defaultKeybinds[i - 1].ToString().Replace("Key", "");
                        var keybindLabel = slotNode.GetNode<Label>("Labels/KeybindLabel");
                        keybindLabel.Text = slotNode.KeybindText;
                    }
                    
                    // Connect events
                    slotNode.SlotClicked += OnSlotClicked;
                    slotNode.ItemDragStarted += OnItemDragStarted;
                    slotNode.ItemDropped += OnItemDropped;
                    
                    _skillSlots.Add(slotNode);
                }
            }

            GD.Print($"Setup {_skillSlots.Count} skill slots from scene");
            
            // Auto-populate with available skills
            CallDeferred(nameof(PopulateWithPlayerSkills));
        }


        private void ConnectToPlayerEvents()
        {
            var player = GameManager.Instance?.CurrentPlayer;
            if (player != null)
            {
                // Connect to skill learning events
                var skillManager = player.GetNode<Skills.SkillManager>("SkillManager");
                if (skillManager != null)
                {
                    skillManager.SkillLearned += OnSkillLearned;
                    // Note: SkillLevelChanged event needs to be implemented in SkillManager
                    // skillManager.SkillLevelChanged += OnSkillLevelChanged;
                }
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                // Handle skill bar hotkeys
                foreach (var keybind in _defaultKeybinds)
                {
                    if (keyEvent.Keycode == keybind.Value)
                    {
                        ActivateSlot(keybind.Key);
                        break;
                    }
                }
            }
        }

        private void OnSlotClicked(int slotIndex, InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                ActivateSlot(slotIndex);
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Right)
            {
                // Right-click for context menu or remove item
                RemoveItemFromSlot(slotIndex);
            }
        }

        private void ActivateSlot(int slotIndex)
        {
            if (_slottedItems.ContainsKey(slotIndex))
            {
                var item = _slottedItems[slotIndex];
                item.Activate();
                
                // Update slot cooldown/state
                var slot = _skillSlots[slotIndex];
                slot.StartCooldown(item.GetCooldownTime());
            }
        }

        private void OnItemDragStarted(int fromSlot, string itemName)
        {
            // Handle drag and drop between slots
            GD.Print($"Drag started from slot {fromSlot}: {itemName}");
        }

        private void OnItemDropped(int toSlot, string itemName)
        {
            // Handle item being dropped onto a slot
            // Find the item by name and set it
            var item = FindItemByName(itemName);
            if (item != null)
            {
                SetSlotItem(toSlot, item);
            }
            GD.Print($"Item dropped to slot {toSlot}: {itemName}");
        }

        private void OnSkillLearned(string skillName)
        {
            // Auto-assign new skills to empty slots
            var skillData = GameManager.Instance?.GetSkillData(skillName);
            if (skillData != null)
            {
                var skillBarItem = new SkillBarSkill(skillData);
                AssignToFirstEmptySlot(skillBarItem);
            }
        }

        private void OnSkillLevelChanged(string skillName, int newLevel)
        {
            // Update any slots containing this skill
            foreach (var slot in _skillSlots)
            {
                if (slot.CurrentItem is SkillBarSkill skill && skill.SkillData.Name == skillName)
                {
                    skill.CurrentLevel = newLevel;
                    slot.UpdateDisplay();
                }
            }
        }

        public void SetSlotItem(int slotIndex, ISkillBarItem item)
        {
            if (slotIndex >= 0 && slotIndex < _skillSlots.Count)
            {
                _slottedItems[slotIndex] = item;
                _skillSlots[slotIndex].SetItem(item);
            }
        }

        public void RemoveItemFromSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _skillSlots.Count)
            {
                _slottedItems.Remove(slotIndex);
                _skillSlots[slotIndex].ClearItem();
            }
        }

        private void AssignToFirstEmptySlot(ISkillBarItem item)
        {
            for (int i = 0; i < _skillSlots.Count; i++)
            {
                if (!_slottedItems.ContainsKey(i))
                {
                    SetSlotItem(i, item);
                    break;
                }
            }
        }

        private void PopulateWithPlayerSkills()
        {
            // Add combat abilities to the first 4 slots
            var abilities = new List<(string name, string display, string desc, string key, float cooldown)>
            {
                ("BasicAttack", "Basic Attack", "Standard melee attack", "Q", 1.0f),
                ("PowerStrike", "Power Strike", "Powerful attack with double damage", "W", 3.0f),
                ("QuickSlash", "Quick Slash", "Fast attack with reduced damage", "E", 2.0f),
                ("SpinAttack", "Spin Attack", "Area attack hitting nearby enemies", "R", 5.0f)
            };

            int slotIndex = 0;
            foreach (var (name, display, desc, key, cooldown) in abilities)
            {
                var abilityItem = new SkillBarAbility(name, display, desc, key, cooldown);
                SetSlotItem(slotIndex, abilityItem);
                GD.Print($"Added ability to slot {slotIndex}: {display} [{key}]");
                slotIndex++;
            }

            // Add some basic skills to remaining slots
            var basicSkills = new List<string> { "BasicSwordplay", "BasicMagic", "BasicCrafting" };
            
            foreach (var skillName in basicSkills)
            {
                if (slotIndex >= DefaultSlotCount) break;
                
                var skillData = GameManager.Instance?.GetSkillData(skillName);
                if (skillData != null)
                {
                    var skillBarItem = new SkillBarSkill(skillData);
                    SetSlotItem(slotIndex, skillBarItem);
                    GD.Print($"Added skill to slot {slotIndex}: {skillName}");
                    slotIndex++;
                }
            }
        }

        public List<ISkillBarItem> GetAllSlottedItems()
        {
            return _slottedItems.Values.ToList();
        }

        public void SaveSkillBarLayout()
        {
            // TODO: Implement save/load functionality
            GD.Print("Saving skill bar layout...");
        }

        public void LoadSkillBarLayout()
        {
            // TODO: Implement save/load functionality
            GD.Print("Loading skill bar layout...");
        }
        
        private ISkillBarItem FindItemByName(string itemName)
        {
            // Find item in slotted items by name
            foreach (var item in _slottedItems.Values)
            {
                if (item.GetDisplayName() == itemName)
                {
                    return item;
                }
            }
            return null;
        }
    }
}
