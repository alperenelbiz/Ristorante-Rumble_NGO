# 🌐 Netcode for GameObjects (NGO) Mimari Rehberi

> Bu döküman, Unity Netcode for GameObjects framework'ünün mimarisini ve Ristorante Rumble projesinde nasıl kullanıldığını açıklar.

---

## 📖 İçindekiler

1. [Temel Kavramlar](#-temel-kavramlar)
2. [Network Topolojisi](#-network-topolojisi)
3. [Temel Bileşenler](#-temel-bileşenler)
4. [Veri Senkronizasyonu](#-veri-senkronizasyonu)
5. [RPC Sistemi](#-rpc-sistemi)
6. [Object Spawning](#-object-spawning)
7. [Ownership Sistemi](#-ownership-sistemi)
8. [Transform & Animator Senkronizasyonu](#-transform--animator-senkronizasyonu)
9. [Bağlantı Akışı](#-bağlantı-akışı)
10. [Best Practices](#-best-practices)

---

## 🎯 Temel Kavramlar

### NGO Nedir?

**Netcode for GameObjects (NGO)**, Unity'nin resmi multiplayer networking çözümüdür. GameObject tabanlı bir mimari sunar ve MonoBehaviour benzeri bir yaklaşım kullanır.

### Temel Terminoloji

| Terim | Açıklama |
|-------|----------|
| **Server** | Oyun mantığını yöneten ve otoriteye sahip olan instance |
| **Host** | Hem Server hem Client olan instance (Listen Server) |
| **Client** | Server'a bağlanan ve komut gönderen instance |
| **Authority** | Bir nesne üzerinde karar verme yetkisi |
| **Owner** | Bir NetworkObject'in sahibi olan client |
| **Spawn** | Bir nesnenin network üzerinde oluşturulması |
| **Despawn** | Bir nesnenin network üzerinden kaldırılması |

---

## 🔷 Network Topolojisi

### Client-Server Modeli

```
                    ┌─────────────┐
                    │   SERVER    │
                    │  (Host)     │
                    │             │
                    │ • Game Logic│
                    │ • Authority │
                    │ • Validation│
                    └──────┬──────┘
                           │
           ┌───────────────┼───────────────┐
           │               │               │
           ▼               ▼               ▼
    ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
    │  CLIENT 1   │ │  CLIENT 2   │ │  CLIENT 3   │
    │             │ │             │ │             │
    │ • Input     │ │ • Input     │ │ • Input     │
    │ • Rendering │ │ • Rendering │ │ • Rendering │
    │ • Prediction│ │ • Prediction│ │ • Prediction│
    └─────────────┘ └─────────────┘ └─────────────┘
```

### Veri Akışı

```
┌────────────────────────────────────────────────────────────┐
│                        SERVER                               │
│                                                            │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐ │
│  │ NetworkVar   │───▶│ Serialize    │───▶│ Broadcast    │ │
│  │ Changes      │    │              │    │ to Clients   │ │
│  └──────────────┘    └──────────────┘    └──────────────┘ │
│         ▲                                       │          │
│         │                                       │          │
│  ┌──────┴───────┐                              │          │
│  │ ServerRpc    │◀─────────────────────────────┘          │
│  │ from Clients │                                         │
│  └──────────────┘                                         │
└────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────┐
│                        CLIENT                               │
│                                                            │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐ │
│  │ Receive      │───▶│ Deserialize  │───▶│ Apply to     │ │
│  │ Data         │    │              │    │ GameObjects  │ │
│  └──────────────┘    └──────────────┘    └──────────────┘ │
│                                                            │
│  ┌──────────────┐    ┌──────────────┐                     │
│  │ User Input   │───▶│ ServerRpc    │────────────────────▶│
│  │              │    │ Call         │    (to Server)      │
│  └──────────────┘    └──────────────┘                     │
└────────────────────────────────────────────────────────────┘
```

---

## 🧱 Temel Bileşenler

### 1. NetworkManager

NetworkManager, tüm network operasyonlarının merkezi yöneticisidir.

```csharp
// Singleton erişimi
NetworkManager.Singleton

// Temel operasyonlar
NetworkManager.Singleton.StartServer();   // Sadece server başlat
NetworkManager.Singleton.StartHost();     // Server + Client başlat
NetworkManager.Singleton.StartClient();   // Client olarak bağlan
NetworkManager.Singleton.Shutdown();      // Bağlantıyı kapat
```

**NetworkManager Özellikleri:**

| Özellik | Açıklama |
|---------|----------|
| `IsServer` | Bu instance server mı? |
| `IsClient` | Bu instance client mı? |
| `IsHost` | Bu instance host mu? (Server + Client) |
| `LocalClientId` | Yerel client'ın ID'si |
| `ConnectedClients` | Bağlı tüm client'ların listesi |
| `ConnectedClientsIds` | Bağlı client ID'leri |

**Events:**

```csharp
NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
NetworkManager.Singleton.OnServerStarted += OnServerStarted;
```

---

### 2. NetworkBehaviour

`NetworkBehaviour`, `MonoBehaviour`'ın network versiyonudur. Network fonksiyonelliği ekler.

```csharp
public class PlayerNetwork : NetworkBehaviour
{
    // NetworkBehaviour özellikleri
    public bool IsOwner { get; }           // Bu client sahibi mi?
    public bool IsServer { get; }          // Server'da mı çalışıyor?
    public bool IsClient { get; }          // Client'da mı çalışıyor?
    public bool IsHost { get; }            // Host'da mı çalışıyor?
    public bool IsLocalPlayer { get; }     // Yerel oyuncu mu?
    public bool IsSpawned { get; }         // Network'te spawn edildi mi?
    public ulong OwnerClientId { get; }    // Sahibinin client ID'si
    public NetworkObject NetworkObject { get; }  // NetworkObject referansı
    
    // Lifecycle metodları
    public override void OnNetworkSpawn() { }    // Network spawn olduğunda
    public override void OnNetworkDespawn() { }  // Network despawn olduğunda
    public override void OnGainedOwnership() { } // Ownership kazanıldığında
    public override void OnLostOwnership() { }   // Ownership kaybedildiğinde
}
```

**Projede Kullanım:**

```csharp
// Scripts/Player/PlayerNetwork.cs
public class PlayerNetwork : NetworkBehaviour
{
    private void Update()
    {
        // Sadece sahip kontrol edebilir
        if (!IsOwner) return;
        
        Move();
    }
}
```

---

### 3. NetworkObject

Her network nesnesinin sahip olması gereken bileşendir.

```
┌─────────────────────────────────────┐
│         GameObject: Player          │
├─────────────────────────────────────┤
│  ☑ NetworkObject                    │
│     • NetworkObjectId: 1            │
│     • IsSpawned: true               │
│     • IsPlayerObject: true          │
│     • OwnerClientId: 0              │
├─────────────────────────────────────┤
│  ☑ PlayerNetwork : NetworkBehaviour │
├─────────────────────────────────────┤
│  ☑ NetworkTransform                 │
├─────────────────────────────────────┤
│  ☑ NetworkAnimator                  │
└─────────────────────────────────────┘
```

**NetworkObject İşlemleri:**

```csharp
// Spawn
NetworkObject networkObject = Instantiate(prefab).GetComponent<NetworkObject>();
networkObject.Spawn();                    // Server ownership ile spawn
networkObject.SpawnWithOwnership(clientId); // Belirli client'a ownership ver
networkObject.SpawnAsPlayerObject(clientId); // Player object olarak spawn

// Despawn
networkObject.Despawn();                  // Network'ten kaldır
networkObject.Despawn(destroy: true);     // Kaldır ve yok et
```

---

## 📊 Veri Senkronizasyonu

### NetworkVariable

`NetworkVariable`, client'lar arasında otomatik senkronize edilen değişkenlerdir.

```csharp
public class PlayerNetwork : NetworkBehaviour
{
    // Temel NetworkVariable
    private NetworkVariable<int> health = new NetworkVariable<int>(100);
    
    // İzin ayarlı NetworkVariable
    private NetworkVariable<int> score = new NetworkVariable<int>(
        value: 0,
        readPerm: NetworkVariableReadPermission.Everyone,    // Herkes okuyabilir
        writePerm: NetworkVariableWritePermission.Server     // Sadece server yazabilir
    );
    
    // Owner yazabilir
    private NetworkVariable<Vector3> position = new NetworkVariable<Vector3>(
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Owner
    );
    
    public override void OnNetworkSpawn()
    {
        // Değer değişikliğini dinle
        health.OnValueChanged += (oldValue, newValue) =>
        {
            Debug.Log($"Health: {oldValue} -> {newValue}");
        };
    }
    
    private void TakeDamage(int damage)
    {
        if (IsServer)
        {
            health.Value -= damage;  // Otomatik senkronize edilir
        }
    }
}
```

### NetworkVariable İzinleri

```
┌─────────────────────────────────────────────────────────────┐
│                   WRITE PERMISSIONS                         │
├─────────────────────────────────────────────────────────────┤
│  Server (default)  │ Sadece server değiştirebilir          │
│  Owner             │ Sadece sahip değiştirebilir           │
├─────────────────────────────────────────────────────────────┤
│                   READ PERMISSIONS                          │
├─────────────────────────────────────────────────────────────┤
│  Everyone (default)│ Herkes okuyabilir                     │
│  Owner             │ Sadece sahip okuyabilir               │
└─────────────────────────────────────────────────────────────┘
```

### Özel Veri Tipleri

Kendi veri tiplerinizi serialize etmek için `INetworkSerializable` kullanın:

```csharp
// Projede: Scripts/Player/PlayerNetwork.cs
public struct MyCustomData : INetworkSerializable
{
    public int _int;
    public bool _bool;
    public FixedString128Bytes message;  // String için FixedString kullan

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _int);
        serializer.SerializeValue(ref _bool);
        serializer.SerializeValue(ref message);
    }
}

// Kullanım
private NetworkVariable<MyCustomData> playerData = new NetworkVariable<MyCustomData>();
```

### Desteklenen Tipler

| Tip | Destekleniyor | Not |
|-----|---------------|-----|
| `int`, `float`, `bool` | ✅ | Primitive tipler |
| `Vector2`, `Vector3`, `Quaternion` | ✅ | Unity tipleri |
| `string` | ❌ | `FixedString` kullan |
| `FixedString32Bytes` - `FixedString4096Bytes` | ✅ | String alternatifi |
| Custom struct | ✅ | `INetworkSerializable` implement et |
| `List<T>`, `Dictionary<K,V>` | ❌ | `NetworkList` kullan |

---

## 📡 RPC Sistemi

### RPC Nedir?

**Remote Procedure Call (RPC)**, bir makineden diğerine fonksiyon çağrısı yapmayı sağlar.

```
┌─────────────────────────────────────────────────────────────┐
│                      RPC TİPLERİ                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   ┌─────────┐     ServerRpc      ┌─────────┐               │
│   │ CLIENT  │ ──────────────────▶│ SERVER  │               │
│   └─────────┘                    └─────────┘               │
│                                                             │
│   ┌─────────┐     ClientRpc      ┌─────────┐               │
│   │ SERVER  │ ──────────────────▶│ CLIENTS │               │
│   └─────────┘                    └─────────┘               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### ServerRpc

Client'tan server'a mesaj gönderir:

```csharp
public class PlayerNetwork : NetworkBehaviour
{
    // Temel ServerRpc
    [ServerRpc]
    private void RequestDamageServerRpc(int damage)
    {
        // Bu kod SADECE SERVER'da çalışır
        TakeDamage(damage);
    }
    
    // Herhangi bir client çağırabilir (owner olmasa bile)
    [ServerRpc(RequireOwnership = false)]
    private void RequestActionServerRpc(ServerRpcParams rpcParams = default)
    {
        // Çağıranın ID'sini al
        ulong senderId = rpcParams.Receive.SenderClientId;
        Debug.Log($"Client {senderId} called this RPC");
    }
    
    private void Update()
    {
        if (!IsOwner) return;
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Client'tan server'a çağır
            RequestDamageServerRpc(10);
        }
    }
}
```

### ClientRpc

Server'dan client'lara mesaj gönderir:

```csharp
public class GameManager : NetworkBehaviour
{
    // Tüm client'lara gönder
    [ClientRpc]
    private void NotifyPlayersClientRpc(string message)
    {
        // Bu kod TÜM CLIENT'larda çalışır
        Debug.Log(message);
        ShowNotification(message);
    }
    
    // Belirli client'lara gönder
    [ClientRpc]
    private void SendToSpecificClientRpc(int data, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"Received: {data}");
    }
    
    private void SendToPlayer(ulong clientId, int data)
    {
        // Sadece belirli client'a gönder
        ClientRpcParams rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        
        SendToSpecificClientRpc(data, rpcParams);
    }
}
```

### RPC Kuralları

| Kural | Açıklama |
|-------|----------|
| **Naming** | ServerRpc: `...ServerRpc`, ClientRpc: `...ClientRpc` son eki zorunlu |
| **Parameters** | Sadece serialize edilebilir tipler |
| **Return** | Void olmalı (veya NetworkBehaviourReference) |
| **Attribute** | `[ServerRpc]` veya `[ClientRpc]` attribute zorunlu |
| **Ownership** | ServerRpc varsayılan olarak sadece owner çağırabilir |

---

## 🎮 Object Spawning

### Spawn Akışı

```
┌─────────────────────────────────────────────────────────────┐
│                    SPAWN AKIŞI                              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. Server prefab'ı Instantiate eder                       │
│                    │                                        │
│                    ▼                                        │
│  2. NetworkObject.Spawn() çağrılır                         │
│                    │                                        │
│                    ▼                                        │
│  3. Server NetworkObjectId atar                            │
│                    │                                        │
│                    ▼                                        │
│  4. Spawn mesajı tüm client'lara gönderilir               │
│                    │                                        │
│                    ▼                                        │
│  5. Client'lar prefab'ı instantiate eder                   │
│                    │                                        │
│                    ▼                                        │
│  6. OnNetworkSpawn() çağrılır (server & clients)          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Spawn Örnekleri

```csharp
public class SpawnManager : NetworkBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject itemPrefab;
    
    // Server tarafında spawn
    public void SpawnEnemy(Vector3 position)
    {
        if (!IsServer) return;
        
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        enemy.GetComponent<NetworkObject>().Spawn();
    }
    
    // Ownership ile spawn
    public void SpawnPlayerItem(ulong clientId)
    {
        if (!IsServer) return;
        
        GameObject item = Instantiate(itemPrefab);
        item.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
    }
    
    // Despawn
    public void DespawnObject(NetworkObject netObj)
    {
        if (!IsServer) return;
        
        netObj.Despawn();  // Sadece despawn, GameObject kalır
        // veya
        netObj.Despawn(destroy: true);  // Despawn ve Destroy
    }
}
```

### NetworkPrefabs Listesi

Spawn edilecek prefab'lar NetworkManager'a kaydedilmelidir:

```
NetworkManager
└── NetworkPrefabs List
    ├── Player.prefab ✓
    ├── Enemy.prefab ✓
    ├── Item.prefab ✓
    └── Projectile.prefab ✓
```

---

## 👤 Ownership Sistemi

### Ownership Nedir?

Ownership, bir NetworkObject üzerinde hangi client'ın kontrol yetkisine sahip olduğunu belirler.

```
┌─────────────────────────────────────────────────────────────┐
│                   OWNERSHIP MODELİ                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │                    SERVER                             │  │
│  │  • Tüm nesneler üzerinde authority                   │  │
│  │  • Ownership değiştirebilir                          │  │
│  │  • Doğrulama yapabilir                               │  │
│  └──────────────────────────────────────────────────────┘  │
│                           │                                 │
│           ┌───────────────┼───────────────┐                │
│           │               │               │                │
│           ▼               ▼               ▼                │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐           │
│  │ Player 1   │  │ Player 2   │  │  Enemy     │           │
│  │ Owner: 0   │  │ Owner: 1   │  │ Owner:     │           │
│  │ (Client 0) │  │ (Client 1) │  │ Server     │           │
│  └────────────┘  └────────────┘  └────────────┘           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Ownership Kontrolleri

```csharp
public class PlayerController : NetworkBehaviour
{
    private void Update()
    {
        // Yöntem 1: IsOwner kontrolü
        if (!IsOwner) return;
        
        // Yöntem 2: OwnerClientId kontrolü
        if (OwnerClientId != NetworkManager.Singleton.LocalClientId) return;
        
        HandleInput();
    }
    
    // Ownership değiştirme (sadece server)
    [ServerRpc]
    private void RequestOwnershipChangeServerRpc(ulong newOwnerId)
    {
        NetworkObject.ChangeOwnership(newOwnerId);
    }
    
    // Ownership callback'leri
    public override void OnGainedOwnership()
    {
        Debug.Log("Bu nesnenin sahibi olduk!");
        EnableControls();
    }
    
    public override void OnLostOwnership()
    {
        Debug.Log("Bu nesnenin sahipliğini kaybettik!");
        DisableControls();
    }
}
```

---

## 🔄 Transform & Animator Senkronizasyonu

### NetworkTransform

Transform verilerini (position, rotation, scale) senkronize eder:

```
┌─────────────────────────────────────────────────────────────┐
│                  NetworkTransform                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Sync Position:  ☑ X  ☑ Y  ☑ Z                            │
│  Sync Rotation:  ☑ X  ☑ Y  ☑ Z                            │
│  Sync Scale:     ☐ X  ☐ Y  ☐ Z                            │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Authority Mode:                                      │   │
│  │   ○ Server Authoritative (Varsayılan)               │   │
│  │   ○ Owner Authoritative (Client Authority)          │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Interpolation: ☑ Enabled                                  │
│  Threshold:     0.001                                      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Client Authoritative Transform

Projede `ClientNetworkTransform` kullanılıyor:

```csharp
// Packages/Multiplayer Samples Utilities/Net/ClientAuthority/ClientNetworkTransform.cs
public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;  // Client (Owner) kontrol eder
    }
}
```

### NetworkAnimator

Animator parametrelerini senkronize eder:

```csharp
// Scripts/Player/OwnerNetworkAnimator.cs
public class OwnerNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;  // Owner animasyonları kontrol eder
    }
}
```

**Kullanım:**

```csharp
public class PlayerNetwork : NetworkBehaviour
{
    private Animator animator;
    
    private void Start()
    {
        animator = GetComponent<Animator>();
    }
    
    private void Move()
    {
        // Animator parametreleri otomatik senkronize edilir
        float speed = moveDir.magnitude * moveSpeed;
        animator.SetFloat("Speed", speed);
    }
}
```

---

## 🔌 Bağlantı Akışı

### Tam Bağlantı Süreci

```
┌─────────────────────────────────────────────────────────────┐
│                   HOST BAŞLATMA                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. UnityServices.InitializeAsync()                        │
│  2. AuthenticationService.SignInAnonymouslyAsync()         │
│  3. Relay.CreateAllocationAsync()                          │
│  4. Relay.GetJoinCodeAsync()                               │
│  5. LobbyService.CreateLobbyAsync()                        │
│  6. UnityTransport.SetHostRelayData()                      │
│  7. NetworkManager.StartHost()                             │
│                                                             │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                   CLIENT KATILMA                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. UnityServices.InitializeAsync()                        │
│  2. AuthenticationService.SignInAnonymouslyAsync()         │
│  3. LobbyService.JoinLobbyByIdAsync()                      │
│  4. Lobby'den RelayJoinCode al                             │
│  5. Relay.JoinAllocationAsync(joinCode)                    │
│  6. UnityTransport.SetClientRelayData()                    │
│  7. NetworkManager.StartClient()                           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Connection Events

```csharp
public class ConnectionManager : NetworkBehaviour
{
    private void Start()
    {
        // Event'lere abone ol
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;
        NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
    }
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected!");
        
        if (IsServer)
        {
            // Server: Yeni client için işlemler
            SpawnPlayerForClient(clientId);
        }
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected!");
        
        if (IsServer)
        {
            // Cleanup işlemleri
            HandleClientDisconnect(clientId);
        }
    }
}
```

---

## ✅ Best Practices

### 1. Authority Kontrolleri

```csharp
// ❌ Yanlış: Kontrol yok
private void TakeDamage(int damage)
{
    health.Value -= damage;
}

// ✅ Doğru: Server kontrolü
private void TakeDamage(int damage)
{
    if (!IsServer) return;
    health.Value -= damage;
}
```

### 2. Null Kontrolü

```csharp
// ❌ Yanlış
NetworkManager.Singleton.StartHost();

// ✅ Doğru
if (NetworkManager.Singleton != null)
{
    NetworkManager.Singleton.StartHost();
}
```

### 3. IsSpawned Kontrolü

```csharp
// ❌ Yanlış
private void Start()
{
    health.Value = 100;  // Network hazır olmayabilir
}

// ✅ Doğru
public override void OnNetworkSpawn()
{
    if (IsServer)
    {
        health.Value = 100;
    }
}
```

### 4. Cleanup

```csharp
public override void OnNetworkDespawn()
{
    // Event aboneliklerini iptal et
    health.OnValueChanged -= OnHealthChanged;
    
    // Kaynakları temizle
    base.OnNetworkDespawn();
}

private void OnDestroy()
{
    if (NetworkManager.Singleton != null)
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }
}
```

### 5. Rate Limiting

```csharp
// ❌ Yanlış: Her frame RPC çağrısı
private void Update()
{
    UpdatePositionServerRpc(transform.position);
}

// ✅ Doğru: Throttle kullan
private float updateInterval = 0.1f;
private float lastUpdateTime;

private void Update()
{
    if (Time.time - lastUpdateTime >= updateInterval)
    {
        UpdatePositionServerRpc(transform.position);
        lastUpdateTime = Time.time;
    }
}
```

---

## 📁 Proje Yapısı Özeti

```
Scripts/
├── Player/
│   ├── PlayerNetwork.cs         # NetworkBehaviour, RPC, NetworkVariable
│   └── OwnerNetworkAnimator.cs  # Client-authoritative animator
│
├── Lobby/
│   └── TestLobby.cs            # Unity Lobby Service entegrasyonu
│
├── UI/
│   └── NetworkManagerUI.cs      # Host/Client/Server butonları
│
└── (Gelecek)
    ├── LobbyManager.cs          # Tam lobi yönetimi
    ├── RelayManager.cs          # Relay entegrasyonu
    └── ConnectionManager.cs     # Bağlantı yönetimi
```

---

## 📚 Ek Kaynaklar

| Kaynak | Link |
|--------|------|
| NGO Resmi Dökümantasyon | [docs-multiplayer.unity3d.com](https://docs-multiplayer.unity3d.com/netcode/current/about/) |
| NGO API Reference | [docs-multiplayer.unity3d.com/netcode/current/api](https://docs-multiplayer.unity3d.com/netcode/current/api/index.html) |
| Unity Multiplayer Samples | [github.com/Unity-Technologies/com.unity.multiplayer.samples.coop](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop) |
| Boss Room Sample | [github.com/Unity-Technologies/com.unity.multiplayer.samples.bossroom](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bossroom) |

---

*Son Güncelleme: 8 Aralık 2024*

