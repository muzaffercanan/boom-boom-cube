# Veri Akışları (Data Flows)

> **Purpose**: Oyundaki ana akışların adım adım izlenmesi.  
> Agent'lar bir sistemde değişiklik yaparken hangi akışları etkileyeceğini anlayabilir.

---

## 1. Level Yükleme Akışı

```
GameManager.Start()
  │
  ├── EnsureServices()         // IAudioService, IProgressService, IGameplayEventBus
  ├── SyncGroupedConfigFromLegacy()
  ├── SyncVisualConfig()
  ├── ParticlePool.Clear()
  ├── AudioService.PlayMusic()
  │
  └── InitNextFrame() [coroutine, 1 frame bekle]
        │
        ├── InitializeSystems()
        │     └── GameplaySystemFactory.Create(config) → GameplaySystems
        │           ├── new GoalTracker()
        │           ├── new GridSystem()
        │           ├── new MatchSystem(grid)
        │           ├── new GravitySystem(grid, geometry, animation)
        │           ├── new RocketHintSystem(grid, match, rocketSize)
        │           ├── new BoardFiller(grid, factory, parent, geometry)
        │           ├── new BoardResolver(gravity, filler, hints)
        │           ├── new SessionLog(writeToConsole)
        │           ├── new MoveCounter(eventBus.RaiseMovesUpdated)
        │           ├── new DamageResolver(grid, goals, eventBus)
        │           ├── new TurnLogger(sessionLog, grid, goals, moveCounter)
        │           ├── new GameStateController(goals, playSound, sfx, progress, eventBus)
        │           ├── new LevelSessionController(json, seed, log, progress)
        │           ├── new CubeTurnHandler(match, resolver, filler, damage, ...)
        │           ├── new RocketTurnHandler(resolver, moveCounter, turnLogger)
        │           ├── new TurnProcessor(moveCounter, cubeTurn, rocketTurn, damage, ...)
        │           ├── new RocketSystem(grid, factory, parent, geometry, runner, damage, sfx)
        │           ├── new BoardInputRouter(grid, turnProcessor, state, runner, ...)
        │           └── new LevelLoader(factory, parent, geometry)
        │
        └── LoadCurrentLevelRoutine() [coroutine]
              │
              ├── PrepareBoardForLevel()    // LevelButton.hide, BoardParent.show
              └── LevelSessionController.LoadSelectedLevelRoutine(ApplyLoadedLevel)
                    │
                    ├── ProgressService.GetSelectedLevel() → levelNumber
                    ├── LevelSessionSeed.BeginSession(levelNumber, ...) → seed
                    ├── SessionLog.BeginLevel(levelNumber, seed)
                    └── LevelRepository.LoadLevelAsync(levelNumber, callback)
                          │
                          ├── 1. TryLoadFromResources("Levels/level_XX")
                          ├── 2. TryLoadFromStreamingAssets("level_XX.json")
                          └── 3. fallback TextAsset (Inspector'dan)

              ↓ callback: ApplyLoadedLevel(levelData)

              ├── GoalTracker.Initialize(levelData.EnumerateItemIds())
              ├── GameStateController.Reset()
              ├── TurnProcessor.ResetTurnState()
              ├── TurnProcessor.SetRemainingMoves(levelData.move_count)
              ├── LevelLoader.LoadLevel(gridSystem, levelData)
              ├── BoardSetupController.SetupForLevel(...)
              ├── RocketHintSystem.UpdateHints() [next frame]
              ├── EventBus.RaiseLevelLoaded(levelData, goals)
              └── EventBus.RaiseMovesUpdated(remainingMoves)
```

---

## 2. Küp Tıklama Akışı (Cube Turn)

```
GameManager.Update()
  │
  └── HandlePointerInput()
        │
        └── Input.GetMouseButtonUp(0) veya Touch.Ended
              │
              └── BoardInputRouter.TryHandleScreenPosition(mousePos, camera)
                    │
                    ├── BoardGeometry.TryScreenPositionToCell() → (x, y)
                    ├── GridSystem.IsValid(x, y) kontrolü
                    ├── TurnProcessor.IsProcessingTurn kontrolü
                    ├── GameStateController.IsGameOver kontrolü
                    ├── RemainingMoves > 0 kontrolü
                    │
                    ├── item = GridSystem.GetItem(x, y)
                    ├── PlaySound(tapSfx)
                    │
                    └── item is CubeItem → StartCoroutine(TurnProcessor.ProcessCubeTurn(x, y))
                          │
                          └── CubeTurnHandler.ProcessTurn(x, y, result)
                                │
                                ├── MatchSystem.FindMatches(x, y) → matches[]
                                ├── matches.Count < MinMatchSize(2) → return (invalid)
                                │
                                ├── MatchSystem.GetAdjacentObstacles(matches) → obstacles[]
                                │
                                ├── matches.Count >= RocketMatchSize(4)?
                                │     ├── YES: Roket oluştur (küp pozisyonuna)
                                │     │     ├── Yön seçimi (yatay/dikey hesaplama)
                                │     │     └── ItemFactory.CreateItem(rocketId, parent, cellSize)
                                │     └── NO: normal blast
                                │
                                ├── Matched küpleri patlat:
                                │     └── foreach cube → PlayDestroyEffect() → GridSystem.DestroyItem() → Destroy(GO)
                                │
                                ├── DamageResolver.DamageAdjacentObstacles(obstacles)
                                │     └── foreach obstacle → TakeDamage(MatchBlast) → destroyed? → DestroyItemWithEffect()
                                │
                                ├── MoveCounter.Use() → EventBus.RaiseMovesUpdated()
                                ├── TurnLogger.LogTurn(...)
                                ├── PlaySound(matchSfx)
                                │
                                └── BoardResolver.ApplyGravityAndFillSequence() [coroutine]
                                      ├── GravitySystem.ApplyGravityAndAnimate()
                                      ├── WaitForSeconds(animationTime)
                                      ├── BoardFiller.FillEmptySpaces()
                                      ├── RocketHintSystem.UpdateHints()
                                      ├── WaitForSeconds(fillDelay)
                                      ├── GravitySystem.ApplyGravityAndAnimate() (refill sonrası)
                                      └── RocketHintSystem.UpdateHints()

                    ↓ TurnProcessor.CompleteTurnIfProcessed()

                    └── OnBoardStableBeforeInput() [coroutine]
                          │
                          └── TurnEndFlow.RunAfterBoardStable(levelNumber)
                                ├── GameStateController.CheckAndResolve(remainingMoves, levelNumber)
                                │     ├── GoalTracker.IsComplete? → Win
                                │     └── RemainingMoves <= 0? → Lose
                                │
                                ├── SessionLog.UpdateLastTurnResult()
                                │
                                └── (if playing && enableNoMoveShuffle)
                                      ├── NoMoveScanner.HasPlayableMove()?
                                      │     ├── YES: return
                                      │     └── NO: shuffle needed
                                      │
                                      ├── ShuffleVisualController.PlayTransition(fadeIn)
                                      ├── ShuffleSystem.TryShuffleNormalCubesUntilPlayable()
                                      ├── RocketHintSystem.UpdateHints()
                                      └── ShuffleVisualController.PlayTransition(fadeOut)
```

---

## 3. Roket Tıklama Akışı (Rocket Turn)

```
BoardInputRouter → item is RocketItem
  │
  └── TurnProcessor.ProcessRocketTurn(x, y, rocket) [coroutine]
        │
        └── RocketTurnHandler.ProcessTurn(x, y, rocket, result)
              │
              ├── RocketSystem.TryProcessRocketClick(x, y, rocket, out isCombo)
              │     │
              │     ├── FindNeighborRocket(x, y) → komşu roket var mı?
              │     │
              │     ├── COMBO (komşu roket bulundu):
              │     │     ├── Her iki roketi destroy et
              │     │     ├── 3x3 alan damage: _onDamageRequest(pos)
              │     │     ├── 3 yatay beam spawn
              │     │     └── 3 dikey beam spawn
              │     │
              │     └── TEKİL ROKET:
              │           ├── Roketi destroy et
              │           └── SpawnRocketBeams(x, y, isHorizontal, false)
              │                 ├── isHorizontal: Sol + Sağ projectile
              │                 └── isVertical: Alt + Üst projectile
              │
              │     Projectile hareketi (RocketProjectile.cs):
              │     ├── Her frame → bir hücre ilerle
              │     ├── Hücredeki item → OnProjectileHitCell(x, y)
              │     │     └── TurnProcessor.HandleDamage(pos)
              │     │           └── DamageResolver.HandleRocketDamage(pos)
              │     │                 ├── item is RocketItem → TriggerRocket (chain)
              │     │                 ├── item is IDamageable → TakeDamage(RocketHit)
              │     │                 └── else → DestroyItemWithEffect
              │     └── Board dışına çıkınca → pool'a dön
              │
              ├── MoveCounter.Use()
              ├── TurnLogger.LogTurn(...)
              │
              ├── RocketSystem.WaitForProjectilesToComplete(2.5s) [coroutine]
              │
              └── BoardResolver.ApplyGravityAndFillSequence() [coroutine]

        ↓ CompleteTurnIfProcessed → TurnEndFlow (aynı akış)
```

---

## 4. Damage Chain (Hasar Zinciri)

```
DamageResolver.HandleRocketDamage(pos)
  │
  ├── item is RocketItem?
  │     └── RocketSystem.TriggerRocket() → yeni projectile'lar → yeni HandleDamage çağrıları
  │           (ZİNCİR REAKSİYON - recursive)
  │
  ├── item is IDamageable?
  │     ├── TakeDamage(RocketHit)
  │     ├── destroyed? → DestroyItemWithEffect()
  │     │     ├── GoalTracker.TryRecordDestroyed(item) → goal update
  │     │     ├── PlayDestroyEffect()
  │     │     ├── GridSystem.DestroyItem()
  │     │     ├── ViewLifecycle.DestroyView()
  │     │     └── GoalsChanged? → EventBus.RaiseGoalsUpdated()
  │     └── not destroyed? → hasar aldı ama hala hayatta
  │
  └── else (normal cube vb.)?
        └── DestroyItemWithEffect() (hemen yok et)
```

---

## 5. Win/Lose Akışı

```
GameStateController.CheckAndResolve(remainingMoves, levelNumber)
  │
  ├── GoalTracker.IsComplete?
  │     └── WIN:
  │           ├── IsGameOver = true
  │           ├── ProgressService.MarkLevelCompleted(levelNumber)
  │           │     └── PlayerPrefs["LastPlayedLevel"] = levelNumber + 1
  │           ├── PlaySound(winSfx)
  │           └── EventBus.RaiseLevelWon()
  │                 └── UIManager.OnLevelWin()
  │                       ├── HUD.hide, WinPanel.show
  │                       ├── ConfettiManager.PlayConfetti()
  │                       └── 3 saniye sonra → MainScene'e dön
  │
  └── RemainingMoves <= 0?
        └── LOSE:
              ├── IsGameOver = true
              ├── PlaySound(loseSfx)
              └── EventBus.RaiseLevelLost()
                    └── UIManager.OnLevelLose()
                          ├── HUD.hide, LosePanel.show
                          └── Butonlar: TryAgain → aynı sahne, MainMenu → MainScene

GameManager.OnTurnComplete()
  │
  └── IsGameOver?
        ├── BoardParent.hide
        ├── BoardSetup.HideBackground()
        └── LevelButton.show
```

---

## 6. Gravity + Refill Döngüsü

```
BoardResolver.ApplyGravityAndFillSequence()
  │
  ├── Phase 1: İlk Gravity
  │     └── GravitySystem.ApplyGravityAndAnimate()
  │           └── Her sütun: boşlukların üstündeki fallable item'ları aşağı düşür
  │                 ├── Stagger delay: bottomItem=0, each above += GravityStepDelay
  │                 ├── Fall duration: FallMoveDuration * sqrt(distance)
  │                 └── Landing bounce: bounceHeight + bounceDuration
  │     └── WaitForSeconds(maxAnimationTime + GravityStepDelay)
  │
  ├── Phase 2: Refill
  │     └── BoardFiller.FillEmptySpaces()
  │           └── Her sütundaki boş CanSpawnItem hücrelere:
  │                 ├── ItemFactory.CreateItem("rand", parent, cellSize) → yeni küp
  │                 ├── Spawn pozisyonu: board üstünden SpawnRowsAboveBoard kadar yukarı
  │                 └── Animasyon: SpawnStartScale → 1.0 scale
  │     └── RocketHintSystem.UpdateHints()
  │     └── WaitForSeconds(fillDelay)
  │
  ├── Phase 3: İkinci Gravity (refill sonrası)
  │     └── GravitySystem.ApplyGravityAndAnimate()
  │     └── PostFillGravityStepDelay
  │
  └── Phase 4: Final Hints
        └── RocketHintSystem.UpdateHints()
```
