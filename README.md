# Boom Boom Cube

A Match-2 puzzle game developed in Unity. Players clear groups of matching cubes, create rockets, damage obstacles, and complete level goals within a limited move count.

![Unity](https://img.shields.io/badge/Unity-6000.0.59f2-black?style=flat&logo=unity)
![Status](https://img.shields.io/badge/Status-Finished-success)

## Gameplay Overview

**Boom Boom Cube** challenges players to clear the board by tapping groups of adjacent cubes with the same color. Larger matches create rockets that clear rows or columns.

### Key Features

* **Match-2 mechanic**: Tap any group of 2 or more adjacent cubes of the same color.
* **Rocket power-up**: Match enough cubes to generate a rocket.
  * **Vertical rocket**: Clears a column.
  * **Horizontal rocket**: Clears a row.
* **Interactive obstacles**:
  * **Box**: Damaged by adjacent cube blasts.
  * **Vase**: Can fall and takes damage from blasts.
  * **Stone**: Requires rocket hits.
* **Dynamic goals**: Each level has target obstacles and limited moves.
* **Gravity and refill**: Cubes fall to fill empty spaces after blasts.
* **Persistent progress**: Level progress is stored with `PlayerPrefs`.

## Project Architecture

The project follows a modular, system-based architecture.

```text
Assets/Scripts/
├── Board/
│   ├── Systems/          # Grid, match, gravity, rockets, goals, board resolving
│   ├── Items/            # Cube, rocket, and obstacle behaviours
│   └── Visuals/          # Rocket projectile visuals
├── Core/                 # Enums, interfaces, shared constants
├── Data/                 # Level data and loading/validation
├── Managers/             # Game, UI, audio, scene transition, progress
├── ScriptableObjects/    # Configuration assets
└── UI/                   # Goal item view
```

### Core Systems

* **GridSystem**: Manages the 2D board state and item placement.
* **MatchSystem**: Uses BFS to detect connected cube groups.
* **GravitySystem**: Moves fallable items down into empty spaces.
* **BoardResolver**: Coordinates gravity, refill, and hint refresh.
* **GoalTracker**: Tracks level goals and completion state.
* **RocketSystem**: Handles rocket activation and combo effects.
* **ItemFactory**: Creates board items and validates prefab mappings.
* **LevelRepository**: Loads, parses, and validates level JSON data.

## Getting Started

### Prerequisites

* Unity Editor **6000.0.59f2** or later.

### Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/muzaffercanan/boom-boom-cube.git
   ```

2. Open the project in Unity Hub.
3. Open the main scene at `Assets/Scenes/MainScene.unity`.
4. Press **Play**.

## Level System

Levels are data-driven and stored as JSON files in `Assets/Levels/`.

Example level data:

```json
{
  "level_number": 1,
  "grid_width": 9,
  "grid_height": 9,
  "move_count": 20,
  "grid": ["r", "g", "b", "bo"]
}
```

Grid codes:

* `r`, `g`, `b`, `y`: Red, green, blue, and yellow cubes
* `bo`: Box
* `s`: Stone
* `v`: Vase
* `rand`: Random cube

## Tests

EditMode tests live under `Assets/Tests/EditMode`.

Useful local checks:

```bash
dotnet build DreamGamesCase.sln --no-restore
```

## Credits

Developed by **Muzaffer Canan**.

<img width="488" height="867" alt="image" src="https://github.com/user-attachments/assets/91af0095-02aa-4c3e-a992-3f93a798f7a1" />
<img width="488" height="872" alt="image" src="https://github.com/user-attachments/assets/7fca2d18-1f17-4de3-86ce-020192befa37" />
<img width="488" height="870" alt="image" src="https://github.com/user-attachments/assets/8eb4f200-63d5-433c-bbb9-15c648ab02d0" />


---
*Built with ❤️ in Unity*
