# Boom Boom Cube - Dream Games Case Study

A modular tile-matching game built in Unity, focusing on clean architecture, scalability, and performance.

## 🚀 Overview
This project is a technical assessment for Dream Games. It implements a grid-based puzzle mechanic where players match colored cubes and clear obstacles using various rocket boosters.

### Key Features
- **Dynamic Level Loading:** Levels are loaded from external JSON files, allowing for easy content expansion.
- **Match System:** Efficient recursive matching logic for identifying cube clusters.
- **Gravity & Board Resolution:** Smooth falling mechanics and automatic grid replenishment.
- **Special Items:** Rocket boosters (Horizontal/Vertical) that clear entire rows/columns.
- **Persistent Progress:** Player progress and unlocked levels are saved using `PlayerPrefs`.

## 🏗️ Architecture
The project follows a decoupled approach, separating game logic from Unity-specific rendering.

- **GameManager:** Orchestrates game states, level loading, and turn management.
- **GridSystem:** A lightweight data structure representing the logical game board.
- **Systems Layer:**
  - `MatchSystem`: Handles clustering and adjacent obstacle detection.
  - `GravitySystem`: Manages item displacement and "falling" logic.
  - `RocketSystem`: Controls projectile behavior and combo interactions.
- **Factory Pattern:** `ItemFactory` handles the instantiation and initialization of different game elements (Cubes, Obstacles, Rockets).

## 🛠️ Technical Details
- **Engine:** Unity 2022.3+
- **Input:** Handles both mouse and touch inputs.
- **UI:** TextMesh Pro for high-quality text rendering.
- **Extensibility:** New item types can be added by implementing `IItem` and adding them to the `ItemFactory`.

## 📖 How to Run
1. Open the project in Unity 2022.3 or later.
2. Select the `Splash` or `Menu` scene to start.
3. Play levels by clicking on the level buttons.
4. (Optional) Run in Editor and use the `BoardDebug` utilities for state inspection.
