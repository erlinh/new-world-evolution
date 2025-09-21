# New World Evolution - RPG Game

A comprehensive RPG game built with Godot 4.5 and C# featuring race-based evolution systems, dynamic goals, and complex character progression.

## Overview

New World Evolution is an RPG where players choose from different races, each with unique progression paths. Non-human races can evolve into powerful forms, while humans specialize through profession/class systems. The game features dynamic goal systems, complex skill trees, and race-based spawn mechanics.

## Features

### Core Systems
- **Race System**: 5 unique races (Human, Goblin, Spider, Demon, Vampire)
- **Evolution System**: Non-human races have branching evolution trees
- **Profession System**: Humans progress through class-based specializations
- **Skill System**: Active, passive, and unique abilities with multiple acquisition methods
- **Goal System**: Dynamic aspirations that unlock based on player actions
- **Spawn System**: Race-specific starting locations on a shared world map

### Races

#### Human
- **Special Feature**: Profession-based progression instead of evolution
- **Starting Stats**: Balanced across all attributes
- **Professions**: Warrior → Knight → Paladin, Mage → Wizard, etc.
- **Spawn Locations**: Villages, trading posts

#### Goblin
- **Special Feature**: Evolution-based progression with leadership paths
- **Starting Stats**: High dexterity and intelligence, low strength
- **Evolutions**: Hobgoblin → Goblin Chief, Goblin Shaman → Witch Doctor
- **Spawn Locations**: Caves, forest clearings

#### Spider
- **Special Feature**: Web-based abilities and stealth evolution paths
- **Starting Stats**: Extremely high dexterity, low charisma
- **Evolutions**: Giant Spider → Spider Queen, Shadow Spider → Phase Spider
- **Spawn Locations**: Ancient ruins, dark forests

#### Demon
- **Special Feature**: Corruption-based powers and dark magic
- **Starting Stats**: Varies by demon type
- **Evolutions**: Multiple paths leading to demon lord status
- **Spawn Locations**: Dimensional rifts, corrupted lands

#### Vampire
- **Special Feature**: Blood magic and aristocratic evolution paths
- **Starting Stats**: High charisma and constitution
- **Evolutions**: Ancient vampire, vampire lord variants
- **Spawn Locations**: Gothic castles, crypts

## Installation & Setup

1. **Prerequisites**:
   - Godot 4.5 or later
   - .NET 6.0 or later
   - C# development environment

2. **Project Setup**:
   ```bash
   # Clone or download the project
   # Open the project in Godot Editor
   # Build the C# solution
   ```

3. **First Run**:
   - Open `project.godot` in Godot Editor
   - Build the project (Build → Build Solution)
   - Run the project

## Project Structure

```
new-world-evolution/
├── Scripts/
│   ├── Core/                 # Core game systems
│   │   └── GameManager.cs    # Main game state manager
│   ├── Player/               # Player-related systems
│   │   ├── PlayerController.cs
│   │   ├── PlayerStats.cs
│   │   ├── Evolution/        # Evolution system
│   │   └── Profession/       # Profession system
│   ├── Skills/               # Skill system
│   ├── Goals/                # Goal/aspiration system
│   ├── World/                # World and spawn systems
│   ├── Data/                 # Data structures
│   └── UI/                   # User interface scripts
├── Scenes/                   # Godot scene files
│   ├── Main/                 # Main menu and core scenes
│   ├── Characters/           # Player and NPC scenes
│   ├── Environments/         # World locations
│   ├── Systems/              # System-specific scenes
│   └── UI/                   # UI scenes
├── Data/
│   └── Json/                 # Game data files
├── Resources/                # Assets and resources
└── GameDesignDocument.md     # Detailed design document
```

## Game Systems

### Evolution System (Non-Humans)
- **Branching Paths**: Multiple evolution options at each tier
- **Requirements**: Level, stats, skills, or special conditions
- **Effects**: Stat bonuses, new abilities, appearance changes
- **Final Forms**: Ultimate evolutions with unique powers

### Profession System (Humans)
- **Class Progression**: Linear advancement through specializations
- **Requirements**: Character level, stats, profession level
- **Benefits**: Skill unlocks, stat bonuses, profession-specific abilities
- **Mastery**: High-level professions with significant power

### Skill System
- **Acquisition Methods**:
  - Leveling up (skill points)
  - Learning from NPCs
  - Quest rewards
  - Title achievements
  - Evolution/profession unlocks
- **Skill Types**:
  - Active: Manually triggered abilities
  - Passive: Always-active bonuses
  - Unique: Race/evolution specific
- **Categories**: Combat, Magic, Survival, Social, Stealth, Knowledge

### Goal System
- **Dynamic Unlocking**: Goals appear based on player actions
- **Major Aspirations**:
  - Demon Lord: Corruption and dark magic mastery
  - Hero: Heroic deeds and honor
  - King/Queen: Political power and influence
  - God: Ultimate transcendence
  - Master Craftsman: Legendary item creation
- **Criteria**: Complex unlock conditions based on stats, actions, achievements

### Spawn System
- **Race-Based**: Each race has 2-3 starting locations
- **Shared World**: All locations exist on the same map
- **Random Selection**: Starting location chosen randomly from race options
- **Environment Effects**: Starting location affects available NPCs and quests

## Development Notes

### Architecture
- **Scene-Based Design**: Heavy use of Godot scenes for modularity
- **Data-Driven**: JSON configuration files for easy content modification
- **Component System**: Modular managers for different game systems
- **Signal-Based Communication**: Loose coupling between systems

### Extensibility
- **Easy Content Addition**: JSON files for races, skills, goals
- **Modular Systems**: Independent managers for each major system
- **Scene Templates**: Reusable scene structures
- **Clear Interfaces**: Well-defined APIs between systems

### Performance Considerations
- **Efficient Skill System**: Minimal per-frame calculations
- **Optimized Evolution Checking**: Event-driven updates
- **Streamlined UI**: Responsive interface updates
- **Smart Loading**: On-demand resource loading

## Future Enhancements

### Planned Features
- **Quest System**: Dynamic quest generation and tracking
- **NPC Relationships**: Complex social interaction system
- **Territory Control**: Kingdom/domain management
- **Multiplayer Support**: Online co-op and PvP modes
- **Modding Framework**: Community content creation tools

### Technical Improvements
- **Save System**: Comprehensive game state persistence
- **Performance Optimization**: LOD systems and efficient rendering
- **AI Systems**: Advanced NPC behavior and decision making
- **Procedural Content**: Dynamic world generation

## Contributing

The project is designed to be easily extensible:

1. **Adding New Races**: Create entries in `races.json` and corresponding scripts
2. **New Skills**: Add to `skills.json` and implement effects in `SkillManager`
3. **Additional Goals**: Define in `goals.json` with unlock conditions
4. **World Expansion**: Create new spawn locations and environments

## Technical Requirements

- **Godot Version**: 4.5 or later
- **C# Version**: .NET 6.0 or later
- **Platform**: Windows, Linux, macOS
- **Memory**: 4GB RAM minimum
- **Storage**: 2GB available space

## License

This project is provided as an example/template for game development with Godot and C#.

## Support

For questions about the codebase or implementation details, refer to:
- `GameDesignDocument.md` for detailed system specifications
- Code comments for specific implementation notes
- Godot documentation for engine-specific questions
