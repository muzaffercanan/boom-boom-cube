# 📚 Dokümantasyon İndeksi

> **Bu dosyayı ilk oku.** Hangi durumda hangi dokümanı okumanız gerektiğini gösterir.

## Hızlı Referans

| Dosya | Ne Zaman Oku | Boyut |
|---|---|---|
| [PROJECT_OVERVIEW.md](./PROJECT_OVERVIEW.md) | ✅ **Her zaman ilk oku** — Projenin ne olduğu, teknoloji stack'i, temel kurallar | Kısa |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Belirli bir sınıfın API'sini veya sorumluluğunu öğrenmen gerektiğinde | Uzun |
| [FILE_MAP.md](./FILE_MAP.md) | Bir dosyayı ararken veya hangi dosyanın ne yaptığını bulmak istediğinde | Orta |
| [CODING_GUIDELINES.md](./CODING_GUIDELINES.md) | Yeni kod yazarken veya mevcut kodu değiştirirken | Orta |
| [DATA_FLOWS.md](./DATA_FLOWS.md) | Bir akışı anlamak istediğinde (level yükleme, tıklama, hasar, win/lose) | Uzun |
| [DEPENDENCY_GRAPH.md](./DEPENDENCY_GRAPH.md) | Bir değişikliğin etkisini analiz ederken, circular dependency anlamak için | Orta |

## Görev Bazlı Rehber

### 🐛 Bug Fix yapacaksam
1. `PROJECT_OVERVIEW.md` → genel bağlam
2. `FILE_MAP.md` → ilgili dosyaları bul
3. `DATA_FLOWS.md` → bug'ın olduğu akışı takip et
4. `DEPENDENCY_GRAPH.md` → fix'in yan etkilerini kontrol et

### ✨ Yeni Özellik ekleyeceksem
1. `PROJECT_OVERVIEW.md` → genel bağlam ve kurallar
2. `ARCHITECTURE.md` → ilgili sistemlerin API'leri
3. `CODING_GUIDELINES.md` → kodlama kuralları ve patterns
4. `DEPENDENCY_GRAPH.md` → nereye entegre edeceğini anla

### 🧪 Test yazacaksam
1. `CODING_GUIDELINES.md` → test yazma kuralları (bölüm 4)
2. `ARCHITECTURE.md` → test edeceğin sistemin API'si
3. `DATA_FLOWS.md` → test edeceğin akışın adımları

### 🔍 Kod anlamak istiyorsam
1. `PROJECT_OVERVIEW.md` → 2 dakikada genel bakış
2. `FILE_MAP.md` → dosya bazlı hızlı açıklamalar
3. `ARCHITECTURE.md` → detaylı sınıf ve API referansı

### 🎮 Yeni Item/Engel ekleyeceğsem
1. `CODING_GUIDELINES.md` → "Yeni Board Item Ekleme Adımları" bölümü
2. `ARCHITECTURE.md` → Board Items bölümü, kalıtım hiyerarşisi
3. `DATA_FLOWS.md` → Damage Chain akışı

### 📊 Yeni Level ekleyeceğsem
1. `CODING_GUIDELINES.md` → "Yeni Level Ekleme" bölümü
2. `DATA_FLOWS.md` → Level Yükleme Akışı

---

## Token Tasarrufu İpuçları

1. **Tüm dosyaları birden okuma.** Görevine göre yukarıdaki "Görev Bazlı Rehber"yi kullan.
2. **`PROJECT_OVERVIEW.md` çoğu zaman yeterli.** Sadece derinlemesine bilgi gerektiğinde diğerlerine geç.
3. **`FILE_MAP.md`** ile dosyayı bulduktan sonra sadece o dosyanın kaynak kodunu oku.
4. **`ARCHITECTURE.md`** çok uzun; sadece ilgili başlığı oku (dosya içi arama yap).
