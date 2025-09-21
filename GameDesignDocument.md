# New World Evolution - Game Design Document

## Core Concept
An RPG where players choose from different races, each with unique evolution paths or profession systems. The game features dynamic goal systems, multiple spawn locations, and complex skill acquisition methods.

## 1. Race System

### Available Races
- **Human**: Cannot evolve, gains professions/classes instead
- **Goblin**: Evolution-based progression
- **Spider**: Evolution-based progression  
- **Demon**: Evolution-based progression
- **Vampire**: Evolution-based progression

### Evolution System
- Each non-human race has multiple evolution paths
- Some evolutions lead to final forms, others to unique branches
- Evolution requirements vary (level, skills, items, actions, etc.)
- Each evolution changes appearance, abilities, and available skills

### Profession System (Humans Only)
- Humans specialize through professions/classes
- Tree-based progression similar to evolution
- Final specializations with unique abilities
- Examples: Warrior → Knight → Paladin/Dark Knight
- Examples: Mage → Elementalist → Archmage/Necromancer

## 2. Spawn System

### Race-Based Starting Locations
- Each race has 2-3 potential spawn locations
- All locations exist on the same world map
- Spawn location affects starting resources and nearby NPCs
- Random selection from race-appropriate locations

### Starting Location Examples:
- **Human**: Villages, small towns, trading posts
- **Goblin**: Caves, abandoned ruins, forest clearings
- **Spider**: Underground tunnels, dark forests, ancient temples
- **Demon**: Volcanic regions, corrupted lands, dimensional rifts
- **Vampire**: Gothic castles, crypts, shadowy mansions

## 3. Skill System

### Skill Types
- **Active Skills**: Manually triggered abilities (spells, attacks, buffs)
- **Passive Skills**: Always-active bonuses (stat boosts, resistances)
- **Unique Skills**: Race/evolution/profession specific abilities

### Skill Acquisition Methods
1. **Leveling Up**: Traditional XP-based skill points
2. **Titles**: Earned through achievements or actions
3. **Quests**: Specific quest rewards
4. **Learning from NPCs**: Training, mentorship, studying
5. **Profession Advancement**: Class-specific skills
6. **Evolution**: Gained through racial evolution

### Skill Categories
- Combat (Melee, Ranged, Magic)
- Survival (Crafting, Gathering, Cooking)
- Social (Persuasion, Leadership, Intimidation)
- Stealth (Sneaking, Lockpicking, Assassination)
- Knowledge (Lore, Investigation, Research)

## 4. Goal/Aspiration System

### Dynamic Goal Unlocking
Goals become available based on player actions, achievements, and story progression.

### Major Goals
- **Demon Lord**: Corruption path, dark magic mastery, conquering territories
- **Hero**: Heroic deeds, saving people, defeating evil
- **King/Queen**: Political influence, territory control, noble connections
- **God**: Ultimate power, divine artifacts, transcendence rituals
- **Master Craftsman**: Legendary crafting, unique item creation
- **Shadow Master**: Stealth mastery, underground networks
- **Arcane Scholar**: Magic research, forbidden knowledge

### Goal Unlock Criteria Examples
- **Demon Lord**: Kill 100 innocents + Master dark magic + Control demonic forces
- **Hero**: Save 50 people + Defeat major evil + Gain popular support
- **King/Queen**: Gain noble title + Control territory + Political alliances
- **God**: Reach max level + Collect divine artifacts + Complete transcendence ritual

## 5. Technical Structure

### Scene Organization
```
Scenes/
├── Main/
│   ├── MainMenu.tscn
│   ├── GameWorld.tscn
│   └── UI/
│       ├── HUD.tscn
│       ├── InventoryUI.tscn
│       ├── SkillTreeUI.tscn
│       └── GoalUI.tscn
├── Characters/
│   ├── Player/
│   │   ├── PlayerBase.tscn
│   │   ├── Human.tscn
│   │   ├── Goblin.tscn
│   │   ├── Spider.tscn
│   │   ├── Demon.tscn
│   │   └── Vampire.tscn
│   └── NPCs/
│       ├── Merchant.tscn
│       ├── Guard.tscn
│       └── QuestGiver.tscn
├── Environments/
│   ├── Spawns/
│   │   ├── HumanVillage.tscn
│   │   ├── GoblinCave.tscn
│   │   ├── SpiderNest.tscn
│   │   ├── DemonRift.tscn
│   │   └── VampireCastle.tscn
│   └── Areas/
│       ├── Forest.tscn
│       ├── Mountain.tscn
│       └── Dungeon.tscn
└── Systems/
    ├── EvolutionTree.tscn
    ├── ProfessionTree.tscn
    ├── SkillSystem.tscn
    └── QuestSystem.tscn
```

### Script Architecture
```
Scripts/
├── Core/
│   ├── GameManager.cs
│   ├── SceneManager.cs
│   └── SaveSystem.cs
├── Player/
│   ├── PlayerController.cs
│   ├── PlayerStats.cs
│   ├── Evolution/
│   │   ├── EvolutionManager.cs
│   │   ├── EvolutionTree.cs
│   │   └── EvolutionData.cs
│   └── Profession/
│       ├── ProfessionManager.cs
│       ├── ProfessionTree.cs
│       └── ProfessionData.cs
├── Skills/
│   ├── SkillManager.cs
│   ├── Skill.cs
│   ├── ActiveSkill.cs
│   └── PassiveSkill.cs
├── Goals/
│   ├── GoalManager.cs
│   ├── Goal.cs
│   └── GoalUnlockCondition.cs
├── World/
│   ├── SpawnManager.cs
│   ├── LocationData.cs
│   └── WorldGenerator.cs
└── Data/
    ├── RaceData.cs
    ├── SkillData.cs
    ├── GoalData.cs
    └── SpawnData.cs
```

## 6. Data Systems

### Race Evolution Trees
```json
{
  "Goblin": {
    "base": "Goblin",
    "evolutions": {
      "HobGoblin": {
        "requirements": {"level": 10, "strength": 15},
        "next": ["GoblinChief", "GoblinWarrior"]
      },
      "GoblinShaman": {
        "requirements": {"level": 8, "intelligence": 12},
        "next": ["GoblinWitchDoctor", "GoblinElementalist"]
      }
    }
  }
}
```

### Skill Trees
```json
{
  "Combat": {
    "MeleeWeapons": {
      "prerequisites": [],
      "skills": ["BasicSwordplay", "AdvancedSwordplay", "MasterSwordsman"]
    },
    "Magic": {
      "prerequisites": ["intelligence >= 10"],
      "skills": ["BasicSpell", "Fireball", "Meteor"]
    }
  }
}
```

### Goal System
```json
{
  "DemonLord": {
    "name": "Demon Lord",
    "description": "Rule over demons and spread darkness",
    "unlock_conditions": [
      {"type": "stat", "stat": "corruption", "value": 50},
      {"type": "skill", "skill": "DarkMagic", "level": 5},
      {"type": "kill_count", "target": "innocent", "count": 100}
    ],
    "rewards": {
      "title": "Dark Lord",
      "skills": ["DemonicAura", "SummonDemon"],
      "stat_bonuses": {"corruption": 25, "dark_magic": 10}
    }
  }
}
```

## 7. Progression Systems

### Experience and Leveling
- Traditional XP system with level caps per evolution/profession tier
- Different activities grant different types of XP
- Some evolutions require specific achievement XP, not just combat XP

### Title System
- Titles earned through specific achievements
- Each title provides unique bonuses or unlocks skills
- Some titles are mutually exclusive (Hero vs Villain paths)
- Examples: "Goblin Slayer", "Dragon Rider", "Master Thief"

### Reputation System
- Different factions have different opinions of the player
- Race affects starting reputation with various groups
- Actions influence reputation dynamically
- High/low reputation unlocks different quests and goals

## 8. World Design

### Map Structure
- Single large world map with diverse biomes
- Multiple towns, dungeons, and special locations
- Race-specific areas with appropriate NPCs and quests
- Hidden areas unlocked through progression or exploration

### Dynamic Events
- Random encounters based on location and player race
- Seasonal events and festivals
- Political changes affecting the world state
- Player actions influence world events

## 9. User Interface Design

### Main Menu
- Race selection with preview of evolution trees
- Load/Save game functionality
- Settings and options

### In-Game HUD
- Health/Mana/Stamina bars
- Quick skill slots
- Mini-map with race-appropriate markers
- Goal progress indicators

### Character Progression UI
- Evolution tree (visual node-based system)
- Profession tree (for humans)
- Skill allocation interface
- Goal tracking panel

## 10. Technical Considerations

### Save System
- Comprehensive save data including:
  - Player stats, race, evolution state
  - Learned skills and their levels
  - Completed quests and achievements
  - World state and NPC relationships
  - Goal progress and unlocked aspirations

### Performance Optimization
- LOD system for distant objects
- Efficient skill system with minimal per-frame calculations
- Streamlined evolution/profession checking
- Optimized UI updates

### Modding Support
- Data-driven design allows easy content addition
- JSON-based configuration files
- Separate scene files for easy modification
- Clear script architecture for extending functionality

This design provides a solid foundation for a complex RPG with multiple progression paths, dynamic goals, and rich character development systems. The heavy use of Godot scenes makes the game highly modular and easier to develop iteratively.
