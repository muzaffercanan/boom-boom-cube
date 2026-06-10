# Boom Boom Cube — Project Overview

> **Purpose**: Bu doküman, AI agentların projeyi hızlıca anlaması ve token tasarrufu yapması için hazırlanmıştır.

## Proje Kimliği

| Özellik | Değer |
|---|---|
| **Proje Adı** | Boom Boom Cube |
| **Tür** | Match-2 Puzzle Oyunu |
| **Engine** | Unity 6000.0.59f2 |
| **Dil** | C# |
| **Tween Kütüphanesi** | DOTween |
| **Test Framework** | Unity Test Framework (NUnit) |
| **Namespace Root** | `DreamGames` |

## Oyun Mekaniği Özeti

- Oyuncu, aynı renkteki **2+ bitişik küpü** tıklayarak patlatır.
- **4+ küp** eşleşmesi → Roket power-up oluşturur (Yatay veya Dikey).
- Roketler tıklanınca satır/sütun temizler. İki bitişik roket → **Combo** (3 satır + 3 sütun).
- Engeller: **Box** (blast ile hasar), **Vase** (düşer + blast ile hasar), **Stone** (sadece roket).
- Her level'da hedef engeller ve sınırlı hamle sayısı vardır.
- Küpler patladıktan sonra **yerçekimi** (gravity) ve **refill** sistemi devreye girer.
- Eğer hamle kalmadıysa board otomatik **shuffle** edilir.

## Teknik Mimari

```
Composition over Inheritance, Factory Pattern, Event-Driven Architecture
```

Proje **MonoBehaviour-light** bir yaklaşım kullanır: İş mantığı (business logic) büyük ölçüde **plain C# sınıflarında** (POCO) yaşar. Unity MonoBehaviour'lar sadece birer giriş noktası (entry point) ve coroutine runner olarak kullanılır.

### Katmanlar

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

## Namespace Haritası

| Namespace | Klasör | Sorumluluk |
|---|---|---|
| `DreamGames.Core` | `Scripts/Core/` | Enumlar, interface'ler, sabitler, event'ler, RNG |
| `DreamGames.Data` | `Scripts/Data/` | Level JSON parse/validasyon |
| `DreamGames.Board.Systems` | `Scripts/Board/Systems/` | Grid, Match, Gravity, Rocket, Goal, ItemFactory |
| `DreamGames.Board.Items` | `Scripts/Board/Items/` | Cube, Rocket, Box, Stone, Vase MonoBehaviour'ları |
| `DreamGames.Board.Visuals` | `Scripts/Board/Visuals/` | BoardVisualConfig, RocketProjectile, ShuffleVisualController |
| `DreamGames.Gameplay` | `Scripts/Managers/` | GameManager, TurnProcessor, UIManager, services |
| `DreamGames.UI` | `Scripts/UI/` | GoalItemView |

## Sahne Yapısı

| Sahne | Dosya | İçerik |
|---|---|---|
| **MainScene** | `Assets/Scenes/MainScene.unity` | Ana menü, level seçim butonları |
| **LevelScene** | `Assets/Scenes/LevelScene.unity` | Oyun board'u, HUD, GameManager + UIManager |

## Dependency Injection Yaklaşımı

Proje formal bir DI container kullanmaz. Bunun yerine:

1. `GameManager.EnsureServices()` → Service interface'lerinin default implementasyonlarını lazy-create eder.
2. `GameplaySystemFactory.Create()` → Tüm board ve gameplay sistemlerini composition root olarak oluşturur.
3. Interface'ler testlerde mock'lanabilir:
   - `IAudioService`, `IProgressService`, `IGameplayEventBus`, `ISceneLoadService`
   - `IBoardItemViewLifecycle`

## Önemli Kurallar (Agent'lar İçin)

1. **Formatlama**: Kodun orijinal formatını (satır sonları, boşluklar, girinti yapısı) KORU. Otomatik formatlama yapma.
2. **Namespace using'leri**: Her dosyada tüm proje namespace'leri import edilir (global pattern). Yeni dosya oluştururken aynı pattern'i kullan.
3. **Random**: `UnityEngine.Random` kullanılmaz. `GameRng.Shared` kullanılır (deterministic replay için).
4. **Event sistemi**: `GameEvents` statik sınıfı üzerinden event fırlatılır, `IGameplayEventBus` interface'i ile sarmalanır.
5. **Level dosyaları**: JSON formatında, `Assets/Levels/` altında, `level_XX.json` isim şablonuyla.
6. **PlayerPrefs key'leri**: `LastPlayedLevel` ve `SelectedLevelForGame`.
