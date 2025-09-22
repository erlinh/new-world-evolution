using Godot;
using System;

namespace NewWorldEvolution.UI.SkillBar
{
    public partial class SkillBarSlot : Control
    {
        [Signal] public delegate void SlotClickedEventHandler(int slotIndex, InputEventMouseButton mouseEvent);
        [Signal] public delegate void ItemDragStartedEventHandler(int slotIndex, string itemName);
        [Signal] public delegate void ItemDroppedEventHandler(int slotIndex, string itemName);

        public int SlotIndex { get; set; }
        public ISkillBarItem CurrentItem { get; private set; }
        public string KeybindText { get; set; } = "";

        private Panel _background;
        private TextureRect _iconDisplay;
        private Label _keybindLabel;
        private Label _chargesLabel;
        private ProgressBar _cooldownOverlay;
        private ColorRect _borderHighlight;

        private bool _isDragging = false;
        private Vector2 _dragStartPosition;

        public override void _Ready()
        {
            GetSceneElements();
            SetupSlotUI();
            MouseFilter = Control.MouseFilterEnum.Pass;
        }

        private void GetSceneElements()
        {
            // Get references to existing scene elements
            _background = GetNodeOrNull<Panel>("Background");
            _iconDisplay = GetNodeOrNull<TextureRect>("IconDisplay");
            _keybindLabel = GetNodeOrNull<Label>("Labels/KeybindLabel");
            _chargesLabel = GetNodeOrNull<Label>("Labels/ChargesLabel");
            _cooldownOverlay = GetNodeOrNull<ProgressBar>("CooldownOverlay");
            _borderHighlight = GetNodeOrNull<ColorRect>("BorderHighlight");
            
            // Setup click area
            var clickArea = GetNodeOrNull<Button>("ClickArea");
            if (clickArea != null)
            {
                clickArea.Pressed += OnButtonPressed;
            }
        }

        private void SetupSlotUI()
        {
            // Initialize visibility and default values
            if (_chargesLabel != null)
                _chargesLabel.Visible = false;
                
            if (_cooldownOverlay != null)
            {
                _cooldownOverlay.Value = 0;
                _cooldownOverlay.Visible = false;
            }
                
            if (_borderHighlight != null)
                _borderHighlight.Color = Colors.Transparent;
                
            // Set keybind text if available
            if (_keybindLabel != null && !string.IsNullOrEmpty(KeybindText))
                _keybindLabel.Text = KeybindText;
        }

        private void OnButtonPressed()
        {
            // Handle left click activation
            EmitSignal(SignalName.SlotClicked, SlotIndex, new InputEventMouseButton());
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
            {
                if (mouseEvent.ButtonIndex == MouseButton.Right)
                {
                    // Right-click to remove item
                    EmitSignal(SignalName.SlotClicked, SlotIndex, mouseEvent);
                }
                else if (mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    _isDragging = true;
                    _dragStartPosition = mouseEvent.GlobalPosition;
                }
            }
            else if (@event is InputEventMouseMotion motionEvent && _isDragging)
            {
                float dragDistance = _dragStartPosition.DistanceTo(motionEvent.GlobalPosition);
                if (dragDistance > 10.0f && CurrentItem != null)
                {
                    EmitSignal(SignalName.ItemDragStarted, SlotIndex, CurrentItem?.GetDisplayName() ?? "");
                    _isDragging = false;
                }
            }
            else if (@event is InputEventMouseButton releaseEvent && !releaseEvent.Pressed)
            {
                _isDragging = false;
            }
        }

        public void SetItem(ISkillBarItem item)
        {
            CurrentItem = item;
            UpdateDisplay();
        }

        public void ClearItem()
        {
            CurrentItem = null;
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (CurrentItem != null)
            {
                // Set icon
                _iconDisplay.Texture = CurrentItem.GetIcon();
                
                // Update charges
                int charges = CurrentItem.GetCurrentCharges();
                int maxCharges = CurrentItem.GetMaxCharges();
                if (maxCharges > 1)
                {
                    _chargesLabel.Text = charges.ToString();
                    _chargesLabel.Visible = true;
                }
                else
                {
                    _chargesLabel.Visible = false;
                }

                // Update border color based on item state
                var borderColor = CurrentItem.GetBorderColor();
                if (borderColor != Colors.Transparent)
                {
                    _borderHighlight.Color = borderColor;
                }

                // Update availability
                if (!CurrentItem.CanActivate())
                {
                    _iconDisplay.Modulate = new Color(0.5f, 0.5f, 0.5f, 1.0f);
                }
                else
                {
                    _iconDisplay.Modulate = Colors.White;
                }
            }
            else
            {
                // Empty slot
                _iconDisplay.Texture = null;
                _chargesLabel.Visible = false;
                _borderHighlight.Color = Colors.Transparent;
                _iconDisplay.Modulate = Colors.White;
            }

            // Update keybind display
            _keybindLabel.Text = KeybindText;
        }

        public void StartCooldown(float duration)
        {
            if (duration > 0)
            {
                _cooldownOverlay.Visible = true;
                _cooldownOverlay.Value = 100;

                var tween = CreateTween();
                tween.TweenProperty(_cooldownOverlay, "value", 0, duration);
                tween.TweenCallback(Callable.From(() => {
                    _cooldownOverlay.Visible = false;
                }));
            }
        }

        public override bool _CanDropData(Vector2 position, Variant data)
        {
            // Allow dropping skill bar items
            return data.Obj is ISkillBarItem;
        }

        public override void _DropData(Vector2 position, Variant data)
        {
            if (data.Obj is ISkillBarItem item)
            {
                EmitSignal(SignalName.ItemDropped, SlotIndex, item.GetDisplayName());
            }
        }

        public override Variant _GetDragData(Vector2 position)
        {
            if (CurrentItem != null && _isDragging)
            {
                // Create drag preview
                var preview = new Control();
                var previewIcon = new TextureRect();
                previewIcon.Texture = CurrentItem.GetIcon();
                previewIcon.CustomMinimumSize = new Vector2(32, 32);
                preview.AddChild(previewIcon);
                
                SetDragPreview(preview);
                return Variant.From(CurrentItem);
            }
            return new Variant();
        }

        public new string GetTooltipText()
        {
            return CurrentItem?.GetTooltipText() ?? "";
        }
    }
}
