# Boom Boom Cube

A highly modular, system-based Match-2 puzzle game developed in Unity. Players clear groups of matching cubes, create rocket power-ups, destroy obstacles, and complete goals within a limited number of moves.

![Unity](https://img.shields.io/badge/Unity-6000.0.59f2-black?style=flat&logo=unity)
![Status](https://img.shields.io/badge/Status-Refactored-success)
![Tests](https://img.shields.io/badge/Tests-Passing-success)

---

## 🎮 Gameplay Overview

**Boom Boom Cube** challenges players to clear the grid by tapping adjacent cubes of the same color. Matching larger groups rewards players with powerful rockets, enabling them to solve challenging puzzles under move constraints.

### Key Features
* **Match-2 Mechanic**: Tap any group of 2 or more adjacent cubes of the same color to blast them.
* **Rocket Power-Ups**: Matching **4 or more cubes** generates a rocket (Vertical or Horizontal).
  * **Vertical Rocket**: Clears an entire column.
  * **Horizontal Rocket**: Clears an entire row.
  * **Combo Rockets**: Detonating two adjacent rockets triggers a massive 3x3 row/column cross blast.
* **Interactive Obstacles**:
  * **Box**: Destroyed by adjacent matches or rocket hits (HP = 1).
  * **Vase**: Düşebilir (fallable), damaged by adjacent matches or rockets, transitions visual state on first hit, and breaks on the second (HP = 2).
  * **Stone**: Static, blocks gravity, and can **only** be destroyed by rocket hits (HP = 1).
* **Gravity & Cascading Refills**: Clearing items causes upper cubes and fallable obstacles (like Vases) to drop down with smooth animations, followed by newly spawned cubes refilling the top.
* **No-Move Shuffle**: If no matches are available on the board, the board automatically shuffles until at least one valid match is available.
* **Persistent Progress**: Saved using `PlayerPrefs` (`LastPlayedLevel` & `SelectedLevelForGame`).

---

## 🏛️ Modern Architecture & Design Patterns

The codebase has been refactored from a traditional, tightly-coupled Unity structure to a clean, modular, **MonoBehaviour-light** architecture adhering to **Composition over Inheritance**.

```
┌──────────────────────────────────────────┐
│  Managers (GameManager, UIManager, vb.)  │  ← MonoBehaviour'lar, Unity lifecycle
├──────────────────────────────────────────┤
│  Gameplay Logic (TurnProcessor, vb.)     │  ← Plain C# sınıfları
├──────────────────────────────────────────┤
│  Board Systems (Grid, Match, Gravity)    │  ← Plain C# sınıfları, domain logic
├──────────────────────────────────────────┤
│  Board Items (Cube, Rocket, Obstacles)   │  ← MonoBehaviour'lar, görsel temsil
├──────────────────────────────────────────┤
│  Core (Enums, Interfaces, Constants)     │  ← Paylaşılan tanımlamalar
├──────────────────────────────────────────┤
│  Data (LevelData, LevelRepository)       │  ← JSON parsing ve validasyon
└──────────────────────────────────────────┘
```

### Core Design Highlights:
1. **MonoBehaviour-Light Approach**: Core gameplay systems and domain logic are implemented in pure C# (Plain Old C# Objects - POCOs). Unity `MonoBehaviour` wrappers are only used for entry points, visual rendering, and coroutine execution.
2. **Composition Root & Factories**:
   * [GameplaySystemFactory](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Managers/GameplaySystemFactory.cs) instantiates all C# systems, resolving dependencies cleanly without service locators or global singletons.
   * [ItemFactory](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Board/Systems/ItemFactory.cs) decouples spawning board items using ID-to-Prefab mapping configured via ScriptableObjects.
3. **Event-Driven Communication**: Components interact through a decoupled event-bus mechanism via [GameEvents](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Core/GameEvents.cs) and [IGameplayEventBus](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Managers/GameplayServices.cs).
4. **Deterministic Seed-Based RNG**: The standard `UnityEngine.Random` is bypassed in favor of [GameRng](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Core/GameRng.cs) (`GameRng.Shared`), ensuring reproducible level layouts, deterministic shuffles, and robust unit testing.
5. **Decoupled Infrastructure**: Services such as audio playback, scene loading, progress tracking, and view lifecycles are abstracted behind mockable C# interfaces ([GameplayServices.cs](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Managers/GameplayServices.cs)), ensuring they can be fully unit-tested in EditMode.

---

## 📂 Project Structure & Namespaces

The project's code structure is divided into distinct, isolated layers using C# namespaces and assembly definitions:

| Namespace | Folder / Location | Responsibility |
| :--- | :--- | :--- |
| `DreamGames.Core` | [Scripts/Core/](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Core/) | Enums, interface definitions, game events, session/turn loggers, and deterministic RNG. |
| `DreamGames.Data` | [Scripts/Data/](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Data/) | Game level structures, data-driven cell layouts, parsing, and JSON validation. |
| `DreamGames.Board.Systems` | [Scripts/Board/Systems/](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Board/Systems/) | 2D Grid state, BFS Match Finding, Gravity calculations, Rocket beams, Goal tracking, Shuffling, and Spawning. |
| `DreamGames.Board.Items` | [Scripts/Board/Items/](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Board/Items/) | Cubes, Rockets, and Obstacles (`Box`, `Stone`, `Vase`) visual behaviour components. |
| `DreamGames.Board.Visuals` | [Scripts/Board/Visuals/](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Board/Visuals/) | Visual configurations, rocket projectiles, and shuffle transition UI elements. |
| `DreamGames.Gameplay` | [Scripts/Managers/](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Managers/) | High-level GameManager, turn logic processing, damage resolution, state controllers, and services. |
| `DreamGames.UI` | [Scripts/UI/](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/UI/) | Dynamic HUD goal tracker elements, win/lose menus, and buttons. |

---

## 🛠️ Detailed System Breakdown

### 1. Board & Grid State ([GridSystem.cs](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Board/Systems/GridSystem.cs))
Manages cell configurations using [BoardCellState](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Board/Systems/BoardCellState.cs) struct:
* **Normal**: Standard cell supporting gravity and spawn logic.
* **Hole**: Empty boundary cells where items cannot exist, blocking gravity and rocket beams.
* **Blocked**: Unusable cells acting as walls (blocks gravity and rockets).
* **Locked**: Normal cell which cannot spawn items but can hold items.

### 2. Matching Engine ([MatchSystem.cs](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Board/Systems/MatchSystem.cs))
Utilizes a BFS flood-fill algorithm to find adjacent cubes sharing the same color. It also fetches adjacent obstacle items to trigger damage chain events when matches are detonated.

### 3. Turn & Interaction Processing ([TurnProcessor.cs](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Managers/TurnProcessor.cs))
Coordinated by [CubeTurnHandler](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Managers/CubeTurnHandler.cs) and [RocketTurnHandler](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Managers/RocketTurnHandler.cs).
* Ensures input routing is locked during turn resolutions (`IsProcessingTurn`).
* Deducts moves upon valid player interactions.
* Logs step-by-step game actions via [SessionLog](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Core/SessionLog.cs) for debugging.

### 4. Cascade and Refill Coordination ([BoardResolver.cs](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Board/Systems/BoardResolver.cs))
Runs the board stabilization loop:
1. **Gravity**: [GravitySystem](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Board/Systems/GravitySystem.cs) drops fallable items into empty spaces. Uses staggered delays to create a cascading waterfall effect, completing with a landing bounce.
2. **Refill**: [BoardFiller](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Board/Systems/BoardFiller.cs) generates new random cubes above the board and drops them down.
3. **Stabilization**: Gravity is reapplied to stabilize newly spawned items.
4. **Hints**: Updates the rocket hints for matching groups.

### 5. Rocket Projectiles & Combos ([RocketSystem.cs](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Board/Systems/RocketSystem.cs))
Controls rocket spawning and beam paths.
* Uses `ObjectPool<GameObject>` to optimize spawning of [RocketProjectile](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Board/Visuals/RocketProjectile.cs) instances.
* Projectiles travel cell-by-cell, sending hit callbacks to [DamageResolver](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Managers/DamageResolver.cs) to process damage to obstacles or ignite chain reactions with other rockets.

---

## 📊 Level Config & Data-Driven Layouts

Levels are configured dynamically as JSON files in `Assets/StreamingAssets/Levels/` or `Assets/Resources/Levels/`.

### Advanced Grid Cell Configuration
The system supports structured cell configurations:
```json
{
  "level_number": 11,
  "grid_width": 5,
  "grid_height": 5,
  "move_count": 15,
  "cells": [
    { "cell_type": "normal", "item": "r" },
    { "cell_type": "hole" },
    { "cell_type": "blocked" },
    { "cell_type": "normal", "item": "bo" },
    { "cell_type": "normal", "item": "v" }
  ]
}
```
* **Grid Codes**:
  * `r` / `g` / `b` / `y`: Red, Green, Blue, and Yellow cubes
  * `bo`: Box Obstacle
  * `s`: Stone Obstacle
  * `v`: Vase Obstacle
  * `rand`: Random color cube (spawned deterministically)
  * `hro` / `vro`: Horizontal / Vertical Rockets

---

## 📚 Technical Documentation Suite

For deep-dive topics, please refer to the comprehensive internal documentation files located under [Assets/Scripts/Documentation/](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Documentation/):

* 📚 [INDEX.md](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Documentation/INDEX.md): Start here! Quick index mapping tasks to the correct docs.
* 📋 [PROJECT_OVERVIEW.md](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Documentation/PROJECT_OVERVIEW.md): Technology stack, namespace lists, sahne setups, and global rules.
* 🏛️ [ARCHITECTURE.md](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Documentation/ARCHITECTURE.md): Technical references, class definitions, and detailed API specs.
* 📝 [CODING_GUIDELINES.md](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Documentation/CODING_GUIDELINES.md): Code formatting patterns, random generation guidelines, and testing requirements.
* 🔄 [DATA_FLOWS.md](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Documentation/DATA_FLOWS.md): Interactive flow charts detailing Level Loading, Cube Blast, Rocket detonating, and Gravity cascades.
* 🕸️ [DEPENDENCY_GRAPH.md](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Documentation/DEPENDENCY_GRAPH.md): Structural dependencies, service implementations, and resolving circular references.
* 🗺️ [FILE_MAP.md](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Documentation/FILE_MAP.md): Location list mapping every script and prefab to its description.

---

## 🧪 Testing & Debugging

### NUnit Automated Testing
The project features extensive unit testing coverage, written in a decoupled manner allowing execution without staging rendering overhead:
* **EditMode Tests**: Located in [Tests/EditMode/BoardSystemsEditModeTests.cs](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Tests/EditMode/BoardSystemsEditModeTests.cs). Validates matching grids, gravity cascading, shuffle loops, and goal tracking.
* **PlayMode Tests**: Located in [Tests/PlayMode/GameManagerPlayModeSmokeTests.cs](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Tests/PlayMode/GameManagerPlayModeSmokeTests.cs). Verifies scene initialization and end-game flow validations.

To execute tests from command line:
```bash
# Clean project compile validation
dotnet build DreamGamesCase.sln --no-restore
```

### Editor & Runtime Diagnostics
* **Level Selector & Custom Editor**: Easily test, load, and debug levels with custom editor windows ([GameDebugWindow.cs](file:///c:/Users/muzca/Desktop/muzo-genel/DEV/DreamGames/DreamGamesCase/Assets/Scripts/Editor/GameDebugWindow.cs)).
* **Runtime IMGUI Debugger**: A developer panel accessible in-game to trigger shuffles, scale animations, reload boards, add moves, and export full session logs.

---

## 🚀 Getting Started

### Prerequisites
* Unity Editor **6000.0.59f2** or later.

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/muzaffercanan/boom-boom-cube.git
   ```
2. Open the project inside **Unity Hub**.
3. Load the initial entry point scene: `Assets/Scenes/MainScene.unity`.
4. Press **Play** in the Unity Editor.

---

## 👥 Credits & Preview

Developed by **Muzaffer Canan**.

<p align="center">
  <img width="280" alt="Main Scene" src="https://github.com/user-attachments/assets/91af0095-02aa-4c3e-a992-3f93a798f7a1" />
  <img width="280" alt="Gameplay Grid" src="https://github.com/user-attachments/assets/7fca2d18-1f17-4de3-86ce-020192befa37" />
  <img width="280" alt="Level Completed" src="https://github.com/user-attachments/assets/8eb4f200-63d5-433c-bbb9-15c648ab02d0" />
</p>

---
*Built with ❤️ in Unity*
