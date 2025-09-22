using Godot;

namespace NewWorldEvolution.UI
{
	public partial class OverheadDisplay : Control
	{
		private Node2D _targetEntity;
		private Label _nameLabel;
		private Label _levelLabel;
		private ProgressBar _healthBar;
		private Panel _backgroundPanel;
		private VBoxContainer _container;
		private Label _damageLabel;
		
		private float _displayOffset = -40.0f;
		private bool _showHealthBar = false;

		public override void _Ready()
		{
			SetupUI();
		}

		private void SetupUI()
		{
			// Set this control to not interfere with mouse events
			MouseFilter = Control.MouseFilterEnum.Ignore;
			
			// Create background panel
			_backgroundPanel = new Panel();
			_backgroundPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
			
			var styleBox = new StyleBoxFlat();
			styleBox.BgColor = new Color(0, 0, 0, 0.7f);
			styleBox.BorderColor = new Color(1, 1, 1, 0.3f);
			styleBox.BorderWidthTop = 1;
			styleBox.BorderWidthBottom = 1;
			styleBox.BorderWidthLeft = 1;
			styleBox.BorderWidthRight = 1;
			styleBox.CornerRadiusTopLeft = 4;
			styleBox.CornerRadiusTopRight = 4;
			styleBox.CornerRadiusBottomLeft = 4;
			styleBox.CornerRadiusBottomRight = 4;
			
			_backgroundPanel.AddThemeStyleboxOverride("panel", styleBox);
			AddChild(_backgroundPanel);
			
			// Create container
			_container = new VBoxContainer();
			_container.MouseFilter = Control.MouseFilterEnum.Ignore;
			_backgroundPanel.AddChild(_container);
			
			// Create name label
			_nameLabel = new Label();
			_nameLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
			_nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_nameLabel.AddThemeColorOverride("font_color", Colors.White);
			_nameLabel.AddThemeColorOverride("font_shadow_color", Colors.Black);
			_nameLabel.AddThemeConstantOverride("shadow_offset_x", 1);
			_nameLabel.AddThemeConstantOverride("shadow_offset_y", 1);
			_nameLabel.AddThemeConstantOverride("shadow_outline_size", 1);
			_container.AddChild(_nameLabel);
			
			// Create level label
			_levelLabel = new Label();
			_levelLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
			_levelLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_levelLabel.AddThemeColorOverride("font_color", Colors.Yellow);
			_levelLabel.AddThemeColorOverride("font_shadow_color", Colors.Black);
			_levelLabel.AddThemeConstantOverride("shadow_offset_x", 1);
			_levelLabel.AddThemeConstantOverride("shadow_offset_y", 1);
			_levelLabel.AddThemeConstantOverride("shadow_outline_size", 1);
			_levelLabel.AddThemeConstantOverride("font_size", 10);
			_container.AddChild(_levelLabel);
			
			// Create health bar (initially hidden)
			_healthBar = new ProgressBar();
			_healthBar.MouseFilter = Control.MouseFilterEnum.Ignore;
			_healthBar.CustomMinimumSize = new Vector2(60, 8);
			_healthBar.ShowPercentage = false;
			_healthBar.Visible = false;
			
			var healthStyle = new StyleBoxFlat();
			healthStyle.BgColor = Colors.Red;
			healthStyle.CornerRadiusTopLeft = 2;
			healthStyle.CornerRadiusTopRight = 2;
			healthStyle.CornerRadiusBottomLeft = 2;
			healthStyle.CornerRadiusBottomRight = 2;
			_healthBar.AddThemeStyleboxOverride("fill", healthStyle);
			
			_container.AddChild(_healthBar);
			
			// Initially hide the entire display
			Visible = false;
		}

		public void SetEntity(Node2D entity, string name, int level, Color nameColor)
		{
			_targetEntity = entity;
			
			if (_nameLabel != null)
			{
				_nameLabel.Text = name;
				_nameLabel.AddThemeColorOverride("font_color", nameColor);
			}
			
			if (_levelLabel != null)
			{
				_levelLabel.Text = $"Level {level}";
			}
			
			Visible = true;
			// Defer position update to ensure viewport is ready
			CallDeferred(nameof(UpdatePosition));
		}

		public void UpdatePosition()
		{
			if (_targetEntity == null || !IsInstanceValid(_targetEntity))
			{
				Visible = false;
				return;
			}

			// Check if we have a valid viewport
			var viewport = GetViewport();
			if (viewport == null) return;

			// Get the camera
			var camera = viewport.GetCamera2D();
			if (camera == null) 
			{
				// Fallback: position relative to entity without camera calculations
				GlobalPosition = _targetEntity.GlobalPosition + new Vector2(0, _displayOffset);
			}
			else
			{
				// Convert world position to screen position
				var worldPos = _targetEntity.GlobalPosition;
				var screenPos = camera.GetScreenCenterPosition() + (worldPos - camera.GlobalPosition);
				screenPos.Y += _displayOffset;
				GlobalPosition = screenPos;
			}
			
			// Ensure UI components are properly sized
			if (_backgroundPanel != null && _container != null)
			{
				_container.Position = Vector2.Zero;
				_backgroundPanel.Size = _container.Size + new Vector2(8, 4);
				_container.Position = new Vector2(4, 2);
				
				// Center the background panel
				_backgroundPanel.Position = -_backgroundPanel.Size / 2;
			}
		}

		public void ShowHealthBar(bool show)
		{
			_showHealthBar = show;
			if (_healthBar != null)
			{
				_healthBar.Visible = show;
			}
		}

		public void UpdateHealthBar(int currentHealth, int maxHealth)
		{
			if (_healthBar != null && _showHealthBar)
			{
				_healthBar.MaxValue = maxHealth;
				_healthBar.Value = currentHealth;
				_healthBar.Visible = true;
				
				// Color coding based on health percentage
				float percentage = (float)currentHealth / maxHealth;
				Color healthColor;
				
				if (percentage > 0.7f)
					healthColor = Colors.Green;
				else if (percentage > 0.3f)
					healthColor = Colors.Yellow;
				else
					healthColor = Colors.Red;
				
				var healthStyle = new StyleBoxFlat();
				healthStyle.BgColor = healthColor;
				healthStyle.CornerRadiusTopLeft = 2;
				healthStyle.CornerRadiusTopRight = 2;
				healthStyle.CornerRadiusBottomLeft = 2;
				healthStyle.CornerRadiusBottomRight = 2;
				_healthBar.AddThemeStyleboxOverride("fill", healthStyle);
			}
		}

		public void ShowDamage(int damage)
		{
			// Create floating damage text
			var damageText = new Label();
			damageText.Text = $"-{damage}";
			damageText.AddThemeColorOverride("font_color", Colors.Red);
			damageText.AddThemeColorOverride("font_shadow_color", Colors.Black);
			damageText.AddThemeConstantOverride("shadow_offset_x", 1);
			damageText.AddThemeConstantOverride("shadow_offset_y", 1);
			damageText.AddThemeConstantOverride("font_size", 14);
			damageText.HorizontalAlignment = HorizontalAlignment.Center;
			
			GetParent().AddChild(damageText);
			damageText.GlobalPosition = GlobalPosition + new Vector2(0, -20);
			
			// Animate the damage text
			var tween = CreateTween();
			tween.Parallel().TweenProperty(damageText, "global_position", 
				damageText.GlobalPosition + new Vector2(0, -30), 1.0f);
			tween.Parallel().TweenProperty(damageText, "modulate", 
				new Color(1, 1, 1, 0), 1.0f);
			tween.TweenCallback(Callable.From(() => damageText.QueueFree()));
		}

		public void SetVisibility(bool visible)
		{
			Visible = visible;
		}

		public override void _Process(double delta)
		{
			// Only update position if we're properly initialized and in the scene tree
			if (IsInsideTree() && _targetEntity != null)
			{
				UpdatePosition();
			}
		}
	}
}
