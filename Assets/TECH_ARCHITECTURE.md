# Ristorante Rumble — Teknoloji & Mimari Kılavuzu

> Son Güncelleme: 21 Şubat 2026
> Versiyon: 1.0

---

## 1. Teknoloji Stack'i

### 1.1 Kesinleşen Teknolojiler

| Katman | Teknoloji | Versiyon | Rol |
|--------|-----------|----------|-----|
| Engine | Unity 6 LTS | 6000.3 | Oyun motoru |
| Rendering | URP | 17.3.0 | Render pipeline |
| Network | Netcode for GameObjects (NGO) | 2.9.2 | Multiplayer framework |
| Transport | Unity Transport + Relay | Latest | NAT traversal, P2P bağlantı |
| Matchmaking | Unity Lobby | Latest | Lobi oluşturma/katılma |
| Auth | Unity Authentication | Latest | Anonim kimlik doğrulama |
| Input | Input System | 1.18.0 | Yeni input framework |
| Camera | Cinemachine | 3.1.4 | Çift kamera sistemi |
| UI | uGUI + TextMeshPro | 2.0.0 | Kullanıcı arayüzü |
| Testing | ParrelSync | Latest | Lokal multi-instance test |

### 1.2 Eklenecek Teknolojiler

| Teknoloji | Ne Zaman | Neden |
|-----------|----------|-------|
| NavMesh / AI Navigation | Faz 3 (Day Phase) | Müşteri NPC pathfinding |
| DOTween veya LeanTween | Faz 3+ | UI animasyon, VFX juice |
| NaughtyAttributes | Hemen | Inspector UX (Button, ShowIf, ReadOnly) |

### 1.3 Kullanılmayacaklar (ve Neden)

| Teknoloji | Neden Kullanılmıyor |
|-----------|---------------------|
| UI Toolkit | Mevcut uGUI çalışıyor, in-world UI desteği zayıf, migration overhead |
| DOTS / ECS | Proje ölçeği gerektirmiyor, NGO zaten GameObject-based |
| Dedicated Server | Host-mode + Relay yeterli, sunucu maliyeti yok |
| VContainer (DI) | Küçük ekip, singleton pattern yeterli |
| Addressables | Asset boyutu küçük, standart Resources/direct ref yeterli |
| FishNet / Mirror | NGO zaten entegre, Unity ekosisteminden çıkmaya gerek yok |
| FMOD / Wwise | Unity Audio şimdilik yeterli |

---

## 2. Mimari Kararlar

### 2.1 Network Topolojisi

```
KARAR: Host-Mode + Unity Relay (kalıcı)

┌─────────────────────────────────────────────┐
│              UNITY RELAY SERVER              │
│          (NAT traversal, routing)            │
└──────────────────┬──────────────────────────┘
                   │
       ┌───────────┼───────────┐
       │           │           │
  ┌────▼───┐  ┌───▼────┐  ┌───▼────┐
  │  HOST  │  │ CLIENT │  │ CLIENT │  ... (max 10)
  │ Server │  │   2    │  │   3    │
  │+Client │  │        │  │        │
  └────────┘  └────────┘  └────────┘
```

**Kurallar:**
- Tüm authoritative logic `IsServer` kontrolü arkasında
- Client sadece input gönderir (RPC), server state değiştirir
- Ekonomi, itibar, hasar — hep server-authoritative
- Hareket — client-authoritative (NetworkTransform, owner authority)
- Host advantage minimal: bu casual/arkadaş grubu oyunu, ranked değil

### 2.2 Kamera Sistemi (Çift Mod)

```
KARAR: Gündüz top-down fixed, Gece 3rd person shooter

┌──────────────────────────────────────────────────────┐
│                    GÜNDÜZ FAZI                         │
│                                                       │
│   Kamera: Top-down fixed (Overcooked style)          │
│   Açı: ~60° yukarıdan                                │
│   Kontrol: WASD 4-yön hareket, mouse = interact      │
│   Cinemachine: CinemachineCamera + follow group       │
│                                                       │
│   ┌─────────────────────────────┐                    │
│   │        RESTORAN             │                    │
│   │   [P1] [P2]    [Mutfak]    │  ← sabit kamera    │
│   │   [P3] [P4]    [Tezgah]    │    tüm alanı       │
│   │        [P5]    [Müşteriler] │    kapsıyor         │
│   └─────────────────────────────┘                    │
└──────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────┐
│                     GECE FAZI                         │
│                                                       │
│   Kamera: 3rd person follow (shooter)                │
│   Açı: Oyuncunun arkası, omuz üstü                   │
│   Kontrol: WASD + mouse look, sol tık = ateş         │
│   Cinemachine: CinemachineCamera + 3rdPersonFollow   │
│                                                       │
│          ┌───┐                                       │
│          │ P │ ← oyuncu                              │
│          └─┬─┘                                       │
│            │                                         │
│         [Kamera]  ← arkadan takip                    │
│                                                       │
│   Harita: Açık alan, restoranlar arası savaş alanı   │
└──────────────────────────────────────────────────────┘
```

**Implementasyon:**
- `CameraManager.cs` — Faz geçişinde kamera modunu değiştirir
- İki ayrı Cinemachine Virtual Camera (gündüz + gece)
- Priority swap ile smooth geçiş
- Gece fazında cursor lock + mouse look aktif
- Gündüz fazında cursor free + point-and-click interact

### 2.3 Movement Sistemi (Çift Mod)

```
KARAR: Faz bazlı hareket kontrolü

GÜNDÜZ (Top-Down):
  - WASD = 4/8 yönlü hareket (mevcut gibi)
  - Mouse = interaksiyon cursor
  - Space = etkileşim (yemek al, koy, servis et)
  - Shift = sprint (opsiyonel)
  - Rigidbody, client-authoritative

GECE (3rd Person Shooter):
  - WASD = karakter hareketi (kameraya göre yön)
  - Mouse = bakış yönü (kamera orbit)
  - Sol tık = ateş
  - Sağ tık = nişan alma / zoom
  - E = etkileşim
  - R = reload
  - Space = zıpla
  - Shift = sprint
  - Rigidbody, client-authoritative hareket
  - Server-authoritative hit detection + hasar
```

**Implementasyon:**
- `PlayerMovement.cs` → faz bazlı input mapping değişimi
- Veya iki ayrı script: `DayMovement.cs` + `NightMovement.cs` (tercih edilen — separation of concerns)
- Input Action Map swap: "DayPhase" map ↔ "NightPhase" map

### 2.4 Combat Sistemi (Shooter-Lite)

```
KARAR: Tabanca + yakın dövüş karışık shooter-lite

SILAH SİSTEMİ:
┌────────────────────────────────────────────┐
│  Varsayılan: Tabanca (sonsuz mermi, düşük hasar)
│  Satın alınabilir:
│  ├── Pompalı tüfek (yüksek hasar, yavaş)
│  ├── SMG (hızlı, düşük hasar, spread)
│  ├── Tava / Bıçak (melee, yüksek hasar, kısa menzil)
│  └── [genişletilebilir — WeaponSO sistemi]
│
│  Kurallar:
│  • Silahlar tek gecelik (her gece sıfırlanır)
│  • Varsayılan tabanca her zaman var
│  • Friendly fire YOK
│  • Ölüm = respawn timer + küçük itibar kaybı
└────────────────────────────────────────────┘

NETWORK AKIŞI:
  Client: Ateş input → ShootRpc(direction, weaponId) → Server
  Server: Raycast/projectile hit detection → hasar hesapla → HP güncelle
  Server: HitEffectRpc() → tüm client'lara VFX/SFX
```

**Implementasyon:**
- `WeaponSO` — ScriptableObject per silah (hasar, menzil, fire rate, spread, mermi sayısı)
- `WeaponSystem.cs` — Silah equip/swap/ateş logic (NetworkBehaviour)
- `CombatManager.cs` — Server-side hit detection, hasar hesaplama
- `HealthSystem.cs` — NetworkVariable<int> HP, OnDeath event
- Projectile-based (fiziksel mermi) veya hitscan (raycast) — silah tipine göre

### 2.5 Data Architecture (ScriptableObject-Driven)

```
Assets/Data/
├── Recipes/
│   ├── RecipeSO.cs                    # Base class
│   ├── Recipe_Pasta.asset             # Pasta tarifi
│   ├── Recipe_Pizza.asset             # Pizza tarifi
│   └── ...
│
├── Ingredients/
│   ├── IngredientSO.cs
│   ├── Ingredient_Tomato.asset
│   ├── Ingredient_Cheese.asset
│   └── ...
│
├── Weapons/
│   ├── WeaponSO.cs
│   ├── Weapon_Pistol.asset            # Varsayılan
│   ├── Weapon_Shotgun.asset
│   ├── Weapon_SMG.asset
│   ├── Weapon_Pan.asset               # Melee
│   └── ...
│
├── Upgrades/
│   ├── UpgradeSO.cs
│   ├── Upgrade_BetterOven.asset
│   ├── Upgrade_ExtraChair.asset
│   └── ...
│
└── Settings/
    ├── PhaseSettingsSO.cs             # Faz süreleri, geçiş kuralları
    ├── EconomySettingsSO.cs           # Fiyatlar, kazanç çarpanları
    ├── ReputationSettingsSO.cs        # İtibar formülleri
    └── CombatSettingsSO.cs            # Hasar çarpanları, respawn süresi
```

**Network'te SO Kullanımı:**
- SO'lar network'te GÖNDERILMEZ
- Network'te sadece ID/index gönderilir (int veya FixedString)
- Her client'ta aynı SO referansları mevcut (build'e dahil)
- Örnek: `ShootRpc(weaponIndex=2)` → server `weaponList[2]` ile hasar hesaplar

### 2.6 State Management

```
CONNECTION LIFECYCLE (ConnectionManager — State Machine):

  Offline → Authenticating → InMenu → InLobby → Connecting → Loading → InGame → Disconnecting
     ▲                                                                              │
     └──────────────────────────────────────────────────────────────────────────────┘

GAME STATE (GameManager — NetworkVariable):

  WaitingForPlayers → Starting → DayPhase ←→ NightPhase → GameOver
                                    │              │
                                    └──────────────┘
                                    (round döngüsü)
```

**Singleton'lar (DontDestroyOnLoad):**
- `NetworkManager` (Unity built-in)
- `LobbyManager`
- `RelayManager`
- `ConnectionManager` (yeni — state machine)
- `LoadingScreenUI`

**Sahne-bağlı Manager'lar (Game sahnesi):**
- `GameManager` (NetworkBehaviour)
- `TeamManager` (NetworkBehaviour)
- `PlayerSpawnManager` (NetworkBehaviour)
- `CombatManager` (NetworkBehaviour) — gece fazı
- `KitchenManager` (NetworkBehaviour) — gündüz fazı
- `CustomerManager` (NetworkBehaviour) — gündüz fazı
- `EconomyManager` (NetworkBehaviour) — ekonomi
- `ReputationManager` (NetworkBehaviour) — itibar
- `RestaurantManager` (NetworkBehaviour) — restoran yerleşim + upgrade

---

## 3. Restoran Sistemi

### 3.1 Restoran Yapısı

Her takımın bir restoranı var. Restoran sabit bir alan içinde **slot-based yerleşim** sistemi kullanır.

```
┌─────────────────────────────────────────────────────────┐
│                   RESTORAN LAYOUT                        │
├─────────────────────────────────────────────────────────┤
│                                                          │
│   ┌──────────── MUTFAK ALANI ──────────────┐            │
│   │                                         │            │
│   │  [Slot A1]  [Slot A2]  [Slot A3]       │            │
│   │   Fırın      Tezgah     (boş)          │            │
│   │                                         │            │
│   │  [Slot B1]  [Slot B2]                   │            │
│   │  Buzdolabı   Kesme Tahtası             │            │
│   │                                         │            │
│   └─────────────────────────────────────────┘            │
│                                                          │
│   ┌──────────── SERVİS ALANI ──────────────┐            │
│   │                                         │            │
│   │  [Servis Tezgahı] ← yemek buraya konur │            │
│   │                                         │            │
│   └─────────────────────────────────────────┘            │
│                                                          │
│   ┌──────────── LOUNGE ALANI ──────────────┐            │
│   │                                         │            │
│   │  [Masa+Sandalye 1]  [Masa+Sandalye 2] │            │
│   │  [Masa+Sandalye 3]  [Slot: boş]       │            │
│   │                                         │            │
│   └─────────────────────────────────────────┘            │
│                                                          │
│   ┌──────────── ARKA ODA ──────────────────┐            │
│   │  [Kasa]  ← soygun hedefi              │            │
│   └─────────────────────────────────────────┘            │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### 3.2 Slot Sistemi

Restoran belirli sayıda **slot** içerir. Oyuncular gündüz fazında dükkandan satın aldıkları objeleri boş slotlara yerleştirir.

```
SLOT TİPLERİ:
├── KitchenSlot        → Pişirme ekipmanı yerleştirilebilir
│   ├── Fırın (başlangıç)
│   ├── Izgara (upgrade)
│   ├── Fritöz (upgrade)
│   └── Kesme Tahtası (başlangıç)
│
├── LoungeSlot         → Masa + sandalye yerleştirilebilir
│   ├── Basit Masa (başlangıç, 2 müşteri)
│   ├── Büyük Masa (upgrade, 4 müşteri)
│   └── VIP Masa (upgrade, 2 müşteri, yüksek bahşiş)
│
├── StorageSlot        → Depolama ekipmanı
│   ├── Buzdolabı (başlangıç, 6 slot)
│   └── Büyük Buzdolabı (upgrade, 12 slot)
│
└── FixedSlot          → Değiştirilemez
    ├── Servis Tezgahı (sabit)
    └── Kasa (sabit)
```

**Yerleştirme Kuralları:**
- Sadece gündüz fazında yerleştirme yapılabilir
- Slot tipi uyumlu olmalı (mutfak slotuna masa konamaaz)
- Yerleştirme server-authoritative (client istek gönderir, server onaylar)
- Yerleştirme geri alınabilir (obje kaldırılıp dükana satılabilir, %50 geri ödeme)

### 3.3 Upgrade Sistemi (Kademeli Reset)

```
UPGRADE KATEGORİLERİ:

┌─────────────────────────────────────────────────────────┐
│              KALICI (maç boyunca)                        │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  Yapısal Upgrade'ler — satın alındığında kalıcı:        │
│  ├── Ekstra mutfak slotu açma                           │
│  ├── Ekstra lounge slotu açma (daha fazla müşteri)      │
│  ├── Büyük buzdolabı (depolama kapasitesi)              │
│  ├── Daha iyi fırın/ızgara (pişirme hızı +%20)         │
│  └── VIP masa (yüksek bahşişli müşteri çekme)           │
│                                                          │
├─────────────────────────────────────────────────────────┤
│              ROUND BAZLI (her round sıfırlanır)          │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  Taktiksel Alımlar — her round yeniden alınmalı:        │
│  ├── Silahlar (tüm silahlar gece sonunda sıfırlanır)    │
│  ├── Sahte müşteri (tek kullanımlık sabotaj)            │
│  └── Özel buff'lar (hız boost, ekstra HP — tek gece)    │
│                                                          │
├─────────────────────────────────────────────────────────┤
│              TÜKETİME DAYALI (bitene kadar kalıcı)       │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  Malzemeler — depoda kalır, kullanıldıkça azalır:       │
│  ├── Dükkandan satın alınır → buzdolabına gider          │
│  ├── Yemek yapınca tüketilir (stoktan düşer)            │
│  ├── Round sonunda sıfırlanMAZ, kalan stok korunur      │
│  ├── İstediğin zaman dükkanadan tekrar alınabilir        │
│  └── Buzdolabı kapasitesi sınırlı (upgrade ile artar)   │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

**Upgrade Tier Sistemi:**
```
Tier 1 (Başlangıç)     → Ücretsiz, default ekipman
Tier 2 (Erken Oyun)    → 100-300₺, temel iyileştirmeler
Tier 3 (Orta Oyun)     → 500-1000₺, önemli avantajlar
Tier 4 (Geç Oyun)      → 1500+₺, güçlü ama pahalı
```

### 3.4 Restoran Network Mimarisi

```csharp
// Her restoran bir NetworkObject
// RestaurantManager tüm restoranları yönetir

Restaurant (NetworkObject)
├── NetworkVariable<int> TeamId
├── NetworkVariable<int> Money           // Takım kasası
├── NetworkList<SlotData> KitchenSlots   // Mutfak slot durumları
├── NetworkList<SlotData> LoungeSlots    // Lounge slot durumları
├── NetworkList<IngredientStack> Fridge  // Buzdolabı içeriği
└── NetworkList<UpgradeId> Upgrades     // Aktif upgrade listesi

SlotData (INetworkSerializable):
├── int SlotIndex
├── int PlacedItemId        // -1 = boş, SO index
├── int ItemTier
└── bool IsUnlocked         // Slot açık mı

UpgradeId (INetworkSerializable):
├── int CategoryIndex       // Hangi kategori (kitchen, lounge, storage)
├── int UpgradeIndex        // SO index
└── bool IsPermanent        // Kalıcı mı, round bazlı mı
```

### 3.5 Yerleştirme Akışı (Network)

```
CLIENT (Owner):                          SERVER:

1. Dükkandan "Izgara" satın al
   → PurchaseItemRpc(itemId) ──────────► 2. Para kontrolü
                                            Envantere ekle
                                            ◄── UpdateInventoryRpc()

3. Mutfak slotuna sürükle
   → PlaceItemRpc(slotIdx, itemId) ───► 4. Slot boş mu? Tip uyumlu mu?
                                            Slot güncelle (NetworkList)
                                            ◄── SlotData değişikliği
                                               tüm client'lara sync

5. Tüm client'larda obje
   slotta görünür
```

### 3.6 İlgili ScriptableObject'ler

```
Assets/Data/
├── Restaurant/
│   ├── RestaurantLayoutSO.cs          # Başlangıç layout tanımı
│   │   ├── initialKitchenSlots[]      # Kaç slot, hangileri açık
│   │   ├── initialLoungeSlots[]       # Başlangıç masa sayısı
│   │   └── fixedItems[]               # Servis tezgahı, kasa pozisyonları
│   │
│   ├── PlaceableItemSO.cs            # Yerleştirilebilir obje base class
│   │   ├── itemName
│   │   ├── icon (Sprite)
│   │   ├── prefab (GameObject)
│   │   ├── slotType (enum: Kitchen, Lounge, Storage)
│   │   ├── tier (int)
│   │   ├── purchaseCost (int)
│   │   ├── sellbackRatio (float, 0.5 = %50)
│   │   └── bonusEffects[]             # Pişirme hızı, müşteri kapasitesi vs.
│   │
│   ├── KitchenItems/
│   │   ├── Item_BasicOven.asset
│   │   ├── Item_Grill.asset
│   │   ├── Item_Fryer.asset
│   │   ├── Item_CuttingBoard.asset
│   │   └── Item_PremiumOven.asset
│   │
│   ├── LoungeItems/
│   │   ├── Item_BasicTable.asset      # 2 müşteri
│   │   ├── Item_LargeTable.asset      # 4 müşteri
│   │   └── Item_VIPTable.asset        # 2 müşteri, yüksek bahşiş
│   │
│   └── StorageItems/
│       ├── Item_BasicFridge.asset     # 6 slot
│       └── Item_LargeFridge.asset     # 12 slot
│
├── Upgrades/
│   ├── UpgradeSO.cs                   # Upgrade base class
│   │   ├── upgradeName
│   │   ├── description
│   │   ├── icon
│   │   ├── cost (int)
│   │   ├── tier (int)
│   │   ├── isPermanent (bool)         # Kalıcı mı round-bazlı mı
│   │   ├── requiredUpgrade (UpgradeSO) # Prerequisite
│   │   └── effects[]                  # Stat modifiers
│   │
│   ├── Permanent/
│   │   ├── Upgrade_ExtraKitchenSlot.asset
│   │   ├── Upgrade_ExtraLoungeSlot.asset
│   │   ├── Upgrade_FasterOven.asset
│   │   └── Upgrade_LargeFridge.asset
│   │
│   └── RoundBased/
│       ├── Upgrade_SpeedBoost.asset
│       └── Upgrade_ExtraHP.asset
```

### 3.7 Restoran Görsel Sistemi

```
YERLEŞTIRME MOD'U (gündüz fazında dükkandan sonra):

1. Oyuncu item satın alır → envantere gider
2. Envaterden item seçer → "yerleştirme modu" aktif
3. Uygun slotlar highlight olur (yeşil outline)
4. Uyumsuz slotlar kırmızı / deaktif
5. Slot'a tıkla → item yerleşir (server onayı sonrası)
6. Yerleşmiş item'a tıkla → kaldır / sat seçeneği

GÖRSEL FEEDBACK:
├── Boş slot: yarı-saydam ghost outline
├── Uyumlu slot (hover): yeşil glow
├── Uyumsuz slot: kırmızı X
├── Yerleşmiş item: tam opak, interact edilebilir
└── Upgrade edilmiş item: altın/parlak VFX border
```

---

## 4. Klasör Yapısı (Hedef)

> Not: `Data/Restaurant/` ScriptableObject'leri bölüm 3.6'da detaylı açıklanmıştır.

```
Assets/
├── Data/                              # ScriptableObject tanımları + asset'leri
│   ├── Recipes/
│   ├── Ingredients/
│   ├── Weapons/
│   ├── Upgrades/
│   ├── Restaurant/                    # Restoran ekipmanları (bkz. §3.6)
│   │   ├── KitchenItems/
│   │   ├── LoungeItems/
│   │   └── StorageItems/
│   └── Settings/
│
├── Prefabs/
│   ├── Player/
│   │   ├── Player.prefab             # Ana oyuncu (NetworkObject + NetworkTransform)
│   │   └── [sınıf varyantları]
│   ├── Restaurant/
│   │   ├── CookingStation.prefab
│   │   ├── ServingCounter.prefab
│   │   ├── Freezer.prefab
│   │   ├── Safe.prefab
│   │   └── Chair.prefab
│   ├── Characters/
│   │   ├── Customer.prefab           # NPC müşteri (NavMesh)
│   │   └── FakeCustomer.prefab
│   ├── Combat/
│   │   ├── Projectile.prefab         # NetworkObject
│   │   └── DroppedMoney.prefab       # NetworkObject
│   └── UI/
│       ├── LobbyItem.prefab
│       ├── PlayerItem.prefab
│       ├── OrderBubble.prefab        # World-space sipariş UI
│       └── DamageNumber.prefab       # World-space hasar UI
│
├── Scenes/
│   ├── MainMenu.unity                # Menü + Lobi UI
│   └── Game.unity                    # Ana oyun sahnesi (tek sahne)
│
├── Scripts/
│   ├── Core/                          # Sahne-bağımsız, DontDestroyOnLoad
│   │   ├── ConnectionManager.cs       # Bağlantı state machine
│   │   ├── SceneController.cs         # Sahne geçişleri
│   │   └── GameEvents.cs             # Static event channel'lar
│   │
│   ├── Network/
│   │   ├── RelayManager.cs
│   │   └── NetworkUtils.cs           # Helper'lar
│   │
│   ├── Lobby/
│   │   ├── LobbyManager.cs
│   │   ├── PlayerReadyManager.cs
│   │   └── LobbyData.cs              # Veri yapıları
│   │
│   ├── Game/                          # Oyun state yönetimi
│   │   ├── GameManager.cs            # Ana state machine
│   │   ├── TeamManager.cs
│   │   ├── PhaseManager.cs           # Gündüz/gece geçişleri
│   │   ├── EconomyManager.cs
│   │   └── ReputationManager.cs
│   │
│   ├── Player/
│   │   ├── PlayerController.cs        # Ana entry point, faz yönetimi
│   │   ├── DayMovement.cs            # Top-down hareket
│   │   ├── NightMovement.cs          # 3rd person shooter hareket
│   │   ├── PlayerInteraction.cs      # Gündüz etkileşim (al, koy, servis et)
│   │   ├── PlayerInventory.cs        # Envanter (NetworkVariable)
│   │   ├── HealthSystem.cs           # HP (NetworkVariable)
│   │   ├── OwnerNetworkAnimator.cs
│   │   └── PlayerSpawnManager.cs
│   │
│   ├── Camera/
│   │   ├── CameraManager.cs          # Faz bazlı kamera swap
│   │   ├── DayCameraController.cs    # Top-down kamera ayarları
│   │   └── NightCameraController.cs  # 3rd person kamera ayarları
│   │
│   ├── Restaurant/                    # Restoran yerleşim + upgrade
│   │   ├── RestaurantManager.cs      # Slot yönetimi, upgrade uygulaması
│   │   ├── SlotSystem.cs             # Slot logic, yerleştirme/kaldırma
│   │   └── UpgradeManager.cs         # Upgrade satın alma, kademeli reset
│   │
│   ├── DayPhase/                      # Gündüz fazı sistemleri
│   │   ├── KitchenManager.cs
│   │   ├── CookingStation.cs
│   │   ├── CustomerManager.cs
│   │   ├── Customer.cs               # NPC AI + NavMesh
│   │   ├── OrderSystem.cs
│   │   ├── FakeCustomerSystem.cs
│   │   └── ShopManager.cs
│   │
│   ├── NightPhase/                    # Gece fazı sistemleri
│   │   ├── CombatManager.cs
│   │   ├── WeaponSystem.cs
│   │   ├── Projectile.cs
│   │   ├── HeistManager.cs
│   │   ├── SafeSystem.cs
│   │   └── SpectatorSystem.cs
│   │
│   ├── UI/
│   │   ├── Menu/
│   │   │   ├── MainMenuUI.cs
│   │   │   ├── LobbyBrowserUI.cs
│   │   │   ├── CreateLobbyUI.cs
│   │   │   └── LobbyRoomUI.cs
│   │   ├── HUD/
│   │   │   ├── GameHUD.cs            # Ana oyun HUD
│   │   │   ├── PhaseTimerUI.cs
│   │   │   ├── ReputationBarUI.cs
│   │   │   ├── MoneyUI.cs
│   │   │   ├── HealthBarUI.cs        # Gece fazı
│   │   │   └── CrosshairUI.cs        # Gece fazı
│   │   ├── World/
│   │   │   ├── OrderBubbleUI.cs      # Müşteri sipariş baloncuğu
│   │   │   ├── PlayerNameUI.cs       # Oyuncu isim etiketi
│   │   │   └── DamageNumberUI.cs
│   │   └── Common/
│   │       ├── LoadingScreenUI.cs
│   │       ├── LobbyItemUI.cs
│   │       └── NetworkManagerUI.cs    # Debug UI
│   │
│   └── Testing/
│       └── RelayTest.cs
│
├── Art/                               # Görsel asset'ler
│   ├── Models/
│   ├── Textures/
│   ├── Materials/
│   ├── Animations/
│   └── VFX/
│
├── Audio/
│   ├── Music/
│   ├── SFX/
│   └── UI/
│
├── Input/
│   └── InputSystem_Actions.inputactions  # Tüm input map'ler
│
└── Packages/
    └── ParrelSync-master/
```

---

## 5. Network Authority Matrisi

Her sistemin network authority modeli:

| Sistem | Authority | Sync Yöntemi | Açıklama |
|--------|-----------|-------------|----------|
| Player Movement | **Owner (Client)** | NetworkTransform | Responsive hareket, host-mode'da kabul edilebilir |
| Player Animation | **Owner (Client)** | OwnerNetworkAnimator | Client kontrollü smooth animasyon |
| Health/HP | **Server** | NetworkVariable<int> | Hile koruması |
| Ekonomi (para) | **Server** | NetworkVariable<int> | Kesinlikle server-authoritative |
| İtibar | **Server** | NetworkVariable<float> | Kesinlikle server-authoritative |
| Silah ateşleme | **Client input → Server validation** | RPC | Client ateş eder, server hit detect yapar |
| Hit Detection | **Server** | Server-side raycast/collision | Anti-cheat |
| Hasar hesaplama | **Server** | RPC + NetworkVariable | Server hesaplar, HP günceller |
| Müşteri spawn | **Server** | Server spawn | NPC yönetimi |
| Sipariş oluşturma | **Server** | NetworkVariable/NetworkList | Server belirler |
| Faz geçişleri | **Server** | NetworkVariable<GameState> | Senkronize timer |
| Envanter | **Server** | NetworkVariable | Server doğrular |
| Kasa (safe) | **Server** | NetworkVariable<int> | Soygun miktarını server hesaplar |
| Spawn/Respawn | **Server** | Server spawn | Pozisyon ve zamanlama |
| Takım atama | **Server** | NetworkList<ulong> | Server belirler |

---

## 6. Input Action Maps

```
Input Actions Asset:
├── DayPhase (Action Map)
│   ├── Move          → WASD / Left Stick       (Vector2)
│   ├── Interact      → E / South Button        (Button)
│   ├── PickUp        → Mouse Left / West Button (Button)
│   ├── Drop          → Q / East Button          (Button)
│   ├── Sprint        → Shift / Left Trigger     (Button)
│   └── Pause         → Escape / Start           (Button)
│
├── NightPhase (Action Map)
│   ├── Move          → WASD / Left Stick       (Vector2)
│   ├── Look          → Mouse Delta / Right Stick (Vector2)
│   ├── Shoot         → Mouse Left / Right Trigger (Button)
│   ├── Aim           → Mouse Right / Left Trigger (Button)
│   ├── Reload        → R / West Button          (Button)
│   ├── Jump          → Space / South Button     (Button)
│   ├── Sprint        → Shift / Left Stick Press  (Button)
│   ├── WeaponSwitch  → Scroll / D-Pad           (Value)
│   └── Pause         → Escape / Start           (Button)
│
└── UI (Action Map)
    ├── Navigate      → Arrow Keys / D-Pad      (Vector2)
    ├── Submit        → Enter / South Button     (Button)
    ├── Cancel        → Escape / East Button     (Button)
    └── Point         → Mouse Position           (Vector2)
```

**Faz geçişinde:**
```csharp
// PhaseManager faz değiştiğinde:
InputSystem.actions.FindActionMap("DayPhase").Disable();
InputSystem.actions.FindActionMap("NightPhase").Enable();
Cursor.lockState = CursorLockMode.Locked; // Gece: mouse look
```

---

## 7. Sahne Mimarisi

```
KARAR: 2 sahne (basit)

MainMenu.unity
├── NetworkManager (DontDestroyOnLoad)
├── LobbyManager (DontDestroyOnLoad)
├── RelayManager (DontDestroyOnLoad)
├── ConnectionManager (DontDestroyOnLoad)
├── LoadingScreen Canvas (DontDestroyOnLoad)
├── EventSystem
└── Canvas (MainMenu + Lobby UI panels)

Game.unity (NetworkManager.SceneManager ile yüklenir)
├── GameManager
├── PhaseManager
├── TeamManager
├── PlayerSpawnManager
├── EconomyManager
├── ReputationManager
├── CameraManager
│   ├── DayCamera (CinemachineCamera — top down)
│   └── NightCamera (CinemachineCamera — 3rd person, başlangıçta deaktif)
├── Environment
│   ├── Restaurant_TeamA
│   │   ├── Kitchen (CookingStations, Freezer)
│   │   ├── Lounge (Chairs, ServingCounter)
│   │   └── Safe
│   ├── Restaurant_TeamB
│   │   └── (aynı yapı)
│   ├── Shops (ortada, her iki takımın erişebildiği)
│   └── NightArena (savaş alanı, restoranlar arası)
├── SpawnPoints
│   ├── TeamA_Day_Spawns[]
│   ├── TeamB_Day_Spawns[]
│   ├── TeamA_Night_Spawns[]
│   └── TeamB_Night_Spawns[]
├── Lighting
│   ├── DayLighting (warm, bright)
│   └── NightLighting (cool, dark, spotlights)
├── Canvas (Game HUD)
└── CustomerManager (gündüz fazında aktif)
```

---

## 8. Faz Geçiş Akışı

```
ROUND DÖNGÜSÜ:

[Oyun Başlangıcı]
       │
       ▼
┌──────────────────┐
│ WaitingForPlayers │ ← min 2 oyuncu bekleniyor
│                  │
│ Oyuncular spawn  │
│ Takım ataması    │
└────────┬─────────┘
         │ (yeterli oyuncu + countdown)
         ▼
┌──────────────────┐
│    GÜNDÜZ FAZI   │ ← ~120 saniye (ayarlanabilir)
│                  │
│ • Top-down kamera │
│ • Yemek pişir     │
│ • Müşteri servis  │
│ • Alışveriş       │
│ • Para kazan      │
│                   │
│ [TIMER: 2:00 ▼]  │
└────────┬─────────┘
         │ (timer bitti)
         ▼
┌──────────────────┐
│   GEÇİŞ ANI     │ ← ~5 saniye
│                  │
│ • Kamera geçişi   │  (top-down → 3rd person)
│ • Input map swap  │  (DayPhase → NightPhase)
│ • Lighting geçişi │  (bright → dark)
│ • Cursor lock     │
│ • Silah equip     │
│ • Müşteriler çıkar│
│ • UI swap         │  (restoran HUD → combat HUD)
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│     GECE FAZI    │ ← ~60 saniye (ayarlanabilir)
│                  │
│ • 3rd person cam  │
│ • PvP savaş       │
│ • Düşman kasasını  │
│   soy             │
│ • Kendi kasanı     │
│   koru            │
│                   │
│ [TIMER: 1:00 ▼]  │
└────────┬─────────┘
         │ (timer bitti veya kazanma koşulu)
         ▼
┌──────────────────┐
│   ROUND SONU     │ ← ~5 saniye
│                  │
│ • Skor tablosu    │
│ • İtibar güncel.  │
│ • Silahlar sıfır. │
│ • HP full reset   │
│ • Kazanma kontrol │
│                   │
│  Kazanan var mı?  │
│  ├─ Hayır → ──────┘ (yeni round → Gündüz Fazı)
│  └─ Evet → GameOver
└──────────────────┘
```

---

## 9. Uzun Dönem Geliştirme Yol Haritası

### Faz 0: Network & Lobby Altyapısı ✅ TAMAMLANDI
- [x] NetworkManager + UnityTransport
- [x] RelayManager (allocation/join)
- [x] LobbyManager (create/join/list/leave/poll)
- [x] Anonim authentication
- [x] ParrelSync test ortamı

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
- [x] GameManager (state: Waiting → Starting → Day → Transition → Night → loop, PhaseManager merged)
- [x] TeamManager (5v5 atama, NetworkList)
- [x] Ready system (Lobby API player data ile, network sync)
- [x] LoadingScreenUI (DDOL)
- [x] CameraManager (Cinemachine 3.x priority swap)
- [x] LightingManager (day/night lerp)
- [x] PhaseSettingsSO (data-driven faz ayarları)
- [ ] ConnectionManager (state machine) — Faz 8'e ertelendi
- [ ] Input Action Map swap (Day ↔ Night) — Faz 5'e ertelendi

### Faz 3: Day Phase — Core Loop
- [ ] ScriptableObject veri yapıları (RecipeSO, IngredientSO, PlaceableItemSO)
- [ ] RestaurantManager + SlotSystem (slot-based yerleşim, bkz. §3)
- [ ] KitchenManager + CookingStation
- [ ] CustomerManager + Customer NPC (NavMesh)
- [ ] OrderSystem (sipariş oluşturma, timer)
- [ ] PlayerInteraction (al, koy, servis et)
- [ ] Temel yemek pişirme akışı (malzeme → istasyon → yemek → servis)
- [ ] World-space UI (sipariş baloncuğu, sabır barı)

### Faz 4: Economy & Shops & Restaurant Upgrade
- [ ] EconomyManager (takım para yönetimi)
- [ ] ShopManager (malzeme, silah, upgrade, restoran ekipmanı)
- [ ] PlayerInventory (NetworkVariable)
- [ ] UpgradeSO + UpgradeManager (kalıcı + round bazlı + tüketime dayalı, bkz. §3.3)
- [ ] Restoran yerleştirme modu (slot highlight, server validation, bkz. §3.5)
- [ ] SafeSystem (kasa)
- [ ] Buzdolabı sistemi (malzeme depolama, kapasite, tüketim)
- [ ] Para UI (HUD)

### Faz 5: Night Phase — Combat
- [ ] WeaponSO veri yapıları
- [ ] WeaponSystem (equip, ateş, reload)
- [ ] Projectile.cs (NetworkObject, hitscan + projectile)
- [ ] CombatManager (server-side hit detection, hasar)
- [ ] HealthSystem (HP, ölüm, respawn timer)
- [ ] NightMovement (3rd person shooter kontrolleri)
- [ ] NightCameraController (3rd person follow + mouse look)
- [ ] CrosshairUI, HealthBarUI
- [ ] Damage number VFX

### Faz 6: Night Phase — Heist/Soygun
- [ ] HeistManager (kasa etkileşim, para çalma)
- [ ] Para taşıma mekaniği (çalan oyuncu yavaşlar?)
- [ ] DroppedMoney prefab (ölünce para düşürme)
- [ ] SpectatorSystem (ölüm sonrası izleme)
- [ ] Respawn sistemi

### Faz 7: Advanced Mechanics
- [ ] FakeCustomerSystem (sabotaj satın alma, düşman restoranına gönderme)
- [ ] Player sınıfları (Chef, Runner, Fighter) — SO-driven bonus'lar
- [ ] ReputationManager (itibar formülleri, kazanma koşulu)
- [ ] İtibar etkileri (müşteri sıklığı, fiyat çarpanı)
- [ ] Kazanma koşulu kontrolü (%70-%30 itibar)
- [ ] Game Over ekranı + skor tablosu

### Faz 8: Connection Reliability & Polish
- [ ] ConnectionManager hata yönetimi (timeout, retry)
- [ ] Graceful disconnect (oyuncu düşerse)
- [ ] Host migration (opsiyonel — NGO sınırlı destek)
- [ ] Reconnection desteği
- [ ] Kullanıcı dostu hata mesajları

### Faz 9: Audio, VFX, Juice
- [ ] Ses: UI tıklama, yemek pişirme, silah sesleri, ambiyans
- [ ] Müzik: Menü, gündüz, gece (dinamik geçiş)
- [ ] VFX: Ateş, patlama, yemek efektleri, faz geçişi
- [ ] UI animasyonları (DOTween)
- [ ] Screen shake, hit feedback
- [ ] Particle systems

### Faz 10: Playtest & Balance
- [ ] SO değerleriyle balance tuning (Inspector'dan)
- [ ] Faz süreleri (gündüz/gece oranı)
- [ ] Ekonomi dengesi (kazanç/harcama)
- [ ] Silah dengesi (hasar/fire rate)
- [ ] İtibar formülleri
- [ ] Playtest feedback döngüsü

---

## 10. Kodlama Kuralları

### Naming Conventions
- **Sınıflar:** PascalCase (`GameManager`, `CookingStation`)
- **Public alanlar:** camelCase (`moveSpeed`, `maxHealth`)
- **Private alanlar:** camelCase (`currentHealth`, `isGrounded`)
- **NetworkVariable:** PascalCase (`Health`, `CurrentState`)
- **RPC:** PascalCase + `Rpc` suffix (`ShootRpc`, `TakeDamageRpc`)
- **Events:** On + PastTense (`OnDamaged`, `OnPhaseChanged`)
- **SO asset'ler:** Prefix_Name (`Recipe_Pasta`, `Weapon_Pistol`)

### Network Kuralları
1. Tüm state-değiştiren logic `IsServer` kontrolü arkasında
2. Client sadece RPC ile input gönderir, direkt state değiştirmez (hareket hariç)
3. NetworkVariable'larda `OnValueChanged` ile UI güncelle
4. RPC method'larda `Rpc` suffix zorunlu (yeni unified `[Rpc]` attribute)
5. `OnNetworkSpawn` içinde initialize, `OnNetworkDespawn` içinde cleanup
6. Rate limiting: Her frame RPC çağrısı yapma, throttle kullan

### Genel Kurallar
1. Her script tek sorumluluk (Single Responsibility)
2. ScriptableObject ile data-driven design
3. Event-driven communication (tight coupling'den kaçın)
4. Magic number kullanma, SerializeField ile Inspector'dan ayarla
5. Debug.Log'larda `[ClassName]` prefix kullan

---

## 11. Risk ve Dikkat Noktaları

| Risk | Etki | Çözüm |
|------|------|-------|
| Çift kamera sistemi karmaşıklığı | Yüksek | Erken prototiple, Cinemachine priority swap |
| Gece fazı shooter feel | Yüksek | 3rd person controller erken prototiple |
| 10 oyuncu + çok sayıda NetworkObject | Orta | Object pooling, interest management |
| Müşteri NPC pathfinding | Orta | Basit NavMesh, waypoint sistemi |
| Faz geçişi smoothness | Orta | Transition state, coroutine ile staged geçiş |
| Balance (ekonomi + combat) | Yüksek | SO-driven değerler, kolay tuning, playtest |
| Host-mode advantage | Düşük | Casual oyun, kabul edilebilir |
| Scope creep | Yüksek | Her fazı MVP olarak bitir, sonra polish |

---

---

## 12. İlgili Dokümanlar

| Doküman | Konum | İçerik |
|---------|-------|--------|
| **PROJECT_ARCHITECTURE.md** | `Assets/` | Oyun tasarım detayları (GDD): ekonomi formülleri, itibar sistemi, sahte müşteri akışı, kazanma koşulu, sınıf bonusları |
| **MULTIPLAYER_ROADMAP.md** | `Assets/Documentation/Archive/` | Arşiv — Faz 0-2 geliştirme geçmişi, tamamlanan adımlar |
| **PHASE3_GAMEFLOW_ROADMAP.md** | `Assets/Documentation/Archive/` | Arşiv — Faz 2 detaylı implementasyon planı (code snippets) |
| **NETCODE_ARCHITECTURE.md** | `Assets/Documentation/Archive/` | Arşiv — NGO API cookbook (NetworkVariable, RPC, spawn örnekleri) |

**Not:** Aktif geliştirme için sadece `TECH_ARCHITECTURE.md` + `PROJECT_ARCHITECTURE.md` yeterli. Arşiv dokümanları referans amaçlıdır.

---

## 13. Mevcut Codebase Durumu

### Aktif Scriptler

| Dosya | Konum | Durum |
|-------|-------|-------|
| `GameEvents.cs` | `Scripts/Core/` | ✅ Tam — static event bus + GameState enum |
| `SceneController.cs` | `Scripts/Core/` | ✅ Tam — DDOL, NGO scene management |
| `CameraManager.cs` | `Scripts/Camera/` | ✅ Tam — Cinemachine 3.x priority swap |
| `GameManager.cs` | `Scripts/Game/` | ✅ Tam — state machine + phase timer |
| `TeamManager.cs` | `Scripts/Game/` | ✅ Tam — NetworkList team assignment |
| `LightingManager.cs` | `Scripts/Game/` | ✅ Tam — day/night light lerp |
| `LobbyManager.cs` | `Scripts/Lobby/` | ✅ Tam — lobby + relay + ready + scene transition |
| `RelayManager.cs` | `Scripts/Network/` | ✅ Tam — allocation, join, cleanup |
| `PlayerMovement.cs` | `Scripts/Player/` | ✅ Temel — Rigidbody hareket + camera target |
| `PlayerSpawnManager.cs` | `Scripts/Player/` | ✅ Tam — team-based custom spawn |
| `OwnerNetworkAnimator.cs` | `Scripts/Player/` | ✅ Tam — client-auth animasyon sync |
| `MainMenuUI.cs` | `Scripts/UI/` | ✅ Tam — panel yönetimi |
| `LobbyBrowserUI.cs` | `Scripts/UI/` | ✅ Tam — lobi listesi, refresh, join |
| `CreateLobbyUI.cs` | `Scripts/UI/` | ✅ Tam — isim, slider, create |
| `LobbyRoomUI.cs` | `Scripts/UI/` | ✅ Tam — network ready sync via Lobby API |
| `LobbyItemUI.cs` | `Scripts/UI/` | ✅ Tam — prefab component |
| `LoadingScreenUI.cs` | `Scripts/UI/` | ✅ Tam — DDOL loading panel |
| `NetworkManagerUI.cs` | `Scripts/UI/` | ✅ Debug — Host/Client/Server butonları |
| `RelayTest.cs` | `Scripts/Testing/` | ✅ Test — LobbyManager entegrasyon testi |

### Data Assets

| Dosya | Konum | Durum |
|-------|-------|-------|
| `PhaseSettingsSO.cs` | `Data/Settings/` | ✅ SO — faz süreleri, min player |

### Silinen Dosyalar
- `TestLobby.cs` — deprecated, LobbyManager tarafından tam olarak replace edilmişti
- `PlayerNetwork.cs` — PlayerMovement.cs ile replace edildi
- `red.mat` — test materyali

### Sahneler

| Sahne | Durum |
|-------|-------|
| `MainMenu.unity` | ✅ Aktif — menü + lobi UI + DDOL managers |
| `Game.unity` | ✅ Aktif — oyun sahnesi (GameManager, TeamManager, SpawnManager, Camera, Lighting) |
| `Movement.unity` | ⚠️ Test — player movement testi, silinebilir |
| `SampleScene.unity` | ⚠️ Eski — kullanılmıyor, silinebilir |
| `TEST.unity` | ⚠️ Eski — kullanılmıyor, silinebilir |

### Prefab'lar

| Prefab | Durum |
|--------|-------|
| `Player/Player.prefab` | ⚠️ Eski — orijinal player |
| `Player/Player Animated.prefab` | ✅ Aktif — animasyonlu player (NetworkObject) |
| `Player/Armature.prefab` | ✅ Aktif — model/armature |
| `UI/LobbyItem.prefab` | ✅ Aktif |
| `UI/PlayerItem.prefab` | ✅ Aktif |

---

*Bu döküman projenin teknik omurgasıdır. Geliştirme sürecinde güncellenir.*