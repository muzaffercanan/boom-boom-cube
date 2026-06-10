# Bağımlılık Grafiği (Dependency Graph)

> **Purpose**: Hangi sınıf hangi sınıfa bağımlı? Agent'lar bir sınıfta değişiklik yaparken etki alanını görebilir.

---

## Katman Bağımlılıkları (Üstten Alta)

```
┌─────────────────────────────────────────────────────┐
│                  MonoBehaviours                      │
│  GameManager, UIManager, AudioManager,               │
│  SceneTransitionManager, BoardSetupController,       │
│  ConfettiManager, GameDebugPanel                     │
├──────────────────────┬──────────────────────────────┤
│                      ▼                              │
│            Gameplay Logic Layer                      │
│  TurnProcessor, BoardInputRouter, CubeTurnHandler,   │
│  RocketTurnHandler, DamageResolver, TurnEndFlow,     │
│  GameStateController, LevelSessionController,        │
│  MoveCounter, TurnLogger, GameplaySystemFactory      │
├──────────────────────┬──────────────────────────────┤
│                      ▼                              │
│             Board Systems Layer                      │
│  GridSystem, MatchSystem, GravitySystem,             │
│  RocketSystem, GoalTracker, BoardResolver,           │
│  BoardFiller, LevelLoader, ItemFactory,              │
│  BoardGeometry, NoMoveScanner, ShuffleSystem,        │
│  RocketHintSystem, ParticlePool, BoardSnapshot       │
├──────────────────────┬──────────────────────────────┤
│                      ▼                              │
│              Board Items Layer                       │
│  AbstractBoardItem → CubeItem, RocketItem,           │
│  ObstacleItem → BoxItem, StoneItem, VaseItem         │
├──────────────────────┬──────────────────────────────┤
│                      ▼                              │
│               Core + Data Layer                      │
│  Enums, Interfaces, ItemIds, GameEvents,             │
│  GameRng, SessionLog, LevelData, LevelRepository     │
└─────────────────────────────────────────────────────┘
```

> **Kural**: Bağımlılık yönü her zaman **yukarıdan aşağıya**. Alt katmanlar üst katmanları bilmez.

---

## Sınıf Bazlı Bağımlılık Matrisi

### GameManager (Composition Root)
```
GameManager
  ├── GameplaySystemFactory    → tüm sistemleri oluşturur
  ├── GameplaySystems          → referansları tutar
  ├── IAudioService            → ses
  ├── IProgressService         → ilerleme
  ├── IGameplayEventBus        → event'ler
  ├── TurnEndFlow              → hamle sonu
  ├── NoMoveScanner            → hamle kontrolü
  ├── ShuffleSystem            → karıştırma
  └── ShuffleVisualController  → karıştırma görseli
```

### GameplaySystemFactory (creates):
```
GameplaySystemFactory
  ├── creates → GoalTracker
  ├── creates → GridSystem
  ├── creates → MatchSystem (← GridSystem)
  ├── creates → GravitySystem (← GridSystem, BoardGeometry)
  ├── creates → RocketHintSystem (← GridSystem, MatchSystem)
  ├── creates → BoardFiller (← GridSystem, ItemFactory, BoardGeometry)
  ├── creates → BoardResolver (← GravitySystem, BoardFiller)
  ├── creates → SessionLog
  ├── creates → MoveCounter (← EventBus)
  ├── creates → DamageResolver (← GridSystem, GoalTracker, EventBus)
  ├── creates → TurnLogger (← SessionLog, GridSystem, GoalTracker, MoveCounter)
  ├── creates → GameStateController (← GoalTracker, IProgressService, EventBus)
  ├── creates → LevelSessionController (← SessionLog, IProgressService)
  ├── creates → CubeTurnHandler (← MatchSystem, BoardResolver, BoardFiller, DamageResolver, ...)
  ├── creates → RocketTurnHandler (← BoardResolver, MoveCounter, TurnLogger)
  ├── creates → TurnProcessor (← MoveCounter, CubeTurnHandler, RocketTurnHandler, DamageResolver)
  ├── creates → RocketSystem (← GridSystem, ItemFactory, TurnProcessor.HandleDamage)
  ├── creates → BoardInputRouter (← GridSystem, TurnProcessor, GameStateController, BoardGeometry)
  └── creates → LevelLoader (← ItemFactory, BoardGeometry)
```

### TurnProcessor
```
TurnProcessor
  ├── MoveCounter
  ├── CubeTurnHandler
  ├── RocketTurnHandler
  ├── DamageResolver
  ├── Action _onTurnComplete (callback)
  └── Func<IEnumerator> _onBoardStableBeforeInput (callback)
```

### BoardInputRouter
```
BoardInputRouter
  ├── GridSystem
  ├── TurnProcessor
  ├── GameStateController
  ├── MonoBehaviour (coroutine runner)
  ├── BoardGeometry
  └── AudioClip tapSfx
```

### DamageResolver
```
DamageResolver
  ├── GridSystem
  ├── GoalTracker
  ├── RocketSystem (late-bound via SetRocketSystem)
  ├── IBoardItemViewLifecycle
  └── Action<Dictionary<string,int>> _onGoalsChanged
```

### RocketSystem
```
RocketSystem
  ├── GridSystem
  ├── ItemFactory
  ├── BoardGeometry
  ├── MonoBehaviour (coroutine runner)
  ├── IBoardItemViewLifecycle
  ├── Action<Vector2Int> _onDamageRequest (→ TurnProcessor.HandleDamage)
  └── ObjectPool<GameObject> (projectile pool)
```

### UIManager
```
UIManager
  ├── ItemFactory (sprite'lar için)
  ├── ConfettiManager
  ├── IGameplayEventBus (event subscribe)
  ├── ISceneLoadService
  └── GoalItemView prefab
```

---

## Circular Dependency Çözümü

`TurnProcessor` ↔ `RocketSystem` arasında circular bağımlılık var:
- `TurnProcessor` → `DamageResolver` → `RocketSystem.TriggerRocket()` (chain reaction)
- `RocketSystem` → `_onDamageRequest` → `TurnProcessor.HandleDamage()`

**Çözüm**: Late binding.
1. `GameplaySystemFactory` önce `TurnProcessor`'ı oluşturur.
2. Sonra `RocketSystem`'ı oluştururken `TurnProcessor.HandleDamage`'i callback olarak verir.
3. Son olarak `TurnProcessor.SetRocketSystem(rocketSystem)` ile bağlar.

```csharp
// GameplaySystemFactory.Create():
TurnProcessor turnProcessor = new TurnProcessor(...);
RocketSystem rocketSystem = new RocketSystem(..., turnProcessor.HandleDamage, ...);
turnProcessor.SetRocketSystem(rocketSystem);
```

---

## Event Akışı (Subscriber Haritası)

```
GameEvents.OnLevelLoaded    ← UIManager.OnLevelLoaded
GameEvents.OnMovesUpdated   ← UIManager.OnMovesUpdated
GameEvents.OnGoalsUpdated   ← UIManager.OnGoalsUpdated
GameEvents.OnLevelWon       ← UIManager.OnLevelWin
GameEvents.OnLevelLost      ← UIManager.OnLevelLose
```

Event'ler `StaticGameplayEventBus` üzerinden fırlatılır, static `GameEvents` sınıfına proxy yapar.

---

## Service Interface Haritası

| Interface | Default Implementasyon | Sorumluluk |
|---|---|---|
| `IAudioService` | `UnityAudioService` | AudioManager singleton'a proxy |
| `IProgressService` | `PlayerPrefsProgressService` | ProgressService static'e proxy |
| `IGameplayEventBus` | `StaticGameplayEventBus` | GameEvents static'e proxy |
| `ISceneLoadService` | `UnitySceneLoadService` | SceneTransitionManager'a proxy |
| `IBoardItemViewLifecycle` | `UnityBoardItemViewLifecycle` | GameObject.Destroy wrapper |
