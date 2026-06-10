# Kodlama Kuralları & Patterns

> **Purpose**: Projeye yeni kod eklerken veya mevcut kodu değiştirirken uyulması gereken kurallar.  
> AI agentlar bu dosyayı okuyarak tutarlı ve projeyle uyumlu kod üretebilir.

---

## 1. Genel Kurallar

### Formatlama
- **Kodun orijinal formatını KORU.** Otomatik formatlama (auto-formatting) veya satır kırma (line wrapping) yapma.
- PR diff'ini minimumda tut: sadece mantıksal değişiklik yaptığın satırlara dokun.
- Girinti (indent): **4 boşluk** (space, tab değil).
- Satır sonu: **CRLF** (`\r\n`).

### Using Pattern
Her `.cs` dosyası şu namespace import bloğunu içerir (projenin her yerinde aynı):

```csharp
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;
```

Yeni dosya oluştururken bu pattern'i kullan, ihtiyaç duyulmasa bile. Unity ve System using'leri bunların üstüne eklenir.

### Namespace Seçimi
- Yeni dosyanın namespace'i, yerleştirildiği klasöre göre belirlenir:
  - `Scripts/Core/` → `DreamGames.Core`
  - `Scripts/Data/` → `DreamGames.Data`
  - `Scripts/Board/Systems/` → `DreamGames.Board.Systems`
  - `Scripts/Board/Items/` → `DreamGames.Board.Items`
  - `Scripts/Board/Visuals/` → `DreamGames.Board.Visuals`
  - `Scripts/Managers/` → `DreamGames.Gameplay`
  - `Scripts/UI/` → `DreamGames.UI`
  - `Scripts/Editor/` → namespace olmayabilir (Editor sınıfları)

---

## 2. Mimari Patterns

### Composition over Inheritance
- Yeni iş mantığı sınıfları **plain C#** olarak yaz (MonoBehaviour değil).
- MonoBehaviour sadece Unity lifecycle gereken yerlerde (input, coroutine, inspector referansları) kullan.

### Factory Pattern
- Yeni item/prefab oluştururken `ItemFactory` kullan.
- Doğrudan `Instantiate()` çağırma; `ItemFactory.CreateItem()` veya `ItemFactory.CreateVisual()` kullan.

### Event-Driven Communication
- Sistemler arası iletişim `GameEvents` statik event'leri üzerinden yapılır.
- Yeni bir event eklerken:
  1. `GameEvents.cs`'ye statik event ve Raise metodu ekle.
  2. `IGameplayEventBus` interface'ine event ve raise metodu ekle.
  3. `StaticGameplayEventBus`'a implementasyonu ekle.
  4. `UIManager` veya ilgili listener'a subscribe/unsubscribe ekle.

### Service Abstraction
- Dış bağımlılıklar (Audio, Progress, Scene) interface üzerinden erişilir:
  - `IAudioService`, `IProgressService`, `ISceneLoadService`, `IGameplayEventBus`
- Yeni dış bağımlılık eklerken aynı pattern'i izle.

---

## 3. Önemli Teknik Kurallar

### Random Sayı Üretimi
```csharp
// YANLIŞ - KULLANMA
UnityEngine.Random.Range(0, 4);

// DOĞRU
GameRng.Shared.Range(0, 4);
```
Deterministic replay desteği için `GameRng.Shared` kullan. Test'lerde seed set ederek tekrarlanabilir sonuçlar al.

### Yeni Board Item Ekleme Adımları
1. `ItemId` enum'ına yeni entry ekle (`GameEnums.cs`)
2. `ItemIds.cs`'ye string sabiti ve dönüşüm ekle
3. MonoBehaviour sınıfını yaz (`AbstractBoardItem`'dan türet)
4. Gerekli interface'leri implement et (`IMatchable`, `IFallable`, `IDamageable`)
5. Prefab oluştur ve `ItemFactory` ScriptableObject'e mapping ekle
6. Eğer hedef item ise, `GoalTracker.IsGoalItem()` ve `GetGoalIdFromItem()`'a ekle
7. `DamageResolver`'da damage logic ekle (gerekiyorsa)
8. Test yaz (`BoardSystemsEditModeTests.cs`)

### Yeni Level Ekleme
1. `Assets/Levels/level_XX.json` oluştur:
```json
{
    "level_number": 11,
    "grid_width": 9,
    "grid_height": 9,
    "move_count": 20,
    "grid": ["r", "g", "b", ...]
}
```
2. `Assets/Resources/Levels/` altına kopyasını koy (TextAsset olarak yüklenir).
3. Grid kodları: `r`, `g`, `b`, `y`, `bo`, `s`, `v`, `rand`
4. Grid sırası: sol-alttan başlayarak satır satır (row-major, y=0 en alt).

### Yeni Level Format (cells)
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
        ...
    ]
}
```

### Coroutine Kullanımı
- İş mantığı sınıfları `IEnumerator` döner.
- Coroutine başlatmak için `MonoBehaviour.StartCoroutine()` gerekir.
- `GameManager` veya `_coroutineRunner` üzerinden başlat.

### DOTween
- Tüm tween'ler `transform.DOKill()` ile iptal edilir (OnDestroy'da).
- `.SetTarget(transform)` kullanarak tween'i objeye bağla.
- `Sequence` kullanarak sıralı animasyonlar oluştur.

### Animation Timing
- `BoardAnimationConfig` üzerinden tüm animasyon süreleri ayarlanabilir.
- `GameDebug.SpeedMultiplier` ile runtime'da hız ayarı (debug).
- Fall duration: `FallMoveDuration * sqrt(cellDistance)`.
- Stagger delay: `GravityStepDelay * cascadeIndex`.

### PlayerPrefs Keys
```csharp
"LastPlayedLevel"       // int: son tamamlanan level + 1
"SelectedLevelForGame"  // int: seçilen level numarası
```

---

## 4. Test Yazma Kuralları

### EditMode Test
- Dosya: `Assets/Tests/EditMode/BoardSystemsEditModeTests.cs`
- Assembly: `DreamGames.EditModeTests`
- NUnit `[Test]` attribute kullan.
- `GridSystem`, `MatchSystem`, `GravitySystem`, vb. sistemleri doğrudan new'le (MonoBehaviour gerektirmez).
- `GameRng.SetSharedSeed()` ile deterministic test yaz.

### PlayMode Test
- Dosya: `Assets/Tests/PlayMode/GameManagerPlayModeSmokeTests.cs`
- Assembly: `DreamGames.PlayModeTests`
- `[UnityTest]` attribute ve `IEnumerator` kullan.
- Sahne yüklemesi gerekiyorsa `SceneManager.LoadScene()` kullan.

### Genel Test İpuçları
- `ItemFactory` testlerde null olabilir; mock kullan veya `CreateItem` çağırma.
- `GridSystem.Initialize()` sonrası `SetItem()` ile test board'u kur.
- `BoardResolver.ResolveImmediate()` testlerde animasyon beklemeden board'u stabilize eder.

---

## 5. Assembly Definition Yapısı

| Assembly | Dosya | Referanslar |
|---|---|---|
| `DreamGames.Runtime` | `Scripts/DreamGames.Runtime.asmdef` | — |
| `DreamGames.Editor` | `Scripts/Editor/DreamGames.Editor.asmdef` | Runtime |
| `DreamGames.EditModeTests` | `Tests/EditMode/DreamGames.EditModeTests.asmdef` | Runtime |
| `DreamGames.PlayModeTests` | `Tests/PlayMode/DreamGames.PlayModeTests.asmdef` | Runtime |

---

## 6. Sık Yapılan Hatalar (Agent'lar İçin Dikkat)

1. **`UnityEngine.Random` kullanma** → `GameRng.Shared` kullan.
2. **Namespace import bloğunu eksik bırakma** → Tüm 7 namespace'i ekle.
3. **Board koordinat sistemi**: (0,0) = sol-alt, X = sütun (sol→sağ), Y = satır (alt→üst).
4. **Non-fallable item'ler** (StoneItem) gravity'de "zemin" gibi davranır. `IFallable` implement etmezler.
5. **`IsProcessingTurn` kontrolü**: Yeni input sistemleri eklerken re-entrant koruması yap.
6. **`_onDamageRequest` callback'i roket chain reaction'ı tetikleyebilir** (roketin vurduğu hücrede başka roket varsa).
7. **Goal sadece engeller**: Box, Stone, Vase. Küpler goal değildir.
