# Mimari & Sistem Referansı

> **Purpose**: Her sınıfın sorumluluğu, bağımlılıkları ve public API'si. Agent'lar bu dosyayı okuyarak tüm codebase'i token okumadan anlayabilir.

---

## Core Katmanı (`Scripts/Core/`)

### Enums (`GameEnums.cs`)

```csharp
enum ItemType     { None, Cube, Rocket, Obstacle }
enum ItemId       { Unknown, Red, Green, Blue, Yellow, Box, Stone, Vase, Random,
                    HorizontalRocket, VerticalRocket, RocketHorizontalPartLeft,
                    RocketHorizontalPartRight, RocketVerticalPartBottom,
                    RocketVerticalPartTop, RocketProjectile }
enum CubeColor    { Red, Green, Blue, Yellow, Random }
enum DamageType   { MatchBlast, RocketHit }
```

### Interfaces

| Interface | Dosya | Amaç |
|---|---|---|
| `IBoardItem` | `Interfaces/IBoardItem.cs` | Board üzerindeki tüm itemlerin temel sözleşmesi. `X`, `Y`, `SetPosition()`, `Init()`, `GetItemType()`, `GetGameObject()`, `PlayDestroyEffect()` |
| `IMatchable` | `Interfaces/IMatchable.cs` | Eşleştirilebilir itemler: `GetColor()`, `CanMatch()` |
| `IFallable` | `Interfaces/IFallable.cs` | Düşebilir itemler: `CanFall()`, `FallTo(targetY, duration)` |
| `IDamageable` | `Interfaces/IDamageable.cs` | Hasar alabilir itemler: `TakeDamage(DamageType)`, `Health` |

### ItemIds (Statik Sabitler)

String ID ↔ `ItemId` enum arasında dönüşüm. Level JSON'ındaki string kodları ile enum arasında köprü.

```
"r"→Red, "g"→Green, "b"→Blue, "y"→Yellow
"bo"→Box, "s"→Stone, "v"→Vase, "rand"→Random
"hro"→HorizontalRocket, "vro"→VerticalRocket
```

### GameEvents (Statik Event Bus)

```csharp
// Olaylar
OnLevelLoaded(LevelData, Dictionary<string,int> goals)
OnMovesUpdated(int moves)
OnGoalsUpdated(Dictionary<string,int> goals)
OnLevelWon()
OnLevelLost()
```

### GameRng (Deterministic Random)

- Singleton pattern: `GameRng.Shared`
- Seed ile başlatılabilir: `GameRng.SetSharedSeed(seed)`
- Testlerde deterministic davranış sağlar
- `Range(min, max)`, `Value()` metotları

### SessionLog & TurnLogEntry

Her hamlenin kaydını tutar. JSON-like export ile replay/debug imkanı sağlar.
- `BeginLevel(levelId, seed)`, `RecordTurn(...)`, `MarkLastTurnShuffle(...)`
- `ToJsonLikeString()` → tüm session'ı JSON olarak export eder

---

## Data Katmanı (`Scripts/Data/`)

### LevelData

```csharp
class LevelData {
    int level_number;
    int grid_width, grid_height;
    int move_count;
    List<string> grid;           // legacy flat array
    List<LevelCellData> cells;   // yeni hücre-bazlı format
}
```

- `HasCellLayout` → cells listesi dolu mu?
- `GetItemIdAt(index)` → cells varsa cells'den, yoksa grid'den okur
- `EnumerateItemIds()` → tüm hücreleri iterate eder

### LevelCellData

```csharp
class LevelCellData {
    string cell_type;  // "normal", "hole", "blocked", "locked"
    string item;       // "r", "bo", "s", vb.
    bool locked;
}
```

### LevelRepository (Statik)

Level yükleme chain'i:
1. `Resources/Levels/level_XX` (TextAsset)
2. `StreamingAssets/Levels/level_XX.json` (async)
3. Inspector'dan atanmış fallback TextAsset

`ParseAndValidate()` → JSON parse + `Validate()` ile veri bütünlüğü kontrolü.

---

## Board Systems (`Scripts/Board/Systems/`)

### GridSystem

2D board state yöneticisi.

```csharp
// Temel API
Initialize(width, height)
Initialize(width, height, BoardCellState[,] cells)
GetItem(x, y) → IBoardItem
SetItem(x, y, item)
ClearCell(x, y)
RemoveItem(x, y) → IBoardItem

// Hücre sorguları
IsValid(x, y)         // bounds + playable
IsInBounds(x, y)
IsPlayableCell(x, y)
CanHoldItem(x, y)
CanSpawnItem(x, y)
BlocksFall(x, y)
BlocksRocket(x, y)
GetCellState(x, y) → BoardCellState
```

### BoardCellState (readonly struct)

Hücre türleri ve özellikleri:

| Tür | Exists | Playable | CanHold | CanSpawn | BlocksFall | BlocksRocket |
|---|---|---|---|---|---|---|
| **Normal** | ✓ | ✓ | ✓ | ✓ | ✗ | ✗ |
| **Hole** | ✗ | ✗ | ✗ | ✗ | ✓ | ✓ |
| **Blocked** | ✓ | ✗ | ✗ | ✗ | ✓ | ✓ |
| **Locked** | ✓ | ✓ | ✓ | ✗ | ✗ | ✗ |

### MatchSystem

BFS ile bağlı küp gruplarını bulur.

```csharp
FindMatches(startX, startY) → List<IBoardItem>      // BFS flood fill
GetAdjacentObstacles(matchedItems) → List<IBoardItem> // komşu engeller
```

### GravitySystem

Sütun bazlı compact: boş hücrelere düşürme.

```csharp
ApplyGravityAndAnimate() → float  // animasyonlu, max süre döner
ApplyGravity() → bool             // anında (test/shuffle için)
```

- Non-fallable itemler (stone, box) "zemin" gibi davranır ve üstündeki grup yeniden başlar.
- Stagger delay ile waterfall cascade efekti.

### RocketSystem

Roket tetikleme ve mermi (projectile) yönetimi.

```csharp
TryProcessRocketClick(x, y, rocket, out isCombo) → bool
TriggerRocket(x, y, rocket)           // tekil roket
WaitForProjectilesToComplete(timeout)  // coroutine
CancelActiveProjectiles()
```

- Combo: 2 bitişik roket → 3 yatay + 3 dikey beam
- Projectile'lar `ObjectPool<GameObject>` ile yönetilir
- Her çarpma → `_onDamageRequest(Vector2Int)` callback'i

### GoalTracker

Engel sayımı ve tamamlanma kontrolü.

```csharp
Initialize(itemIds)              // level başında
TryRecordDestroyed(item) → bool  // engel patladığında
IsComplete → bool                // tüm hedefler 0'a düştü mü
Counts → Dictionary<string,int>  // mevcut hedef durumu
```

Hedef itemler: sadece `Box`, `Stone`, `Vase`.

### ItemFactory (ScriptableObject)

Prefab ↔ ItemId eşleştirmesi ve item oluşturma.

```csharp
CreateItem(id, parent, cellSize) → IBoardItem
CreateVisual(id, parent) → GameObject
GetPrefab(id) → GameObject
GetSprite(id) → Sprite  // UI ikonları için
ApplyVisualSettings(instance, itemId, cellSize)
```

- `ItemId.Random` → `GameRng.Shared` ile rastgele renk seçimi
- `BoardVisualConfig` üzerinden scale, offset, collider ayarları

### BoardResolver

Gravity + refill + hint update koordinasyonu.

```csharp
ApplyGravityAndFillSequence()  // coroutine: gravity → fill → gravity → hints
ResolveImmediate()             // anında (test/shuffle için)
```

### BoardFiller

Boş hücrelere yeni küpler spawn eder.

### LevelLoader

`LevelData` → `GridSystem`'e item yerleştirme.

### BoardGeometry

Hücre koordinatları ↔ world/local/screen pozisyonları dönüşümü.

### NoMoveScanner

Board'da oynanabilir hamle var mı kontrolü.

### ShuffleSystem

Hamle kalmadığında küpleri karıştırır (max N deneme).

### RocketHintSystem

4+ küp eşleşen gruplara görsel hint verir.

### ParticlePool

Particle system instance'larını pool'lar.

---

## Board Items (`Scripts/Board/Items/`)

### Kalıtım Hiyerarşisi

```
MonoBehaviour
└── AbstractBoardItem : IBoardItem, IPointerClickHandler
    ├── CubeItem : IMatchable, IFallable
    ├── RocketItem : IFallable
    └── ObstacleItem : IDamageable
        ├── BoxItem
        ├── StoneItem
        └── VaseItem : IFallable
```

### AbstractBoardItem

Tüm board itemlerinin base class'ı:
- Pozisyon yönetimi (`SetPosition`, `X`, `Y`)
- Sorting order (Y bazlı)
- DOTween animasyonları: `MoveToPosition()`, `FallToPosition()`, `FallToPositionDelayed()`, `SnapToPosition()`
- Particle efekt spawning: `SpawnParticle()`

### CubeItem

- `CubeColor` ile renk bilgisi
- `IMatchable`: `GetColor()`, `CanMatch()` (her zaman true)
- `IFallable`: `CanFall()` (her zaman true)
- Patladığında renk bazlı particle efekti

### RocketItem

- `IsHorizontal` → yatay mı dikey mi
- `IFallable`: düşebilir
- Tıklanınca `RocketSystem` tarafından tetiklenir

### ObstacleItem (abstract)

- `IDamageable`: `TakeDamage(type)`, `Health`
- `Health` başlangıçta `_maxHealth` (subclass'larda set edilir)

### BoxItem

- `_maxHealth = 1`
- Sadece `MatchBlast` ile hasar alır (roket ile almaz, çünkü `TakeDamage` override'ı yok, base `ObstacleItem.TakeDamage` DamageType kontrolü yapmaz → aslında her ikisiyle de hasar alır)

### StoneItem

- `_maxHealth = 1`
- Sadece `RocketHit` ile hasar alır (`TakeDamage` override)
- `IFallable` DEĞİL → düşmez, yerçekiminde "zemin" görevi görür

### VaseItem

- `_maxHealth = 2` (ilk darbe görsel değiştirir, ikinci darbe patlatır)
- `IFallable`: düşebilir
- Her iki `DamageType` ile hasar alır

---

## Gameplay Katmanı (`Scripts/Managers/`)

### GameManager (MonoBehaviour — Ana Giriş Noktası)

- `Start()` → sistemleri init eder, level yükler
- `Update()` → mouse/touch input'u yakalar
- `GameplaySystemFactory` ile tüm sistemleri oluşturur
- Config grupları: `BoardSceneReferences`, `LevelLoadConfig`, `GameplayRulesConfig`, `SessionConfig`, `GameAudioConfig`
- Debug API: `DebugAddMoves()`, `DebugForceCompleteGoals()`, `DebugLoadLevel()`, `DebugForceShuffle()`, `DebugReloadWithSize()`, `ExportSessionLog()`

### TurnProcessor

Hamle işleme merkezi.

```csharp
ProcessCubeTurn(x, y)               // coroutine
ProcessRocketTurn(x, y, rocket)     // coroutine
HandleDamage(Vector2Int)            // roket damage callback
IsProcessingTurn → bool             // re-entrant koruması
RemainingMoves → int
```

### BoardInputRouter

Screen/world pozisyonundan grid hücresine dönüşüm + click routing.

```csharp
TryHandleScreenPosition(screenPos, camera) → bool
OnItemClicked(x, y)
```

- CubeItem tıklaması → `TurnProcessor.ProcessCubeTurn()`
- RocketItem tıklaması → `TurnProcessor.ProcessRocketTurn()`

### CubeTurnHandler / RocketTurnHandler

Turn processing detayları (match bulma, damage uygulama, roket oluşturma, board resolve).

### DamageResolver

Hasar işleme merkezi.

```csharp
HandleRocketDamage(Vector2Int)                    // roket çarpması
DamageAdjacentObstacles(IEnumerable<IBoardItem>)  // blast komşu hasarı
DestroyItemWithEffect(x, y, DamageType)           // item yok etme + goal tracking
```

### GameStateController

Win/lose durumu kontrolü.

```csharp
CheckAndResolve(remainingMoves, levelNumber)  // goals complete? moves = 0?
IsGameOver → bool
Reset()
```

### TurnEndFlow

Hamle sonrası akış: `CheckAndResolve` → shuffle kontrolü → visual transition.

### LevelSessionController

Level yükleme session'ı: seed başlatma, `LevelRepository` çağrısı, `SessionLog.BeginLevel()`.

### UIManager (MonoBehaviour)

HUD, win/lose panelleri, goal ikonları.
- `IGameplayEventBus` üzerinden event dinler
- `GoalItemView` prefab'ını instantiate eder

### MoveCounter

Hamle sayacı (basit wrapper).

### TurnLogger

`SessionLog`'a turn bilgisi yazma.

### AudioManager (MonoBehaviour — Singleton)

`Instance` pattern. `PlayMusic()`, `PlaySFX()`.

### SceneTransitionManager (MonoBehaviour — Singleton)

Fade transition ile sahne geçişi.

### ProgressService (Statik)

`PlayerPrefs` üzerinden level ilerleme yönetimi.

---

## Görsel Sistem (`Scripts/Board/Visuals/`)

### BoardVisualConfig (ScriptableObject)

Item bazlı görsel ayarları: scale, offset, sorting bias, collider boyutu.

### RocketProjectile (MonoBehaviour)

Roket mermisi hareketi ve çarpışma. Her frame hücre bazlı ilerler, çarpınca `onHitCell` callback'i tetikler.

### ShuffleVisualController

Shuffle sırasında fade-in/out geçişi.

---

## Konfigürasyon Sınıfları (`Scripts/Managers/GameManagerConfig.cs`)

| Sınıf | Sorumluluk |
|---|---|
| `BoardSceneReferences` | Inspector referansları (ItemFactory, BoardParent, CellSize, vb.) |
| `LevelLoadConfig` | Level JSON ve debug log ayarı |
| `GameplayRulesConfig` | MinMatchSize(2), RocketMatchSize(4), shuffle ayarları |
| `BoardAnimationConfig` | Fall duration, stagger delay, bounce height/duration, spawn ayarları |
| `SessionConfig` | Seed override, session log, snapshot ayarları |
| `GameAudioConfig` | AudioClip referansları |

---

## Editor Araçları (`Scripts/Editor/`)

| Dosya | Amaç |
|---|---|
| `GameDebugWindow.cs` | Editor penceresi: board state görüntüleme, debug komutları |
| `GameTools.cs` | Editor menü araçları |
| `LevelSelectorWindow.cs` | Level seçim penceresi |
| `LevelSceneReferenceRepairer.cs` | Sahne referans onarımı |
| `LevelTools.cs` | Level ile ilgili editor araçları |

## Test Yapısı

| Dosya | Tür | İçerik |
|---|---|---|
| `Tests/EditMode/BoardSystemsEditModeTests.cs` | Edit Mode | Grid, Match, Gravity, Rocket, Goal, ItemFactory, LevelRepository testleri |
| `Tests/PlayMode/GameManagerPlayModeSmokeTests.cs` | Play Mode | GameManager entegrasyon smoke testleri |
