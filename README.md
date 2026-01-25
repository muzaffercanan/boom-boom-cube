# Boom Boom Cube 🧩🚀

A high-quality Match-3 puzzle game developed in Unity, featuring dynamic gameplay, explosive power-ups, and a robust architecture.

![Unity](https://img.shields.io/badge/Unity-6000.0.59f2-black?style=flat&logo=unity)
![Status](https://img.shields.io/badge/Status-Finished-success)

## 🎮 Gameplay Overview

**Boom Boom Cube** challenges players to clear the board by tapping on groups of matching colored cubes. Strategic play is rewarded with powerful rockets that can clear entire rows or columns!

### Key Features
*   **Match-2 Mechanic**: Tap any group of 2 or more adjacent cubes of the same color to blast them.
*   **Rocket Power-Up**: Match 5 or more cubes to generate a **Rocket**.
    *   **Vertical Rocket**: Clears a column.
    *   **Horizontal Rocket**: Clears a row.
*   **Interactive Obstacles**:
    *   📦 **Box**: Destroy adjacent cubes to break.
    *   🏺 **Vase**: Fragile obstacles that crumble with matches.
    *   🪨 **Stone**: Tough obstacles requiring strategic blasts.
*   **Dynamic Goals**: Each level has specific targets (e.g., "Destroy 10 Boxes", "Clear 20 Blue Cubes") and limited moves.
*   **Physics & Gravity**: Cubes fall realistically to fill empty spaces, creating new match opportunities.
*   **Persistent Progress**: Unlocked levels and high scores are saved automatically.

## 🏗 Project Architecture

The project follows a modular, system-based architecture to ensure scalability and clean code separation.

```
Assets/Scripts/
├── Board/              # Core Gameplay Logic
│   ├── Systems/        # Logic handlers (Match, Gravity, Rocket)
│   ├── Items/          # Item behaviors (Cube, Rocket, Obstacle)
│   └── Visuals/        # VFX and animations
├── Managers/           # Global State Management
│   ├── GameManager.cs  # Main game loop & rules
│   ├── UIManager.cs    # HUD & UI updates
│   └── AudioManager.cs # Sound & Music
├── Data/               # Data structures
│   └── LevelData.cs    # JSON serialization model
└── ScriptableObjects/  # Configuration assets
```

### Core Systems
*   **GridSystem**: Manages the 2D grid state, item placement, and coordinate validation.
*   **MatchSystem**: Uses **Flood Fill (BFS)** to detect connected groups of cubes efficiently.
*   **GravitySystem**: Handles the falling mechanics, ensuring the board refills correctly after blasts.
*   **RocketSystem**: Manages rocket creation, logic (Row/Column selection), and activation effects.
*   **ItemFactory**: Implements the Factory Pattern to handle instantiation of various game elements.

## 🚀 Getting Started

### Prerequisites
*   Unity Editor **6000.0.59f2** (Unity 6 Preview) or later.

### Installation
1.  Clone the repository:
    ```bash
    git clone https://github.com/muzaffercanan/boom-boom-cube.git
    ```
2.  Open the project in Unity Hub.
3.  Open the main scene in `Assets/Scenes/Main.unity`.
4.  Press **Play** to start!

## 🧩 Level System

Levels are data-driven and stored as JSON files in `Assets/Levels/`. This allows for easy creation and modification of levels without recompiling code.

**Example Level Data (`level_01.json`):**
```json
{
  "level_number": 1,
  "grid_width": 9,
  "grid_height": 9,
  "move_count": 20,
  "grid": [ "r", "g", "b", "bo", ... ]
}
```

*   **Grid Codes**:
    *   `r`, `g`, `b`, `y`: Colored Cubes (Red, Green, Blue, Yellow)
    *   `bo`: Box
    *   `s`: Stone
    *   `v`: Vase
    *   `rand`: Random Item

## 🎨 Visuals & Audio

*   **Particle Effects**: Custom particle systems for blasts, rocket trails, and victory confetti.
*   **Sound Manager**: Centralized audio control for SFX (matching, rockets, win/lose) and background music.
*   **Responsive UI**: Dynamic HUD that adapts to the level goals and remaining moves.

## 👨‍💻 Credits

Developed by **Muzaffer Canan**.

---
*Built with ❤️ in Unity*
