# Ristorante Rumble — Gelistirme Yol Haritasi

> Tarih: 16 Nisan 2026 (PO/PM Review: 16 Nisan 2026)
> Ekip: Alperen (Developer + 3D Artist + UI/UX Designer), Tuna (Developer)

---

## Urun Vizyonu ve MVP Tanimi

### Oyun Tanimı (Elevator Pitch)
"Gunduz restoran islet, gece rakipleri soy — 5v5 multiplayer cooking-meets-shooter."

### MVP — Minimum Oynanabilir Urun (Sprint 0-4 Sonrasi)
Asagidaki ozellikler tamamlandiginda oyun **ilk kez tam bir dongu** olarak oynanabilir:

1. **Lobi → Oyuna gir** — Calisiyor ✅
2. **Gunduz: Yemek pisir → Servis et → Para kazan** — %80 ✅ (siparis sistemi eksik)
3. **Gunduz: Dukkandan alisveris yap** — Henuz yok ❌
4. **Gece: PvP savas + Soygun** — Henuz yok ❌
5. **Round dongusu → Kazanma kosulu** — State machine var ✅, itibar sistemi yok ❌

**MVP Hedefi (GUNCELLENDI — PO Review):** Sprint **5** sonunda "oynanabilir tam dongu" — gunduz fazi + temel gece fazi (basit combat). Sprint 4 sonunda "playable demo" (gunduz tam, gece placeholder).

> **NEDEN SPRINT 5?** Oyunun deger onerisi "gunduz pisir, gece savas". Sadece gunduz fazi olan bir oyun cooking+combat oyununun MVP'si olamaz. Sprint 4 "demo", Sprint 5 "gercek MVP".

### Basari Kriterleri (Ogrenme Projesi)
Bu bir ogrenme/portfolyo projesi oldugu icin basari olcutleri ticari bir oyundan farkli:

| Kriter | Olcut | Oncelik |
|--------|-------|---------|
| Multiplayer stabil calisiyor | 4+ kisi 15dk sorunsuz oynayabilmeli | Yuksek |
| Gunduz dongusu tatmin edici | Pisir-servis et-para kazan akisi sezgisel | Yuksek |
| Gorsel kimlik tutarli | Art style belirlenmis ve uygulanmis | Orta |
| Gece fazi heyecan verici | PvP + soygun mekaniği eglenceli | Orta |
| Portfolyo kalitesi | Demo video/GIF cekilebilir | Yuksek |
| Teknik ogrenme | NGO, server-auth, state machine deneyimi | Yuksek |

### Oyuncu Deneyimi Akisi (Player Journey)
```
Ana Menu → Lobi Olustur/Katil → Takim Atama (otomatik)
    ↓
GUNDUZ (120s):
  Malzeme al → Pisir → Servis et → Para kazan
  Dukkana git → Silah/upgrade/malzeme al
    ↓
GECE (60s):
  Silahlan → Dusman restoranina git → Kasa soy
  Kendi restoranini koru → Oldur/Ol
    ↓
ROUND SONU → Itibar kontrolu → Yeni round veya GAME OVER
```

**Kritik UX Sorulari (karar verilmeli):**
- Yeni oyuncu ilk 30 saniyede ne yapmasi gerektigini biliyor mu? → Tutorial/onboarding gerekli
- Gece fazinda olunce ne oluyor? Sıkılıyor mu? → Spectator sistemi onemli
- Ekonomi dengesi nasil? Cok para kazanmak mi kolay, harcamak mi? → Playtest ile belirlenecek

---

## Ekip Rolleri ve Guc Dagilimi

### Alperen Elbiz
- **Developer:** Core systems, game flow, day phase, networking, UI scripting
- **3D Artist:** Tum 3D modelleme, texturing, material olusturma
- **UI/UX Designer:** Tum UI tasarim, layout, UX akisi, animasyonlar

### Tuna Yavuz
- **Developer:** Customer/restaurant sistemi, NPC AI, NavMesh

### Calisma Prensibi
Alperen uzerinde cok buyuk bir yuk var (3 rol). Tuna'nin developer olarak daha fazla sorumluluk almasi, Alperen'in 3D ve UI/UX islerine zaman ayirabilmesi icin kritik.

---

## Sprint Plani

### Sprint 0: Stabilite & Bug Fix (1 Hafta)

> Amac: Mevcut kodu saglam hale getir, kritik bug'lari fix et

**Alperen (Dev) — Mimari Fix'ler:**
- [ ] **M1 KRITIK** — GameManager.cs: Late-join state sync ekle (OnNetworkSpawn'da current state fire)
- [ ] **M2** — PlayerInteraction.cs: Tum day-phase RPC'lere GameState guard ekle
- [ ] **M3** — PlayerInteraction.cs: DayPhaseCleanup event'ine subscribe ol, CarriedItem sifirla
- [ ] **R2** — PlayerInteraction.cs: CollectDish'te bos el kontrolu ekle

**Alperen (Dev) — Kod Temizligi:**
- [ ] **M6** — GameManager.cs: StartTransition'daki DayPhaseCleanup cagirisini kaldir
- [ ] **R10** — RecipeDatabase.cs: OnDestroy'da Instance = null ekle
- [x] ~~**R1** — ServingCounter NetworkList~~ — GECERSIZ: NGO 1.x pattern'i, bug degil

**Tuna (Dev):**
- [ ] **R5** — RestaurantRegistry.cs: All property'sini immutable dondur
- [ ] **R3** — LobbyManager.cs: async void → async Task, CancellationToken ekle
- [ ] **R9** — RelayManager.cs: NetworkManager.Singleton null kontrolu ekle
- [ ] **M5** — Host disconnect: GameManager.OnNetworkDespawn'da cleanup + ana menuye don
- [x] ~~**R4** — gameStarted flag~~ — GECERSIZ: Cleanup() icinde zaten reset ediliyor

**Beraber:**
- [ ] README.md: Unity versiyonunu "Unity 6 LTS (6000.3)" olarak guncelle
- [ ] Namespace tutarliligi: Tum script'lere `RistoranteRumble` namespace'i ekle
- [ ] **Sprint 1 Hazirlik:** Tuna OrderData struct'ini tanimlar (Sprint 0 icinde), Alperen buna karsi stub yazar

---

### Sprint 1: Faz 3 Tamamlama — Siparis Sistemi (2 Hafta)

> Amac: Gunduz fazinin core loop'unu tamamla (musteri → siparis → yemek → para)

**Tuna (Dev) — Musteri Siparis Entegrasyonu:**
- [ ] CustomerAgent'a siparis verme mekanigi ekle (otur → siparis → bekle → ye/git)
- [ ] OrderData struct'ini kullanarak NetworkVariable ile siparis sync'i
- [ ] Musteri sabir suresi (patience timer) — sure dolunca mutsuz ayrilma
- [ ] Basarili siparis teslimi → EconomyManager.AddMoney cagrisi
- [ ] Basarisiz siparis → itibar dusme event'i (henuz ReputationManager yok, event hazirla)
- [ ] Sahte musteri temel mekanigi (baska takimdan gonderilen, basarisiz servis = itibar kaybi)

**Alperen (Dev) — Siparis Akis Entegrasyonu:**
- [ ] ServingCounter'dan yemek alma → musteri masasina gotürme akisi
- [ ] PlayerInteraction'a musteri servisi ekleme (E ile masaya yemek birakma)
- [ ] Siparis baloncugu (world-space UI) — musteri ustunde ne istedigini gosteren
- [ ] DayPhaseHUD'a siparis durumu ekleme

**Alperen (UI/UX):**
- [ ] Siparis baloncugu UI tasarimi (OrderBubble prefab)
- [ ] Musteri memnuniyet gostergesi (yesil → sari → kirmizi progress bar)
- [ ] DayPhaseHUD'a aktif siparis listesi

**Alperen (3D):**
- [ ] Temel yemek modelleri (pizza, burger — en az 2 tarif icin)
- [ ] Malzeme modelleri (domates, peynir, hamur, et — ikonlar yeterli olabilir)

---

### Sprint 2: Restoran Ortami & Gorsel Kimlik (2-3 Hafta)

> Amac: Oyunun gorsel kimligini olustur, greybox'tan cikar

**Alperen (3D Artist) — Ana Odak:**
- [ ] **Art style belirleme** — Low-poly? Stylized? Cartoon? Referans boardu hazirla
- [ ] Restoran ic mekan modeli (mutfak alani, lounge, servis tezgahi)
- [ ] Pisirme istasyonu modelleri (firin, izgara, kesme tahtasi)
- [ ] Masa ve sandalye varyantlari (2'li, 4'lu, VIP)
- [ ] Buzdobali ve kasa modeli
- [ ] Malzeme kutusu/rafi modeli (IngredientSource gorsel karsiligi)
- [ ] Temel cevre elemanlari (zemin, duvar, tavan, kapilar)
- [ ] Isik/dekorasyon elemanlari (restoran havasini olusturacak)

**Alperen (UI/UX):**
- [ ] Renk paleti ve UI tema tanimlama (ScriptableObject ile)
- [ ] Icon seti (malzemeler, tarifler, para, itibar)
- [ ] Buton, panel, slider ozel grafikleri
- [ ] Font secimi (TMP uyumlu)

**Tuna (Dev) — Restoran Yerlestirme Temeli:**
- [ ] Restaurant.cs'e slot sistemi ekle (KitchenSlot, LoungeSlot, StorageSlot)
- [ ] SlotSystem.cs — slot bos mu, tip uyumlu mu kontrolleri
- [ ] Slot gorsel feedback (highlight, ghost outline) icin altyapi
- [ ] Restoran layout'unu RestaurantLayoutSO ile data-driven yap

**Beraber:**
- [ ] 3D modelleri sahneye yerlestirme ve prefab olusturma
- [ ] Isik ayarlari ve post-processing guncelleme

---

### Sprint 3: Ekonomi, Dukkan & Upgrade Sistemi (2 Hafta)

> Amac: Faz 4'u buyuk olcude tamamla

**Alperen (Dev):**
- [ ] ShopManager.cs — malzeme, silah, upgrade satin alma (server-auth)
- [ ] PlayerInventory.cs — NetworkVariable ile envanter
- [ ] UpgradeSO + UpgradeManager.cs (kalici + round bazli + tuketime dayali)
- [ ] SafeSystem.cs — kasa obje, para depolama

**Alperen (UI/UX):**
- [ ] Dukkan UI tasarimi (tab'li: malzeme, silah, upgrade)
- [ ] Envanter UI (oyuncunun uzerindekiler)
- [ ] Restoran yerlestirme modu UI (slot highlight, onay)
- [ ] Upgrade tree/listesi gorseli

**Tuna (Dev):**
- [ ] Buzdobali sistemi (IngredientStorage.cs — kapasite, stok yonetimi)
- [ ] Malzeme alis → buzdobabina koyma → buzdobabindan alma akisi
- [ ] Restoran yerlestirme modu server logic (PlaceItemRpc, RemoveItemRpc)

**Alperen (3D):**
- [ ] Dukkan ortami modeli
- [ ] Silah modelleri (tabanca, pompali, tava — Faz 5'e hazirlik)

---

### Sprint 4: UI Polish & Ses Temeli (1-2 Hafta)

> Amac: Oyunu oynanabilir hale getir (game feel)

**Alperen (UI/UX) — Ana Odak:**
- [ ] Tum panellere gec/kapa animasyonu ekle (DOTween veya Unity Animation)
- [ ] Buton hover/press efektleri
- [ ] Async islemlerde yukleniyor gostergesi + buton deaktif
- [ ] Hata mesajlari (toast notification sistemi)
- [ ] Renk koru dostu alternatifler (icon + renk kombinasyonu)
- [ ] Skor tablosu / round ozeti ekrani
- [ ] **YENİ: Temel onboarding/tutorial** — Ilk oyunda context-sensitive ipuclari ("E ile malzeme al", "Pisirme istasyonuna birak" vb.)
- [ ] **YENİ: Playtest feedback formu** (Google Form, 5-10 soru)

**Alperen (Ses):**
- [ ] Temel ses efektleri kaynak bul/olustur:
  - UI tiklama, basari, hata sesleri
  - Yemek pisirme sesleri (cizirdama, firin, kesme)
  - Para kazanma sesi
  - Faz gecis sesi
- [ ] Muzik track'leri kaynak bul:
  - Menu muzigi
  - Gunduz fazi muzigi (hafif, enerjik)
  - Gece fazi muzigi (gerilimli, karanlik)
- [ ] AudioManager.cs (singleton, ses seviyesi kontrolu)

**Tuna (Dev):**
- [ ] Itibar sistemi temeli (ReputationManager.cs — NetworkVariable, formul)
- [ ] Itibar degisim event'leri (GameEvents'e ekle)
- [ ] Musteri spawn sikligi itibara bagli hale getir

---

### Sprint 5: Gece Fazi — Combat Temeli (3 Hafta)

> Amac: Gece fazinin temel mekanigini olustur

**Tuna (Dev) — Combat Core:**
- [ ] WeaponSO veri yapisi (hasar, menzil, fire rate, spread, mermi)
- [ ] WeaponSystem.cs (equip, ates, reload — NetworkBehaviour)
- [ ] CombatManager.cs (server-side hit detection, hasar hesaplama)
- [ ] HealthSystem.cs (NetworkVariable HP, olum, respawn timer)
- [ ] Projectile.cs (NetworkObject — hitscan + fiziksel mermi)

**Alperen (Dev) — Player Controller:**
- [ ] NightMovement.cs (3rd person shooter hareketi, kameraya gore yon)
- [ ] NightCameraController.cs (3rd person follow + mouse look)
- [ ] Input Action Map swap (DayPhase ↔ NightPhase)
- [ ] Cursor lock/unlock faz gecisinde

**Alperen (UI/UX):**
- [ ] Nisan UI (CrosshairUI)
- [ ] Saglik gostergesi (HealthBarUI)
- [ ] Mermi sayaci
- [ ] Hasar sayisi (world-space DamageNumber)
- [ ] Kill feed / bildirim

**Alperen (3D):**
- [ ] Gece arenasi / sehir ortami modelleme
- [ ] Silah modelleri detaylandirma
- [ ] Karakter silah tutma animasyonlari (varsa asset store'dan)

---

### Sprint 6: Gece Fazi — Soygun & Ileri Mekanikler (2 Hafta)

> Amac: Soygun mekaniği ve ileri mekanikler

**Tuna (Dev):**
- [ ] HeistManager.cs (kasa etkilesim, para calma)
- [ ] DroppedMoney.cs (olunce para dusurme — NetworkObject)
- [ ] SpectatorSystem.cs (olum sonrasi izleme)
- [ ] Respawn sistemi (timer + spawn noktasi)

**Alperen (Dev):**
- [ ] Para tasima mekanigi (calan oyuncu yavaslar)
- [ ] Sahte musteri gonderme sistemi (FakeCustomerSystem)
- [ ] Player siniflari (Chef, Runner, Fighter) — SO-driven bonus
- [ ] Kazanma kosulu kontrolu (%70-%30 itibar)
- [ ] Game Over ekrani

**Alperen (3D/VFX):**
- [ ] Para objesi modeli
- [ ] Ates, duman, patlama partikulleri
- [ ] Hasar efektleri
- [ ] Faz gecis efekti

---

### Sprint 7: Connection & Polish (2 Hafta)

> Amac: Baglanti guvenirliligi ve genel polish

**Tuna (Dev):**
- [ ] ConnectionManager.cs (state machine — Faz 8)
- [ ] Graceful disconnect (oyuncu duserse)
- [ ] Reconnection destegi (mumkunse)
- [ ] Hata mesajlari (timeout, connection lost)

**Alperen (Dev + UI/UX):**
- [ ] Tum UI ekranlarinin son polish'i
- [ ] Ayarlar ekrani (ses, grafik, kontrol)
- [ ] Tutorial / onboarding akisi
- [ ] Performans optimizasyonu (object pooling, LOD)

**Beraber:**
- [ ] Playtest — en az 4 kisilik test
- [ ] SO degerleriyle balance tuning
- [ ] Bug fixing

---

## Oncelik Matrisi

```
ACIL & ONEMLI (Simdi Yap)
├── Sprint 0: Bug fix'ler
├── Sprint 1: Siparis sistemi (Faz 3 bitirmek)
└── Art style belirleme (Sprint 2 oncesi karar)

ONEMLI (Yakin Gelecek)
├── Sprint 2: Gorsel kimlik + restoran ortami
├── Sprint 3: Ekonomi sistemi
└── Sprint 4: UI polish + ses

GEREKLI (Orta Vade)
├── Sprint 5: Gece fazi combat
├── Sprint 6: Soygun + ileri mekanikler
└── Sprint 7: Connection + polish

OPSIYONEL (Gec Vade)
├── Otomatik test altyapisi
├── CI/CD build pipeline
├── Host migration
└── Mobil platform destegi
```

---

## Tuna Icin Ozel Notlar

Tuna'nin su ana kadarki katkisi (Customer/Restaurant sistemi) yuksek kalitede. `CustomerAgent.cs` ve `Restaurant.cs` projenin en iyi yazilmis dosyalari arasinda. Oneriler:

1. **Daha genis sorumluluk al:** Alperen 3 rol tasiyor. Tuna'nin combat, ekonomi veya network tarafinda daha aktif olmasi gerekiyor.
2. **LobbyManager refactoru iyi bir baslangiç:** En cok sorun olan dosya — async pattern'leri duzelterek buyuk etki yaratabilir.
3. **Combat sistemi Tuna'ya uygun:** Server-side hit detection, hasar hesaplama gibi backend-agirlikli isler.
4. **Siparis sistemi oncelikli:** CustomerAgent zaten var, siparis mekanigini eklemek dogal uzanti.

## Alperen Icin Ozel Notlar

1. **3D asset uretimi darbogazdir:** Oyunun gorunumu tamamen Alperen'e bagli. Art style kararini erken ver.
2. **UI/UX ikinci darbogazdir:** Her yeni ozellik yeni UI gerektiriyor. UI framework/tema sistemi kur bir kere, sonra hizli uret.
3. **Developer rolunü minimize et (mumkun oldukça):** Kritik core isler haricinde (PlayerInteraction, GameManager) diger kodlama islerini Tuna'ya devret.
4. **Asset Store'dan yararlan:** Tum 3D modelleri sifirdan yapma. Placeholder veya stylized asset pack'ler zaman kazandirir.

---

## Cevaplanan Sorular

1. **Art style:** Stylized lowpoly yonelimli — Fortnite renk paleti + Overcooked basitligi. Arastirma devam ediyor.
2. **Hedef platform:** Sadece PC. Mobil kisitlamasi yok, URP PC preset kullanilabilir.
3. **Release tarihi:** Yok — ogrenme/portfolyo projesi. Kalite ve ogrenme oncelikli.
4. **Tuna musaitligi:** Sorun yok, musait.

## Acik Sorular

5. **Ses icin kaynak:** Ozel ses mi uretilyecek, asset store mi, free kaynak mi? Ses uretimi ayri bir uzmanliktir.
6. **Oyun nasil dagitilacak?** GitHub Releases veya itch.io oneriliyor. CI/CD build pipeline Sprint 1'de kurulmali.
7. **Playtest grubu var mi?** 5v5 test icin en az 4-6 kisi gerek. Sprint 3 sonunda ilk playtest yapilmali.

---

## PO/PM & Mimari Review Bulgulari (16 Nisan 2026)

### Scope Cut Kararlari (2 Kisilik Ekip Icin Onerilen)

Bu kararlar kesinlestirilmeli — PO olarak Alperen'in onaylamasi gerekir:

| Ozellik | GDD'de | MVP'de | Oneri |
|---------|--------|--------|-------|
| Gunduz core loop | Detayli | Evet | Tam implement |
| Gece combat (basit) | Detayli | Evet | Hitscan only, 1 silah tipi yeterli |
| Ekonomi/dukkan | Detayli | Evet | Basit versiyon (malzeme + silah) |
| Itibar sistemi | Detayli | Evet | Basit % tracking, kazanma kosulu |
| **Sahte musteri** | Detayli | **HAYIR — KES** | Karmasik, MVP sonrasi |
| **Oyuncu siniflari** | Detayli | **HAYIR — KES** | 3 varyant x model = agir, MVP sonrasi |
| **Spectator modu** | Detayli | **HAYIR — KES** | Gece fazinda olunce respawn timer yeterli |
| **Restoran yerlestirme** | Detayli | Basit | Slot sistemi var ama drag-drop UI MVP sonrasi |
| **Upgrade tier sistemi** | Detayli | Basit | Tek tier yeterli (al/kullan), tier tree MVP sonrasi |

### Playtest Plani (YENİ — eksikti)

```
Sprint 3 Sonu — Playtest #1 (Gunduz Fazi)
├─ Katilimci: 4 kisi (2 dev + 2 arkadaslar)
├─ Sure: 30 dk
├─ Sorular:
│  ├─ "Pisir → servis et → para kazan dongusunu anladin mi?"
│  ├─ "Ne yapmam gerektigini tutorial olmadan cozdum mu?"
│  ├─ "2 dakikalik timer dogru mu?"
│  └─ "Eklenmesini istedigin sey?"
└─ Cikti: 1 sayfa feedback ozeti

Sprint 5 Sonu — Playtest #2 (Tam Dongu)
├─ Katilimci: 6 kisi (5v5 hedef ama 3v3 yeterli)
├─ Sorular:
│  ├─ "Gece fazi eglenceli mi?"
│  ├─ "Gunduz kazandigin para geceyi etkiliyor mu?"
│  └─ "Kazanma kosulu (itibar) anlasilir mi?"
└─ Cikti: Balance ayarlari icin veri

Sprint 7 — Playtest #3 (Polish)
├─ Katilimci: 8-10 kisi
├─ Tam 5v5 test
└─ Performans ve stabilite odakli
```

### Sprint 1 Bagimlk Duzeltmesi (YENİ — blocker bulundu)

**Sorun:** Sprint 1'de Tuna OrderSystem yazarken Alperen servis akisi yaziyor — ama Alperen'in kodu Tuna'nin tanimladigi OrderData struct'ina bagimli.

**Cozum:**
1. **Sprint 0 icinde** (son 1-2 gun): Tuna `OrderData` struct'ini ve `IOrderHandler` interface'ini tanimlar
2. **Sprint 1 Hafta 1:** Tuna CustomerAgent'a siparis mekanigi ekler, Alperen interface'e karsi stub yazar
3. **Sprint 1 Hafta 2:** Entegrasyon — iki taraf birlestirilir

### Balance Gate'leri (YENİ — eksikti)

Her sprint sonunda kontrol:
- **Sprint 3:** 4 kisi 120 saniyede 3+ siparis tamamlayabiliyor mu?
- **Sprint 4:** Ortalama kazanc 200-500₺ arasi mi? (0 veya 5000 degilse dengesiz)
- **Sprint 5:** Gece fazinda en az 1 basarili soygun oluyor mu?
- **Sprint 6:** Hic bir takim hem gunduz hem gece dominant olmuyor mu?

### Mimari Risk Registeri (YENİ)

| Risk | Olasilik | Etki | Mitigasyon |
|------|----------|------|-----------|
| Late-join state sync (M1) | %100 | Kritik | Sprint 0'da fix |
| 3 haftada combat sistemi yetersiz | %80 | Yuksek | Sprint 4'te 3rd person kamera prototipi yap |
| Art style karari gecikir | %70 | Yuksek | Bu hafta karar ver |
| 5v5 olcekte performans sorunu | %40 | Orta | Sprint 3'te 6+ oyuncuyla profiling |
| Customer AI pathfinding karisik | %30 | Orta | Sprint 1'de NavMesh testi |

---

*Bu yol haritasi 16 Nisan 2026 tarihinde projenin mevcut durumuna gore hazirlanmistir.*
*PO/PM Review: 16 Nisan 2026 — MVP yeniden tanimlandi (Sprint 5), scope cut onerildi, playtest plani eklendi.*
*Mimari Review: 16 Nisan 2026 — 6 yeni mimari sorun bulundu (M1-M6), 3 yanlis iddia duzeltildi (R1, R4, R7).*
