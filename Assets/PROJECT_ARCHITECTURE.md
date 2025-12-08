# Ristorante Rumble - Proje Mimarisi

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
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐            │
│  │ GameManager  │  │ PhaseManager │  │ NetworkManager│           │
│  │              │  │              │  │              │            │
│  │ • Oyun       │  │ • Gündüz/    │  │ • NGO        │            │
│  │   durumu     │  │   Gece geçiş │  │ • Lobby      │            │
│  │ • Skorlama   │  │ • Zamanlayıcı│  │ • Sync       │            │
│  └──────────────┘  └──────────────┘  └──────────────┘            │
│                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐            │
│  │ TeamManager  │  │ ReputationMgr│  │ EconomyManager│           │
│  │              │  │              │  │              │            │
│  │ • Takım      │  │ • İtibar     │  │ • Para       │            │
│  │   ataması    │  │   hesaplama  │  │   yönetimi   │            │
│  │ • 5v5        │  │ • Kazanma    │  │ • Kasa       │            │
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
├── Player
│   ├── NetworkTransform
│   ├── Health (NetworkVariable)
│   ├── Inventory (NetworkVariable)
│   └── PlayerClass (NetworkVariable)
│
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

```
Scenes/
├── MainMenu/
│   ├── MainMenu.unity          # Ana menü
│   └── Lobby.unity             # Multiplayer lobi
│
├── Game/
│   ├── GameScene.unity         # Ana oyun sahnesi
│   │   ├── Restaurant_TeamA    # A takımı restoranı
│   │   ├── Restaurant_TeamB    # B takımı restoranı
│   │   ├── Shops               # Ortak dükklar
│   │   └── CombatArea          # Gece savaş alanı
│   │
│   └── (Additive scenes)
│       ├── DayPhase.unity      # Gündüz UI/sistemler
│       └── NightPhase.unity    # Gece UI/sistemler
```

---

## 9. Prefab Yapısı

```
Prefabs/
├── Player/
│   ├── Player.prefab           # Ana oyuncu prefab
│   ├── Chef.prefab             # Chef varyantı
│   ├── Runner.prefab           # Runner varyantı
│   └── Fighter.prefab          # Fighter varyantı
│
├── Restaurant/
│   ├── CookingStation.prefab   # Pişirme istasyonu
│   ├── ServingCounter.prefab   # Servis tezgahı
│   ├── Freezer.prefab          # Buzdolabı
│   ├── Safe.prefab             # Kasa
│   └── Chair.prefab            # Müşteri sandalyesi
│
├── Characters/
│   ├── Customer.prefab         # Normal müşteri
│   └── FakeCustomer.prefab     # Sahte müşteri
│
├── Items/
│   ├── Ingredients/            # Malzeme prefabları
│   ├── Dishes/                 # Yemek prefabları
│   └── Weapons/                # Silah prefabları
│
├── UI/
│   ├── OrderUI.prefab          # Sipariş göstergesi
│   ├── ReputationBar.prefab    # İtibar barı
│   └── PhaseTimer.prefab       # Faz zamanlayıcısı
│
└── Network/
    ├── DroppedMoney.prefab     # Düşürülen para
    └── Projectile.prefab       # Mermi/projektil
```

---

## 10. ScriptableObject Yapısı

```
ScriptableObjects/
├── Recipes/
│   ├── Recipe.cs               # Tarif base class
│   └── [TarifAdı].asset        # Her tarif için asset
│
├── Ingredients/
│   ├── Ingredient.cs           # Malzeme base class
│   └── [MalzemeAdı].asset      # Her malzeme için asset
│
├── Weapons/
│   ├── WeaponData.cs           # Silah base class
│   └── [SilahAdı].asset        # Her silah için asset
│
├── Upgrades/
│   ├── UpgradeData.cs          # Upgrade base class
│   ├── KitchenUpgrades/        # Mutfak geliştirmeleri
│   └── LoungeUpgrades/         # Lounge geliştirmeleri
│
└── GameSettings/
    ├── PhaseSettings.asset     # Faz süreleri
    ├── EconomySettings.asset   # Ekonomi değerleri
    └── ReputationSettings.asset# İtibar değerleri
```

---

## 11. Özellik Yol Haritası

### Faz 1: Temel Altyapı
- [ ] Network Manager kurulumu (NGO)
- [ ] Lobi sistemi
- [ ] Temel oyuncu hareketi
- [ ] Faz geçiş sistemi

### Faz 2: Gündüz Fazı
- [ ] Mutfak sistemi
- [ ] Sipariş/Müşteri sistemi
- [ ] Envanter sistemi
- [ ] Dükkan sistemi
- [ ] Upgrade sistemi

### Faz 3: Gece Fazı
- [ ] Combat sistemi
- [ ] Silah sistemi
- [ ] Soygun/Kasa sistemi
- [ ] Respawn/Spectator sistemi

### Faz 4: Gelişmiş Mekanikler
- [ ] Sahte müşteri sistemi
- [ ] Sınıf yetenekleri
- [ ] İtibar etkileri
- [ ] Kazanma koşulu

### Faz 5: Polish
- [ ] UI/UX
- [ ] Ses efektleri
- [ ] Görsel efektler
- [ ] Optimizasyon

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

*Bu döküman Ristorante Rumble oyun tasarım dökümanına (GDD) dayanmaktadır.*

