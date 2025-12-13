# 🎮 Ristorante Rumble - Multiplayer Yol Haritası

> Bu döküman, oyunun online multiplayer sisteminin geliştirilmesi için takip edilmesi gereken adımları içerir.

---

## 📊 Mevcut Durum Özeti

### ✅ Tamamlanan Özellikler

| Özellik | Dosya | Durum |
|---------|-------|-------|
| NetworkManager UI (Host/Client/Server) | `Scripts/UI/NetworkManagerUI.cs` | ✅ Temel |
| Player Network Behaviour | `Scripts/Player/PlayerNetwork.cs` | ✅ Temel |
| Owner Network Animator | `Scripts/Player/OwnerNetworkAnimator.cs` | ✅ Tamamlandı |
| Unity Services Başlatma | `Scripts/Lobby/TestLobby.cs` | ✅ Tamamlandı |
| Anonim Kimlik Doğrulama | `Scripts/Lobby/TestLobby.cs` | ✅ Tamamlandı |
| Lobi Oluşturma | `Scripts/Lobby/TestLobby.cs` | ✅ Temel |
| Lobi Listeleme | `Scripts/Lobby/TestLobby.cs` | ✅ Temel |
| Heartbeat Sistemi | `Scripts/Lobby/TestLobby.cs` | ✅ Tamamlandı |

### ❌ Eksik Özellikler

| Özellik | Öncelik | Açıklama |
|---------|---------|----------|
| ~~Relay Entegrasyonu~~ | ✅ Tamamlandı | İnternet üzerinden bağlantı |
| ~~Lobiye Katılma~~ | ✅ Tamamlandı | ID/Kod ile katılım |
| ~~Lobiden Ayrılma~~ | ✅ Tamamlandı | Temiz çıkış |
| ~~Lobi UI Sistemi~~ | ✅ Tamamlandı | Kullanıcı arayüzü |
| Oyuncu Veri Yönetimi | 🟡 Orta | İsim, karakter seçimi |
| ~~Lobi Polling~~ | ✅ Tamamlandı | Gerçek zamanlı güncelleme |
| ~~Oyun Başlatma Akışı~~ | ✅ Tamamlandı | Host'un oyunu başlatması |
| Sahne Yönetimi | 🟡 Orta | Lobi → Oyun geçişi |
| Oyuncu Spawn Sistemi | 🟡 Orta | Spawn noktaları |
| Bağlantı Kopma Yönetimi | 🟡 Orta | Reconnection |
| ~~Quick Join~~ | ✅ Tamamlandı | Hızlı eşleşme |
| Özel Lobi (Şifreli) | 🟢 Düşük | Arkadaşlarla oynama |

---

## 🗺️ Geliştirme Aşamaları

### Faz 1: Temel Lobi Sistemi 🔴 (Öncelikli)

#### 1.1 Relay Servis Entegrasyonu ✅
- [x] Unity Relay paketini projeye ekle
- [x] `RelayManager.cs` oluştur
- [x] Allocation oluşturma implementasyonu
- [x] Join Code alma/kullanma sistemi
- [x] UnityTransport ile Relay bağlantısı

```csharp
// Örnek Relay Manager yapısı
public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }
    
    public async Task<string> CreateRelay(int maxPlayers);
    public async Task<bool> JoinRelay(string joinCode);
}
```

#### 1.2 Lobi Yönetim Sistemi ✅
- [x] `LobbyManager.cs` oluştur (TestLobby'yi genişlet)
- [x] Singleton pattern uygula
- [x] Event sistemi ekle (OnLobbyCreated, OnLobbyJoined, vb.)

**Gerekli Metodlar:**
- [x] `CreateLobby(string lobbyName, int maxPlayers, bool isPrivate)`
- [x] `JoinLobbyById(string lobbyId)`
- [x] `JoinLobbyByCode(string lobbyCode)`
- [x] `QuickJoinLobby()`
- [x] `LeaveLobby()`
- [ ] `KickPlayer(string playerId)`
- [ ] `UpdateLobbyData(Dictionary<string, string> data)`
- [ ] `UpdatePlayerData(Dictionary<string, string> data)`

#### 1.3 Lobi Veri Yapıları
- [ ] `LobbyData.cs` - Lobi bilgilerini tutan struct
- [ ] `PlayerLobbyData.cs` - Oyuncu lobi verisi

```csharp
// Örnek veri yapıları
public struct LobbyPlayerData
{
    public string PlayerId;
    public string PlayerName;
    public bool IsReady;
    public int CharacterIndex;
    public int TeamIndex;
}
```

---

### Faz 2: UI Sistemi ✅ (Tamamlandı)

#### 2.1 Ana Menü UI
- [x] `MainMenuUI.cs` oluştur
- [x] Play butonu
- [x] Settings butonu
- [x] Quit butonu

#### 2.2 Lobi Tarayıcı UI
- [x] `LobbyBrowserUI.cs` oluştur
- [x] Lobi listesi görüntüleme
- [x] Refresh butonu
- [x] Create Lobby butonu
- [x] Quick Join butonu
- [ ] Lobi filtreleme (opsiyonel)

#### 2.3 Lobi Oluşturma UI
- [x] `CreateLobbyUI.cs` oluştur
- [x] Lobi ismi input
- [x] Maksimum oyuncu seçimi
- [ ] Oyun modu seçimi
- [ ] Public/Private toggle
- [x] Create butonu

#### 2.4 Lobi Odası UI
- [x] `LobbyRoomUI.cs` oluştur
- [x] Oyuncu listesi
- [x] Ready butonu
- [x] Leave butonu
- [x] Start Game butonu (sadece host)
- [ ] Chat sistemi (opsiyonel)
- [x] Lobi kodu gösterimi

#### 2.5 UI Prefab'ları
- [x] LobbyPlayerCard prefab (PlayerItem)
- [x] LobbyListItem prefab (LobbyItem)
- [ ] Loading screen prefab

---

### Faz 3: Oyun Akışı 🟡

#### 3.1 Oyun Başlatma
- [ ] `GameStarter.cs` oluştur
- [ ] Tüm oyuncuların hazır olduğunu kontrol et
- [ ] Relay bağlantısını kur
- [ ] NetworkManager'ı başlat
- [ ] Sahne geçişini yönet

```csharp
// Oyun başlatma akışı
public async Task StartGame()
{
    // 1. Relay oluştur
    string relayCode = await RelayManager.Instance.CreateRelay(maxPlayers);
    
    // 2. Relay kodunu lobiye kaydet
    await LobbyManager.Instance.UpdateLobbyData("RelayJoinCode", relayCode);
    
    // 3. Network'ü başlat
    NetworkManager.Singleton.StartHost();
    
    // 4. Oyun sahnesine geç
    NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
}
```

#### 3.2 Sahne Yönetimi
- [ ] `SceneController.cs` oluştur
- [ ] Lobi sahnesi
- [ ] Oyun sahnesi
- [ ] Sahne geçiş animasyonları
- [ ] Loading screen

#### 3.3 Oyuncu Spawn Sistemi
- [ ] `PlayerSpawnManager.cs` oluştur
- [ ] Spawn noktaları tanımla
- [ ] Takım bazlı spawn
- [ ] Respawn sistemi

---

### Faz 4: Oyuncu Yönetimi 🟡

#### 4.1 Oyuncu Profili
- [ ] `PlayerProfile.cs` oluştur
- [ ] Oyuncu ismi kaydetme (PlayerPrefs)
- [ ] Karakter seçimi
- [ ] İstatistikler (opsiyonel)

#### 4.2 Karakter Seçim Sistemi
- [ ] `CharacterSelectUI.cs` oluştur
- [ ] Karakter önizleme
- [ ] Seçim senkronizasyonu

#### 4.3 Takım Sistemi (Eğer Gerekli)
- [ ] `TeamManager.cs` oluştur
- [ ] Takım atama
- [ ] Takım dengeleme

---

### Faz 5: Bağlantı Güvenilirliği 🟡

#### 5.1 Bağlantı Durumu Yönetimi
- [ ] `ConnectionManager.cs` oluştur
- [ ] Bağlantı durumu UI
- [ ] Timeout yönetimi
- [ ] Ping gösterimi

#### 5.2 Disconnect Handling
- [ ] Graceful disconnect
- [ ] Host migration (gelişmiş)
- [ ] Reconnection sistemi

#### 5.3 Error Handling
- [ ] Kullanıcı dostu hata mesajları
- [ ] Retry mekanizması
- [ ] Fallback stratejileri

---

### Faz 6: Polish & Ekstralar 🟢

#### 6.1 Ses & Görsel Feedback
- [ ] UI ses efektleri
- [ ] Bağlantı bildirimleri
- [ ] Animasyonlar

#### 6.2 Chat Sistemi
- [ ] Lobi içi chat
- [ ] Oyun içi chat
- [ ] Emote sistemi

#### 6.3 Arkadaş Sistemi
- [ ] Arkadaş listesi
- [ ] Davet sistemi
- [ ] Son oynananlar

---

## 📁 Önerilen Klasör Yapısı

```
Assets/
├── Scripts/
│   ├── Lobby/
│   │   ├── LobbyManager.cs
│   │   ├── RelayManager.cs
│   │   ├── LobbyData.cs
│   │   └── LobbyEvents.cs
│   │
│   ├── UI/
│   │   ├── MainMenuUI.cs
│   │   ├── LobbyBrowserUI.cs
│   │   ├── CreateLobbyUI.cs
│   │   ├── LobbyRoomUI.cs
│   │   └── LoadingScreenUI.cs
│   │
│   ├── Player/
│   │   ├── PlayerNetwork.cs
│   │   ├── PlayerProfile.cs
│   │   ├── PlayerSpawnManager.cs
│   │   └── OwnerNetworkAnimator.cs
│   │
│   ├── Game/
│   │   ├── GameManager.cs
│   │   ├── GameStarter.cs
│   │   └── SceneController.cs
│   │
│   └── Network/
│       ├── ConnectionManager.cs
│       └── NetworkEvents.cs
│
├── Prefabs/
│   ├── UI/
│   │   ├── LobbyPlayerCard.prefab
│   │   ├── LobbyListItem.prefab
│   │   └── LoadingScreen.prefab
│   │
│   └── Player/
│       └── Player.prefab
│
└── Scenes/
    ├── MainMenu.unity
    ├── Lobby.unity
    └── Game.unity
```

---

## 🔧 Gerekli Unity Paketleri

| Paket | Durum | Açıklama |
|-------|-------|----------|
| Netcode for GameObjects | ✅ Yüklü | Ana network framework |
| Unity Lobby | ✅ Yüklü | Lobi servisi |
| Unity Authentication | ✅ Yüklü | Kimlik doğrulama |
| Unity Relay | ✅ Yüklü | P2P bağlantı |
| Unity Transport | ✅ Yüklü | Network transport |
| TextMeshPro | ✅ Yüklü | UI text |

### Relay Paketi Ekleme (Eğer Eksikse)
```
Window → Package Manager → + → Add package by name
com.unity.services.relay
```

---

## ⏱️ Tahmini Süre

| Faz | Tahmini Süre | Notlar |
|-----|--------------|--------|
| Faz 1: Temel Lobi | 2-3 gün | En kritik |
| Faz 2: UI Sistemi | 2-3 gün | Paralel yapılabilir |
| Faz 3: Oyun Akışı | 1-2 gün | Faz 1-2'ye bağlı |
| Faz 4: Oyuncu Yönetimi | 1-2 gün | - |
| Faz 5: Bağlantı Güvenilirliği | 1-2 gün | - |
| Faz 6: Polish | 1-2 gün | İsteğe bağlı |
| **Toplam** | **8-14 gün** | - |

---

## 🎯 Öncelik Sırası

1. **🔴 Relay Entegrasyonu** - İnternet üzerinden oynamak için şart
2. **🔴 LobbyManager Geliştirme** - Katılma/ayrılma fonksiyonları
3. **🔴 Temel Lobi UI** - Kullanıcı deneyimi
4. **🔴 Oyun Başlatma Akışı** - Lobi → Oyun geçişi
5. **🟡 Oyuncu Spawn** - Oyun içi gereklilik
6. **🟡 Karakter Seçimi** - Görsel çeşitlilik
7. **🟢 Chat & Ekstralar** - Nice to have

---

## 🚀 Adım Adım Uygulama Rehberi

> Bu bölüm, geliştirme sürecinde hangi sırayla ilerlemeniz gerektiğini detaylı şekilde açıklar.

### Hafta 1: Temel Altyapı

#### 📍 Gün 1-2: Relay Kurulumu ✅
```
1. ☑ Unity Dashboard'da Relay servisini aktifleştir
   → dashboard.unity3d.com → Proje Seç → Multiplayer → Relay
2. ☑ com.unity.services.relay paketini ekle
   → Window → Package Manager → + → Add package by name
3. ☑ RelayManager.cs oluştur
4. ☑ Host için CreateRelay() yaz
5. ☑ Client için JoinRelay() yaz
6. ☑ Test et (ParrelSync ile)
```

#### 📍 Gün 3-4: LobbyManager Geliştir ✅
```
1. ☑ TestLobby.cs'i LobbyManager.cs olarak yeniden yaz
2. ☑ Singleton pattern ekle
3. ☑ JoinLobbyById() ekle
4. ☑ JoinLobbyByCode() ekle  
5. ☑ LeaveLobby() ekle
6. ☑ Lobby Polling ekle (güncelleme almak için)
7. ☑ Relay kodunu Lobby data'ya kaydetme
```

#### 📍 Gün 5: Bağlantı Testi ✅
```
1. ☑ Host: Lobi oluştur → Relay başlat → NetworkManager.StartHost()
2. ☑ Client: Lobi bul → Relay kodunu al → JoinRelay() → NetworkManager.StartClient()
3. ☑ 2 pencerede test et (ParrelSync)
```

---

### Hafta 2: UI & Akış

#### 📍 Gün 6-7: Temel UI ✅
```
1. ☑ MainMenuUI.cs - Ana menü
2. ☑ LobbyBrowserUI.cs - Lobi listesi
3. ☑ LobbyRoomUI.cs - Lobi odası (oyuncu listesi, ready, start)
4. ☑ Butonları LobbyManager'a bağla
```

#### 📍 Gün 8-9: Oyun Başlatma Akışı
```
1. ☐ GameStarter.cs - Oyunu başlatan mantık
2. ☐ Tüm oyuncular ready → Host "Start" butonunu görsün
3. ☐ Sahne geçişi (Lobby → Game)
4. ☐ Loading screen
```

#### 📍 Gün 10: Spawn Sistemi
```
1. ☐ Spawn noktaları oluştur (boş GameObjectler)
2. ☐ PlayerSpawnManager.cs
3. ☐ Her oyuncu doğru yerde spawn olsun
```

---

### Sonraki Adımlar (İsteğe Bağlı)

```
Gün 11-12: Karakter seçimi UI ve senkronizasyonu
Gün 13-14: Disconnect handling, hata yönetimi
Gün 15+:   Chat sistemi, arkadaş sistemi, polish
```

---

## 🎯 Kritik Uygulama Sırası

```
┌─────────────────────────────────────────────────────────┐
│  1. RELAY SERVİSİ                                       │
│     └─> İnternet üzerinden bağlantı için ŞART           │
├─────────────────────────────────────────────────────────┤
│  2. LOBBY MANAGER (Tam)                                 │
│     └─> Oluştur + Katıl + Ayrıl + Polling               │
├─────────────────────────────────────────────────────────┤
│  3. RELAY + LOBBY ENTEGRASYONU                          │
│     └─> Lobby data'ya relay kodu kaydet                 │
├─────────────────────────────────────────────────────────┤
│  4. TEMEL UI                                            │
│     └─> Lobi listele + Oluştur + Katıl butonları        │
├─────────────────────────────────────────────────────────┤
│  5. OYUN BAŞLATMA                                       │
│     └─> Ready sistemi + Start + Sahne geçişi            │
├─────────────────────────────────────────────────────────┤
│  6. SPAWN SİSTEMİ                                       │
│     └─> Oyuncuları doğru yerde spawn et                 │
└─────────────────────────────────────────────────────────┘
```

---

## 🏁 İlk Adım: Başlangıç Kontrol Listesi

Geliştirmeye başlamadan önce bu adımları tamamla:

### Unity Dashboard Kurulumu ✅
```
1. ☑ dashboard.unity3d.com adresine git
2. ☑ Projeyi seç (Ristorante-Rumble_NGO)
3. ☑ Multiplayer > Relay servisini aktifleştir
4. ☑ Multiplayer > Lobby servisini kontrol et (aktif olmalı)
5. ☑ Project Settings > Services'dan proje ID'sini Unity'ye bağla
```

### Unity Editör Kurulumu ✅
```
1. ☑ Edit > Project Settings > Services > Link Project
2. ☑ Package Manager'dan Relay paketini kontrol et:
   → com.unity.services.relay (yoksa ekle)
3. ☑ ParrelSync'in çalıştığını kontrol et:
   → ParrelSync > Clones Manager
```

---

## ⚠️ Önemli İpuçları

| İpucu | Açıklama |
|-------|----------|
| 🧪 **Her adımı test et** | Bir sonraki adıma geçmeden önce çalıştığından emin ol |
| 👥 **ParrelSync kullan** | 2 Unity penceresi ile lokal test yap |
| 📝 **Debug.Log bol kullan** | Network'te hata ayıklama zor, logla takip et |
| ⏱️ **Rate limiting** | Lobby API'ye saniyede 1'den fazla istek atma |
| 🔄 **Async/Await** | Tüm network çağrıları async olmalı |
| 🛡️ **Try-Catch** | Her network işlemini try-catch'e al |

---

## 📚 Faydalı Kaynaklar

- [Unity Netcode for GameObjects Docs](https://docs-multiplayer.unity3d.com/netcode/current/about/)
- [Unity Lobby Service Docs](https://docs.unity.com/lobby/en-us/manual/unity-lobby-service)
- [Unity Relay Docs](https://docs.unity.com/relay/en-us/manual/relay-overview)
- [CodeMonkey Multiplayer Tutorial](https://www.youtube.com/watch?v=7glCsF9fv3s)

---

## 📝 Notlar

- Tüm network işlemleri `try-catch` bloklarında olmalı
- Rate limiting'e dikkat et (Lobby: 1 req/sec, Relay: benzer)
- Test için ParrelSync kullan (projede mevcut)
- Her faz sonunda test yap

---

---

## 📄 İlgili Dökümanlar

| Döküman | Açıklama |
|---------|----------|
| [PHASE3_GAMEFLOW_ROADMAP.md](./PHASE3_GAMEFLOW_ROADMAP.md) | Faz 3 detaylı geliştirme rehberi |
| [PROJECT_ARCHITECTURE.md](./PROJECT_ARCHITECTURE.md) | Proje genel mimarisi |
| [NETCODE_ARCHITECTURE.md](./NETCODE_ARCHITECTURE.md) | NGO teknik referans |

---

*Son Güncelleme: 13 Aralık 2025*  
*Versiyon: 1.4 - Faz 3 Roadmap Eklendi*

