# Ristorante Rumble - Proje Mimarisi

> Son Güncelleme: 22 Şubat 2026

## 1. Oyun Genel Bakış

**Ristorante Rumble**, gündüz restoran yönetimi ve gece soygun/PvP savaşı içeren **5v5 rekabetçi multiplayer** bir oyundur.

### Temel Döngü
```
┌─────────────────────────────────────────────────────────┐
│                    OYUN DÖNGÜSÜ                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│   ┌─────────────┐              ┌─────────────┐          │
│   │  GÜNDÜZ     │              │   GECE      │          │
│   │  FAZI       │ ──────────►  │   FAZI      │          │
│   │             │              │             │          │
│   │ • Yemek     │              │ • PvP       │          │
│   │ • Alışveriş │              │ • Soygun    │          │
│   │ • Upgrade   │              │ • Hırsızlık │          │
│   └─────────────┘              └─────────────┘          │
│         ▲                            │                  │
│         └────────────────────────────┘                  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Kazanma Koşulu
- Başlangıç: Her takım **%50 itibar** ile başlar
- Hedef: İtibar oranını **%70-%30** seviyesine getirmek
- %70'e ulaşan takım için son zamanlayıcı başlar
- Kaybeden takım toparlamazsa oyun biter

---

## 2. Sistem Mimarisi

### 2.1 Core Systems (Çekirdek Sistemler)

```
┌──────────────────────────────────────────────────────────────────┐
│                       CORE SYSTEMS                               │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────┐  ┌────────────────┐  ┌──────────────┐          │
│  │ GameManager  │  │ SceneController│  │ NetworkManager│         │
│  │ ✅ implemente│  │ ✅ implemente  │  │ ✅ implemente│          │
│  │ • Oyun state │  │ • Sahne geçişi │  │ • NGO        │          │
│  │ • Faz timer  │  │ • DDOL         │  │ • Lobby      │          │
│  │ • PhaseManager│ │ • NGO scene mgr│  │ • Sync       │          │
│  │   merged     │  │                │  │              │          │
│  └──────────────┘  └────────────────┘  └──────────────┘          │
│                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────────┐          │
│  │ TeamManager  │  │ CameraManager│  │LightingManager │          │
│  │ ✅ implemente│  │ ✅ implemente│  │ ✅ implemente  │          │
│  │ • Takım      │  │ • Cinemachine│  │ • Day/night    │          │
│  │   ataması    │  │ • Priority   │  │   light lerp   │          │
│  │ • 5v5        │  │   swap       │  │                │          │
│  └──────────────┘  └──────────────┘  └────────────────┘          │
│                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐            │
│  │ GameEvents   │  │ ReputationMgr│  │ EconomyManager│           │
│  │ ✅ implemente│  │ ⬜ planlanıyor│  │ ⬜ planlanıyor│           │
│  │ • Static     │  │ • İtibar     │  │ • Para       │            │
│  │   event bus  │  │   hesaplama  │  │   yönetimi   │            │
│  │ • GameState  │  │ • Kazanma    │  │ • Kasa       │            │
│  └──────────────┘  └──────────────┘  └──────────────┘            │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### 2.2 Day Phase Systems (Gündüz Fazı Sistemleri)

```
┌──────────────────────────────────────────────────────────────────┐
│                    DAY PHASE SYSTEMS                             │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────┐    ┌─────────────────┐                      │
│  │ KitchenManager  │    │ CustomerManager │                      │
│  │                 │    │                 │                      │
│  │ • Pişirme       │    │ • Müşteri spawn │                      │
│  │ • Malzeme       │    │ • Sipariş       │                      │
│  │ • İstasyonlar   │    │ • Sabır süresi  │                      │
│  │ • Tarifler      │    │ • Sahte müşteri │                      │
│  └─────────────────┘    └─────────────────┘                      │
│                                                                  │
│  ┌─────────────────┐    ┌─────────────────┐                      │
│  │ ShopManager     │    │ UpgradeManager  │                      │
│  │                 │    │                 │                      │
│  │ • Malzeme dükkanı│   │ • Mutfak upgrade│                      │
│  │ • Silah dükkanı │    │ • Lounge upgrade│                      │
│  │ • Upgrade dükkanı│   │ • Sandalye ekleme│                     │
│  └─────────────────┘    └─────────────────┘                      │
│                                                                  │
│  ┌─────────────────┐                                             │
│  │ InventorySystem │                                             │
│  │                 │                                             │
│  │ • Buzdolabı     │                                             │
│  │ • Malzeme stok  │                                             │
│  │ • Silah envanter│                                             │
│  └─────────────────┘                                             │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### 2.3 Night Phase Systems (Gece Fazı Sistemleri)

```
┌──────────────────────────────────────────────────────────────────┐
│                    NIGHT PHASE SYSTEMS                           │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────┐    ┌─────────────────┐                      │
│  │ CombatManager   │    │ WeaponSystem    │                      │
│  │                 │    │                 │                      │
│  │ • Hasar hesap   │    │ • Silah türleri │                      │
│  │ • Ölüm/Respawn  │    │ • Varsayılan    │                      │
│  │ • Friendly fire │    │   tabanca       │                      │
│  │   (yok)         │    │ • Tek gecelik   │                      │
│  └─────────────────┘    └─────────────────┘                      │
│                                                                  │
│  ┌─────────────────┐    ┌─────────────────┐                      │
│  │ HeistManager    │    │ SafeSystem      │                      │
│  │                 │    │                 │                      │
│  │ • Para çalma    │    │ • Kasa içeriği  │                      │
│  │ • Para taşıma   │    │ • Çalınabilir   │                      │
│  │ • Para düşürme  │    │   miktar        │                      │
│  └─────────────────┘    └─────────────────┘                      │
│                                                                  │
│  ┌─────────────────┐                                             │
│  │ SpectatorSystem │                                             │
│  │                 │                                             │
│  │ • Ölüm sonrası  │                                             │
│  │   izleme        │                                             │
│  │ • Respawn timer │                                             │
│  └─────────────────┘                                             │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## 3. Oyuncu Sınıfları (Player Classes)

```
┌──────────────────────────────────────────────────────────────────┐
│                      PLAYER CLASSES                              │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │                        CHEF                                │  │
│  │  • Gündüz: Yemekleri daha hızlı pişirir                    │  │
│  │  • Gece: Standart yetenekler                               │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │                       RUNNER                               │  │
│  │  • Gündüz: Müşterilere yemekleri daha hızlı taşır          │  │
│  │  • Gece: Standart yetenekler                               │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │                      FIGHTER                               │  │
│  │  • Gündüz: Standart yetenekler                             │  │
│  │  • Gece: Özel silahları kullanabilir                       │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  NOT: Tüm sınıflar her görevi yapabilir, sınıf sadece bonus     │
│       sağlar.                                                    │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## 4. Ekonomi Sistemi

### 4.1 Para Kaynakları
| Kaynak | Açıklama |
|--------|----------|
| Müşteri siparişi | Başarılı teslimat = para + itibar |
| Çalınan para | Gece fazında düşman kasasından |
| Düşman ölümü | Direkt para yok, sadece itibar etkisi |

### 4.2 Harcama Alanları
| Alan | Açıklama |
|------|----------|
| Malzeme Dükkanı | Yemek yapmak için malzeme |
| Upgrade Dükkanı | Mutfak ve lounge geliştirmeleri |
| Silah Dükkanı | Gece savaşı için silahlar |
| Sahte Müşteri | Düşmana sabotaj |

### 4.3 Kasa Sistemi
```
┌─────────────────────────────────────────────────────────┐
│                    KASA SİSTEMİ                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  • Her takımın bir kasası var                           │
│  • Gündüz kazanılan para kasaya gider                   │
│  • Gece düşman kasasından çalınabilir                   │
│  • Çalınabilir miktar:                                  │
│    - Servis edilen yemek değerine bağlı                 │
│    - Kasadaki mevcut paraya bağlı                       │
│  • Kilit veya tuzak YOK                                 │
│  • Kamp yapma discourage edilir (ölüm = itibar kaybı)   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 5. İtibar (Reputation) Sistemi

### 5.1 İtibar Değişimleri
| Eylem | Etki |
|-------|------|
| Başarılı sipariş teslimi | + İtibar |
| Başarısız/gecikmeli sipariş | - İtibar |
| Gece fazında öldürme | + İtibar (küçük) |
| Gece fazında ölme | - İtibar (küçük) |
| Sahte müşteri (başarılı servis) | - İtibar (küçük) |
| Sahte müşteri (başarısız servis) | - İtibar (büyük) |

### 5.2 İtibar Etkileri
```
İTİBAR YÜKSEK (%60+)
    │
    ├─► Daha fazla müşteri gelir
    ├─► Daha fazla para kazanılır
    └─► Daha fazla zorluk (sipariş yoğunluğu)

İTİBAR DÜŞÜK (%40-)
    │
    ├─► Daha az müşteri
    ├─► Daha az gelir
    └─► Toparlanma şansı
```

---

## 6. Sahte Müşteri Mekaniği

```
┌─────────────────────────────────────────────────────────┐
│               SAHTE MÜŞTERİ AKIŞI                       │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  1. Takım A, dükkandan sahte müşteri satın alır         │
│                    │                                    │
│                    ▼                                    │
│  2. Rastgele gecikme sonrası düşman restoranında        │
│     sahte müşteri belirir                               │
│                    │                                    │
│                    ▼                                    │
│  3. Normal müşteri gibi sipariş verir                   │
│                    │                                    │
│           ┌───────┴───────┐                             │
│           ▼               ▼                             │
│     SERVİS BAŞARILI   SERVİS BAŞARISIZ                  │
│           │               │                             │
│           ▼               ▼                             │
│     Yemek beğenmeme   BÜYÜK itibar                      │
│     (KÜÇÜK itibar     kaybı                             │
│      kaybı)                                             │
│                                                         │
│  NOT: Satın alan takım için risk veya dezavantaj YOK    │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 7. Network Mimarisi (NGO)

### 7.1 Senkronizasyon Gereksinimleri

| Sistem | Sync Tipi | Öncelik |
|--------|-----------|---------|
| Oyuncu pozisyonu | Real-time | Yüksek |
| Sağlık/Hasar | Real-time | Yüksek |
| Para/İtibar | Server-authoritative | Yüksek |
| Faz geçişleri | Server-authoritative | Yüksek |
| Sipariş durumu | Server-authoritative | Orta |
| Envanter | Server-authoritative | Orta |
| Müşteri spawn | Server-authoritative | Orta |

### 7.2 Network Objeler
```
NetworkObjects:

── ✅ İMPLEMENTE ──────────────────────────────

├── Player (Player Animated.prefab)
│   ├── NetworkObject
│   ├── NetworkTransform (owner-authoritative)
│   ├── OwnerNetworkAnimator (client-auth anim sync)
│   └── PlayerMovement (NetworkBehaviour)
│
── ⬜ PLANLANYOR ───────────────────────────────

├── Restaurant
│   ├── Safe (NetworkVariable - money)
│   ├── Freezer (NetworkVariable - ingredients)
│   └── Upgrades (NetworkVariable - list)
│
├── Kitchen
│   ├── CookingStations (NetworkBehaviour)
│   └── Orders (NetworkList)
│
├── Customer
│   ├── Order (NetworkVariable)
│   ├── PatienceTimer (NetworkVariable)
│   └── IsFake (NetworkVariable)
│
└── DroppedMoney
    ├── Amount (NetworkVariable)
    └── Position (NetworkTransform)
```

---

## 8. Sahne Yapısı

> 2 sahne, additive yok. NGO SceneManager ile geçiş.

```
Scenes/
├── MainMenu.unity               # Menü + Lobi UI
│   ├── NetworkManager (DontDestroyOnLoad)
│   ├── LobbyManager (DontDestroyOnLoad)
│   ├── RelayManager (DontDestroyOnLoad)
│   ├── LoadingScreen Canvas (DontDestroyOnLoad)
│   ├── EventSystem
│   └── Canvas (MainMenu + Lobby UI panels)
│
└── Game.unity                   # Ana oyun sahnesi (NetworkManager.SceneManager ile yüklenir)
    ├── GameManager
    ├── TeamManager
    ├── PlayerSpawnManager
    ├── CameraManager
    │   ├── DayCamera (CinemachineCamera — top down)
    │   └── NightCamera (CinemachineCamera — 3rd person, başlangıçta deaktif)
    ├── LightingManager
    │   ├── DayLighting (warm, bright)
    │   └── NightLighting (cool, dark)
    ├── Environment
    │   ├── Restaurant_TeamA (planlanıyor)
    │   ├── Restaurant_TeamB (planlanıyor)
    │   ├── Shops (planlanıyor)
    │   └── NightArena (planlanıyor)
    ├── SpawnPoints
    │   ├── TeamA_Day_Spawns[]
    │   ├── TeamB_Day_Spawns[]
    │   ├── TeamA_Night_Spawns[]
    │   └── TeamB_Night_Spawns[]
    └── Canvas (Game HUD — planlanıyor)
```

---

## 9. Prefab Yapısı

### ✅ Mevcut Prefab'lar
```
Prefabs/
├── Player/
│   ├── Player.prefab              # Orijinal player (eski)
│   ├── Player Animated.prefab     # Aktif player — NetworkObject + NetworkTransform + OwnerNetworkAnimator
│   └── Armature.prefab            # Model/armature
│
└── UI/
    ├── LobbyItem.prefab           # Lobi listesi item
    └── PlayerItem.prefab          # Lobi oda player item
```

### ⬜ Planlanıyor
```
Prefabs/
├── Restaurant/
│   ├── CookingStation.prefab      # Pişirme istasyonu
│   ├── ServingCounter.prefab      # Servis tezgahı
│   ├── Freezer.prefab             # Buzdolabı
│   ├── Safe.prefab                # Kasa
│   └── Chair.prefab               # Müşteri sandalyesi
│
├── Characters/
│   ├── Customer.prefab            # Normal müşteri (NavMesh)
│   └── FakeCustomer.prefab        # Sahte müşteri
│
├── Combat/
│   ├── Projectile.prefab          # NetworkObject — mermi
│   └── DroppedMoney.prefab        # NetworkObject — düşürülen para
│
└── UI/
    ├── OrderBubble.prefab         # World-space sipariş baloncuğu
    └── DamageNumber.prefab        # World-space hasar UI
```

---

## 10. ScriptableObject Yapısı

### ✅ Mevcut
```
Assets/Data/
└── Settings/
    ├── PhaseSettingsSO.cs         # SO tanımı — faz süreleri, min player
    └── PhaseSettings.asset        # Instance
```

### ⬜ Planlanıyor
```
Assets/Data/
├── Recipes/
│   ├── RecipeSO.cs                # Tarif base class
│   └── [TarifAdı].asset
│
├── Ingredients/
│   ├── IngredientSO.cs            # Malzeme base class
│   └── [MalzemeAdı].asset
│
├── Weapons/
│   ├── WeaponSO.cs                # Silah base class
│   └── [SilahAdı].asset
│
├── Restaurant/
│   ├── RestaurantLayoutSO.cs      # Başlangıç layout tanımı
│   ├── PlaceableItemSO.cs         # Yerleştirilebilir obje base class
│   ├── KitchenItems/              # Fırın, ızgara, kesme tahtası...
│   ├── LoungeItems/               # Masa varyantları
│   └── StorageItems/              # Buzdolabı varyantları
│
├── Upgrades/
│   ├── UpgradeSO.cs               # Upgrade base class
│   ├── Permanent/                 # Kalıcı upgrade'ler
│   └── RoundBased/                # Round bazlı upgrade'ler
│
└── Settings/
    ├── EconomySettingsSO.cs       # Fiyatlar, kazanç çarpanları
    ├── ReputationSettingsSO.cs    # İtibar formülleri
    └── CombatSettingsSO.cs        # Hasar çarpanları, respawn süresi
```

---

## 11. Özellik Yol Haritası

> TECH_ARCHITECTURE.md §9 ile hizalı 10 fazlı plan. Detaylı açıklamalar orada.
> Arşiv: [PHASE3_GAMEFLOW_ROADMAP.md](./Documentation/Archive/PHASE3_GAMEFLOW_ROADMAP.md) — Faz 2 detaylı geliştirme geçmişi

### Faz 0: Network & Lobby Altyapısı ✅ TAMAMLANDI
- [x] NetworkManager + UnityTransport
- [x] RelayManager (allocation/join)
- [x] LobbyManager (create/join/list/leave/poll)
- [x] Anonim authentication
- [x] ParRelSync test ortamı

### Faz 1: UI Sistemi ✅ TAMAMLANDI
- [x] MainMenuUI, LobbyBrowserUI, CreateLobbyUI, LobbyRoomUI
- [x] LobbyItemUI, NetworkManagerUI (debug)

### Faz 1.5: Player Movement ✅ TAMAMLANDI
- [x] PlayerMovement.cs (Rigidbody, Input System, sprint, jump)
- [x] OwnerNetworkAnimator (client-auth animasyon)
- [x] Player Animated prefab
- [ ] Movement → faz bazlı ayrıştırma (DayMovement + NightMovement) — Faz 5'e ertelendi

### Faz 2: Game Flow ✅ TAMAMLANDI
- [x] GameEvents.cs (static event bus + GameState enum)
- [x] SceneController (sahne geçişi, DDOL)
- [x] Game.unity sahnesi
- [x] PlayerSpawnManager (takım bazlı spawn)
- [x] GameManager (state machine + phase timer, PhaseManager merged)
- [x] TeamManager (5v5 atama, NetworkList)
- [x] Ready system (Lobby API player data ile)
- [x] LoadingScreenUI (DDOL)
- [x] CameraManager (Cinemachine 3.x priority swap)
- [x] LightingManager (day/night lerp)
- [x] PhaseSettingsSO (data-driven faz ayarları)
- [ ] ConnectionManager (state machine) — Faz 8'e ertelendi
- [ ] Input Action Map swap (Day ↔ Night) — Faz 5'e ertelendi

### Faz 3: Day Phase — Core Loop ⬜
- [ ] SO veri yapıları (RecipeSO, IngredientSO, PlaceableItemSO)
- [ ] RestaurantManager + SlotSystem
- [ ] KitchenManager + CookingStation
- [ ] CustomerManager + Customer NPC (NavMesh)
- [ ] OrderSystem (sipariş, timer)
- [ ] PlayerInteraction (al, koy, servis et)
- [ ] Temel yemek pişirme akışı
- [ ] World-space UI (sipariş baloncuğu, sabır barı)

### Faz 4: Economy & Shops & Restaurant Upgrade ⬜
- [ ] EconomyManager (takım para yönetimi)
- [ ] ShopManager (malzeme, silah, upgrade, ekipman)
- [ ] PlayerInventory (NetworkVariable)
- [ ] UpgradeSO + UpgradeManager (kademeli reset)
- [ ] Restoran yerleştirme modu (slot highlight, server validation)
- [ ] SafeSystem (kasa)
- [ ] Buzdolabı sistemi

### Faz 5: Night Phase — Combat ⬜
- [ ] WeaponSO veri yapıları
- [ ] WeaponSystem (equip, ateş, reload)
- [ ] CombatManager (server-side hit detection, hasar)
- [ ] HealthSystem (HP, ölüm, respawn timer)
- [ ] NightMovement (3rd person shooter)
- [ ] NightCameraController
- [ ] CrosshairUI, HealthBarUI

### Faz 6: Night Phase — Heist/Soygun ⬜
- [ ] HeistManager (kasa etkileşim, para çalma)
- [ ] DroppedMoney (ölünce para düşürme)
- [ ] SpectatorSystem
- [ ] Respawn sistemi

### Faz 7: Advanced Mechanics ⬜
- [ ] FakeCustomerSystem
- [ ] Player sınıfları (Chef, Runner, Fighter)
- [ ] ReputationManager (itibar, kazanma koşulu)
- [ ] Game Over ekranı

### Faz 8: Connection Reliability & Polish ⬜
- [ ] ConnectionManager hata yönetimi
- [ ] Graceful disconnect
- [ ] Reconnection desteği

### Faz 9: Audio, VFX, Juice ⬜
- [ ] Ses, müzik, VFX, UI animasyonları

### Faz 10: Playtest & Balance ⬜
- [ ] SO değerleriyle balance tuning
- [ ] Playtest feedback döngüsü

---

## 12. Notlar ve Kısıtlamalar

### Oyun Kuralları
- **AI oyuncu YOK** - Takımlar mevcut oyuncu sayısıyla oynar
- **Friendly fire YOK** - Takım arkadaşına hasar verilemez
- **Silahlar tek gecelik** - Her gece yeniden alınmalı
- **Kasa koruması YOK** - Kilit veya tuzak yok

### Teknik Kısıtlamalar
- Maksimum 10 oyuncu (5v5)
- Server-authoritative ekonomi
- Client-side prediction hareket için
- Server reconciliation combat için

---

*Bu döküman Ristorante Rumble oyun tasarım dökümanıdır (GDD). Teknik detaylar için bkz. TECH_ARCHITECTURE.md.*

