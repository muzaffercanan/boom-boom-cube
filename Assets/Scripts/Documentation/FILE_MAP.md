# Dosya Haritası (File Map)

> **Purpose**: Her dosyanın tam yolu, namespace'i ve tek cümlelik açıklaması.  
> Agent'lar belirli bir dosyayı ararken bu haritayı kullanabilir.

## Scripts/Core/

| Dosya | Namespace | Açıklama |
|---|---|---|
| `Core/Enums/GameEnums.cs` | `DreamGames.Core` | `ItemType`, `ItemId`, `CubeColor`, `DamageType` enum tanımları |
| `Core/Interfaces/IBoardItem.cs` | `DreamGames.Core` | Board itemlerinin temel interface'i |
| `Core/Interfaces/IDamageable.cs` | `DreamGames.Core` | Hasar alabilir itemler: `TakeDamage()`, `Health` |
| `Core/Interfaces/IFallable.cs` | `DreamGames.Core` | Düşebilir itemler: `CanFall()`, `FallTo()` |
| `Core/Interfaces/IMatchable.cs` | `DreamGames.Core` | Eşleştirilebilir itemler: `GetColor()`, `CanMatch()` |
| `Core/GameEvents.cs` | `DreamGames.Core` | Statik event bus: level loaded, moves, goals, win, lose |
| `Core/GameRng.cs` | `DreamGames.Core` | Deterministic RNG (singleton `Shared`). `UnityEngine.Random` yerine kullan |
| `Core/ItemIds.cs` | `DreamGames.Core` | String ID ↔ `ItemId` enum dönüşüm tablosu ve sabitler |
| `Core/SceneNames.cs` | `DreamGames.Core` | Sahne adı sabitleri: `Main`, `Level` |
| `Core/LevelSessionSeed.cs` | `DreamGames.Core` | Session seed üretimi (deterministic + runtime) |
| `Core/SessionLog.cs` | `DreamGames.Core` | Hamle kaydı tutucu: `SessionLog` + `TurnLogEntry` |

## Scripts/Data/

| Dosya | Namespace | Açıklama |
|---|---|---|
| `Data/LevelData.cs` | `DreamGames.Data` | Level JSON veri modeli: `LevelData` + `LevelCellData` |
| `Data/LevelRepository.cs` | `DreamGames.Data` | Level yükleme (Resources → StreamingAssets → fallback) + validasyon |

## Scripts/Board/Items/

| Dosya | Namespace | Açıklama |
|---|---|---|
| `Board/Items/AbstractBoardItem.cs` | `DreamGames.Board.Items` | Tüm itemlerin base class'ı: pozisyon, animasyon, particle |
| `Board/Items/CubeItem.cs` | `DreamGames.Board.Items` | Renkli küp: `IMatchable`, `IFallable` |
| `Board/Items/RocketItem.cs` | `DreamGames.Board.Items` | Roket power-up: `IFallable`, `IsHorizontal` |
| `Board/Items/ObstacleItem.cs` | `DreamGames.Board.Items` | Engel base class: `IDamageable`, `Health` |
| `Board/Items/BoxItem.cs` | `DreamGames.Board.Items` | Kutu engeli: HP=1, blast ile kırılır |
| `Board/Items/StoneItem.cs` | `DreamGames.Board.Items` | Taş engeli: HP=1, sadece roket ile kırılır, düşmez |
| `Board/Items/VaseItem.cs` | `DreamGames.Board.Items` | Vazo engeli: HP=2, düşebilir, her damage ile kırılır |

## Scripts/Board/Systems/

| Dosya | Namespace | Açıklama |
|---|---|---|
| `Board/Systems/GridSystem.cs` | `DreamGames.Board.Systems` | 2D grid state yönetimi: `IBoardItem[,]` + `BoardCellState[,]` |
| `Board/Systems/BoardCellState.cs` | `DreamGames.Board.Systems` | Hücre durumu (Normal/Hole/Blocked/Locked) readonly struct |
| `Board/Systems/BoardCellLayout.cs` | `DreamGames.Board.Systems` | Grid layout hesaplaması |
| `Board/Systems/MatchSystem.cs` | `DreamGames.Board.Systems` | BFS ile aynı renk küp gruplarını bulur |
| `Board/Systems/GravitySystem.cs` | `DreamGames.Board.Systems` | Sütun bazlı yerçekimi: animated + immediate |
| `Board/Systems/RocketSystem.cs` | `DreamGames.Board.Systems` | Roket tetikleme, combo, projectile spawn, pool yönetimi |
| `Board/Systems/GoalTracker.cs` | `DreamGames.Board.Systems` | Engel hedef sayımı ve tamamlanma kontrolü |
| `Board/Systems/ItemFactory.cs` | `DreamGames.Board.Systems` | ScriptableObject: prefab mapping, item oluşturma, görsel ayar |
| `Board/Systems/BoardResolver.cs` | `DreamGames.Board.Systems` | Gravity → fill → hints koordinasyonu |
| `Board/Systems/BoardFiller.cs` | `DreamGames.Board.Systems` | Boş hücrelere yeni küp spawn |
| `Board/Systems/LevelLoader.cs` | `DreamGames.Board.Systems` | LevelData → GridSystem item yerleştirme |
| `Board/Systems/BoardGeometry.cs` | `DreamGames.Board.Systems` | Hücre ↔ world/local/screen koordinat dönüşümü |
| `Board/Systems/NoMoveScanner.cs` | `DreamGames.Board.Systems` | Board'da oynanabilir hamle var mı taraması |
| `Board/Systems/ShuffleSystem.cs` | `DreamGames.Board.Systems` | Küpleri karıştırma (max N deneme ile) |
| `Board/Systems/RocketHintSystem.cs` | `DreamGames.Board.Systems` | 4+ küp eşleşme gruplarına görsel hint |
| `Board/Systems/ParticlePool.cs` | `DreamGames.Board.Systems` | ParticleSystem instance pooling |
| `Board/Systems/BoardSnapshot.cs` | `DreamGames.Board.Systems` | Board state snapshot (debug/log için) |
| `Board/Systems/BoardItemViewLifecycle.cs` | `DreamGames.Board.Systems` | `IBoardItemViewLifecycle` + Unity implementasyonu |
| `Board/Systems/TurnEndResolver.cs` | `DreamGames.Board.Systems` | Hamle sonu shuffle kararı |

## Scripts/Board/Visuals/

| Dosya | Namespace | Açıklama |
|---|---|---|
| `Board/Visuals/BoardVisualConfig.cs` | `DreamGames.Board.Visuals` | ScriptableObject: item bazlı scale, offset, collider, sort bias |
| `Board/Visuals/RocketProjectile.cs` | `DreamGames.Board.Visuals` | Roket mermisi hareket ve çarpışma MonoBehaviour |
| `Board/Visuals/ShuffleVisualController.cs` | `DreamGames.Board.Visuals` | Shuffle fade transition controller |

## Scripts/Managers/

| Dosya | Namespace | Açıklama |
|---|---|---|
| `Managers/GameManager.cs` | `DreamGames.Gameplay` | Ana MonoBehaviour: init, input, level lifecycle, debug API |
| `Managers/GameManagerConfig.cs` | `DreamGames.Gameplay` | Config sınıfları: References, Rules, Animation, Session, Audio |
| `Managers/GameplaySystemFactory.cs` | `DreamGames.Gameplay` | Composition root: tüm gameplay sistemlerini oluşturur |
| `Managers/GameplaySystems.cs` | `DreamGames.Gameplay` | System container (tüm sistem referanslarını tutar) |
| `Managers/GameplayServices.cs` | `DreamGames.Gameplay` | Service interface'leri ve implementasyonları |
| `Managers/GameStateController.cs` | `DreamGames.Gameplay` | Win/lose kontrolü |
| `Managers/TurnProcessor.cs` | `DreamGames.Gameplay` | Hamle işleme merkezi (cube + rocket turn handling) |
| `Managers/CubeTurnHandler.cs` | `DreamGames.Gameplay` | Küp tıklama hamle işleme detayı |
| `Managers/RocketTurnHandler.cs` | `DreamGames.Gameplay` | Roket tıklama hamle işleme detayı |
| `Managers/BoardInputRouter.cs` | `DreamGames.Gameplay` | Screen/world → grid hücre çözümleme + click routing |
| `Managers/DamageResolver.cs` | `DreamGames.Gameplay` | Hasar işleme: roket/blast damage + goal tracking |
| `Managers/TurnEndFlow.cs` | `DreamGames.Gameplay` | Hamle sonu akışı: state check → shuffle → visual |
| `Managers/LevelSessionController.cs` | `DreamGames.Gameplay` | Level yükleme session yönetimi |
| `Managers/MoveCounter.cs` | `DreamGames.Gameplay` | Hamle sayacı wrapper |
| `Managers/TurnLogger.cs` | `DreamGames.Gameplay` | SessionLog'a turn bilgisi yazma |
| `Managers/TurnExecutionResult.cs` | `DreamGames.Gameplay` | Turn işleme sonuç container'ı |
| `Managers/UIManager.cs` | `DreamGames.Gameplay` | HUD, win/lose panelleri, goal ikonları |
| `Managers/AudioManager.cs` | `DreamGames.Gameplay` | Singleton audio yönetimi |
| `Managers/SceneTransitionManager.cs` | `DreamGames.Gameplay` | Fade ile sahne geçişi |
| `Managers/ProgressService.cs` | `DreamGames.Gameplay` | PlayerPrefs level ilerleme |
| `Managers/ConfettiManager.cs` | `DreamGames.Gameplay` | Win konfeti efekti |
| `Managers/BoardSetupController.cs` | `DreamGames.Gameplay` | Board background ve kamera setup |
| `Managers/BoardDebug.cs` | `DreamGames.Gameplay` | Runtime board debug bilgileri |
| `Managers/GameDebug.cs` | `DreamGames.Gameplay` | Global debug ayarları (SpeedMultiplier, ItemScale) |
| `Managers/GameDebugPanel.cs` | `DreamGames.Gameplay` | Runtime IMGUI debug paneli |

## Scripts/ScriptableObjects/

| Dosya | Namespace | Açıklama |
|---|---|---|
| `ScriptableObjects/LevelConfig.cs` | `DreamGames.Data` | ScriptableObject: rows, columns, minGroupSize, rocketCreateGroupSize, cellSize |

## Scripts/UI/

| Dosya | Namespace | Açıklama |
|---|---|---|
| `UI/GoalItemView.cs` | `DreamGames.UI` | Tek bir goal item UI widget'ı (ikon + sayı) |

## Scripts/ (Root)

| Dosya | Namespace | Açıklama |
|---|---|---|
| `LevelButtonController.cs` | — | Ana menüdeki level buton kontrolü |

## Scripts/Editor/

| Dosya | Namespace | Açıklama |
|---|---|---|
| `Editor/GameDebugWindow.cs` | — | Custom editor penceresi |
| `Editor/GameTools.cs` | — | Editor menü araçları |
| `Editor/LevelSelectorWindow.cs` | — | Level seçim editor penceresi |
| `Editor/LevelSceneReferenceRepairer.cs` | — | Sahne referans onarımı |
| `Editor/LevelTools.cs` | — | Level editor araçları |

## Tests/

| Dosya | Tür | Açıklama |
|---|---|---|
| `Tests/EditMode/BoardSystemsEditModeTests.cs` | NUnit/EditMode | Grid, Match, Gravity, Rocket, Goal, Factory, Repository testleri |
| `Tests/PlayMode/GameManagerPlayModeSmokeTests.cs` | NUnit/PlayMode | GameManager entegrasyon smoke testleri |

## Levels/ (`Assets/Levels/`)

10 adet level JSON dosyası: `level_01.json` – `level_10.json`

## Prefabs/ (`Assets/Prefabs/`)

| Alt Klasör | İçerik |
|---|---|
| `Cubes/` | Renkli küp prefab'ları |
| `Rockets/` | Roket prefab'ları |
| `Obstacles/` | Box, Stone, Vase prefab'ları |
| `Cube_Particles/` | Küp patlama particle'ları |
| `Box_Particles/` | Box patlama particle'ları |
| `Stone_Particles/` | Stone patlama particle'ları |
| `Vase_Particles/` | Vase patlama particle'ları |
| `goalItem.prefab` | Goal UI item prefab'ı |
