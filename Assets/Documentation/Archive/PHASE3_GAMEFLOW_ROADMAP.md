# 🎮 Faz 3: Oyun Akışı - Geliştirme Yol Haritası

> Bu döküman, Lobi → Oyun geçişi ve temel oyun altyapısının kurulmasını içerir.

---

## 📊 Mevcut Durum

### ✅ Tamamlanan (Faz 1-2)

| Sistem | Dosya | Durum |
|--------|-------|-------|
| LobbyManager | `Scripts/Lobby/LobbyManager.cs` | ✅ Tam |
| RelayManager | `Scripts/Network/RelayManager.cs` | ✅ Tam |
| MainMenuUI | `Scripts/UI/MainMenuUI.cs` | ✅ Tam |
| LobbyBrowserUI | `Scripts/UI/LobbyBrowserUI.cs` | ✅ Tam |
| LobbyRoomUI | `Scripts/UI/LobbyRoomUI.cs` | ✅ Tam |
| CreateLobbyUI | `Scripts/UI/CreateLobbyUI.cs` | ✅ Tam |
| PlayerNetwork | `Scripts/Player/PlayerNetwork.cs` | ✅ Temel hareket |
| OwnerNetworkAnimator | `Scripts/Player/OwnerNetworkAnimator.cs` | ✅ Tam |

### 🎯 Faz 3 Hedefleri

| Özellik | Öncelik | Açıklama |
|---------|---------|----------|
| Sahne Yönetimi | 🔴 Kritik | Lobby → Game sahne geçişi |
| Loading Screen | 🔴 Kritik | Geçiş sırasında loading UI |
| Player Spawn | 🔴 Kritik | Spawn noktaları ile oyuncu spawn |
| GameManager | 🔴 Kritik | Oyun durumu yönetimi |
| Ready Sistemi | 🟡 Orta | Network senkronize ready durumu |
| Team Sistemi | 🟡 Orta | 5v5 takım ataması |

---

## 🗺️ Geliştirme Adımları

### Adım 1: Sahne Yapısı oluştur 🔴

**Süre:** 1-2 saat

#### 1.1 Game Sahnesi Oluştur
```
Scenes/
├── MainMenu.unity     ✅ Mevcut
├── Lobby.unity        (opsiyonel, MainMenu içinde)
└── Game.unity         ❌ OLUŞTUR
```

**Game.unity içeriği:**
- NetworkManager (varolan kullan, DontDestroyOnLoad)
- GameManager (yeni)
- SpawnPoints (boş GameObject'ler)
- Temel zemin/arena

#### 1.2 Sahne Build Settings
```
Build Settings → Scenes In Build:
  0: MainMenu
  1: Game
```

---

### Adım 2: SceneController oluştur 🔴

**Süre:** 2-3 saat

**Dosya:** `Scripts/Game/SceneController.cs`

```csharp
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : NetworkBehaviour
{
    public static SceneController Instance { get; private set; }

    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string menuSceneName = "MainMenu";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadGameScene()
    {
        if (!IsServer) return;
        
        NetworkManager.Singleton.SceneManager.LoadScene(
            gameSceneName, 
            LoadSceneMode.Single
        );
    }

    public void ReturnToMenu()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene(menuSceneName);
    }
}
```

**Görevler:**
- [ ] `Scripts/Game/` klasörü oluştur
- [ ] `SceneController.cs` oluştur
- [ ] NetworkManager'a SceneController ekle
- [ ] LobbyManager.StartGame() içine LoadGameScene() ekle

---

### Adım 3: Loading Screen UI 🔴

**Süre:** 1-2 saat

**Dosya:** `Scripts/UI/LoadingScreenUI.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class LoadingScreenUI : MonoBehaviour
{
    public static LoadingScreenUI Instance { get; private set; }

    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text tipText;

    private string[] tips = {
        "Gündüz yemek yap, gece soygun yap!",
        "Takım arkadaşlarınla iletişim kur!",
        "Kasanı koru, düşman kasasını soy!"
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Show(string status = "Yükleniyor...")
    {
        loadingPanel.SetActive(true);
        statusText.text = status;
        tipText.text = tips[Random.Range(0, tips.Length)];
        progressBar.value = 0;
    }

    public void UpdateProgress(float progress, string status = null)
    {
        progressBar.value = progress;
        if (status != null) statusText.text = status;
    }

    public void Hide()
    {
        loadingPanel.SetActive(false);
    }
}
```

**Görevler:**
- [ ] Loading Screen Canvas prefab oluştur
- [ ] LoadingScreenUI.cs oluştur
- [ ] LobbyManager.OnGameStarting event'inde Show() çağır

---

### Adım 4: PlayerSpawnManager 🔴

**Süre:** 2-3 saat

**Dosya:** `Scripts/Player/PlayerSpawnManager.cs`

```csharp
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlayerSpawnManager : NetworkBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }

    [Header("Spawn Points")]
    [SerializeField] private Transform[] teamASpawnPoints;
    [SerializeField] private Transform[] teamBSpawnPoints;

    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab;

    private Dictionary<ulong, int> playerTeams = new Dictionary<ulong, int>();
    private int teamAIndex = 0;
    private int teamBIndex = 0;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        int team = AssignTeam(clientId);
        Vector3 spawnPos = GetSpawnPosition(team);
        
        SpawnPlayer(clientId, spawnPos, team);
    }

    private int AssignTeam(ulong clientId)
    {
        // Basit round-robin takım ataması
        int team = playerTeams.Count % 2;
        playerTeams[clientId] = team;
        return team;
    }

    private Vector3 GetSpawnPosition(int team)
    {
        Transform[] spawnPoints = team == 0 ? teamASpawnPoints : teamBSpawnPoints;
        int index = team == 0 ? teamAIndex++ : teamBIndex++;
        
        if (spawnPoints.Length == 0)
            return Vector3.zero;
            
        return spawnPoints[index % spawnPoints.Length].position;
    }

    private void SpawnPlayer(ulong clientId, Vector3 position, int team)
    {
        GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);
        NetworkObject netObj = player.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId);
        
        Debug.Log($"[SpawnManager] Player {clientId} spawned at Team {team}");
    }

    public int GetPlayerTeam(ulong clientId)
    {
        return playerTeams.TryGetValue(clientId, out int team) ? team : -1;
    }
}
```

**Görevler:**
- [ ] Game sahnesinde spawn noktaları oluştur (boş GameObject'ler)
- [ ] PlayerSpawnManager.cs oluştur
- [ ] Game sahnesine PlayerSpawnManager ekle
- [ ] NetworkManager'dan otomatik player spawn'u kapat
  - NetworkManager → Player Prefab = None
  - PlayerSpawnManager üzerinden spawn yap

---

### Adım 5: GameManager 🔴

**Süre:** 3-4 saat

**Dosya:** `Scripts/Game/GameManager.cs`

```csharp
using Unity.Netcode;
using UnityEngine;
using System;

public enum GameState
{
    WaitingForPlayers,
    Starting,
    DayPhase,
    NightPhase,
    GameOver
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private float startCountdown = 5f;
    [SerializeField] private float dayPhaseDuration = 120f;
    [SerializeField] private float nightPhaseDuration = 60f;

    public NetworkVariable<GameState> CurrentState = new NetworkVariable<GameState>(
        GameState.WaitingForPlayers,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> PhaseTimer = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> CurrentRound = new NetworkVariable<int>(1);

    public event Action<GameState> OnStateChanged;
    public event Action<float> OnTimerUpdated;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        CurrentState.OnValueChanged += (prev, current) =>
        {
            OnStateChanged?.Invoke(current);
            Debug.Log($"[GameManager] State: {prev} -> {current}");
        };

        if (IsServer)
        {
            CurrentState.Value = GameState.WaitingForPlayers;
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        switch (CurrentState.Value)
        {
            case GameState.WaitingForPlayers:
                CheckPlayersReady();
                break;
            case GameState.Starting:
                UpdateStartCountdown();
                break;
            case GameState.DayPhase:
            case GameState.NightPhase:
                UpdatePhaseTimer();
                break;
        }
    }

    private void CheckPlayersReady()
    {
        int playerCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
        if (playerCount >= minPlayersToStart)
        {
            StartCountdown();
        }
    }

    private void StartCountdown()
    {
        CurrentState.Value = GameState.Starting;
        PhaseTimer.Value = startCountdown;
    }

    private void UpdateStartCountdown()
    {
        PhaseTimer.Value -= Time.deltaTime;
        OnTimerUpdated?.Invoke(PhaseTimer.Value);

        if (PhaseTimer.Value <= 0)
        {
            StartDayPhase();
        }
    }

    private void StartDayPhase()
    {
        CurrentState.Value = GameState.DayPhase;
        PhaseTimer.Value = dayPhaseDuration;
        NotifyPhaseChangeClientRpc("day");
    }

    private void StartNightPhase()
    {
        CurrentState.Value = GameState.NightPhase;
        PhaseTimer.Value = nightPhaseDuration;
        NotifyPhaseChangeClientRpc("night");
    }

    private void UpdatePhaseTimer()
    {
        PhaseTimer.Value -= Time.deltaTime;

        if (PhaseTimer.Value <= 0)
        {
            if (CurrentState.Value == GameState.DayPhase)
            {
                StartNightPhase();
            }
            else
            {
                CurrentRound.Value++;
                StartDayPhase();
            }
        }
    }

    [ClientRpc]
    private void NotifyPhaseChangeClientRpc(string phase)
    {
        Debug.Log($"[GameManager] Phase changed to: {phase}");
        // UI güncellemesi, ses efekti vs.
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestEndGameServerRpc()
    {
        CurrentState.Value = GameState.GameOver;
    }
}
```

**Görevler:**
- [ ] `Scripts/Game/GameManager.cs` oluştur
- [ ] Game sahnesine GameManager ekle
- [ ] PhaseManager için temel yapı (GameManager içinde)

---

### Adım 6: Ready Sistem (Network Sync) 🟡

**Süre:** 2-3 saat

**Dosya:** `Scripts/Lobby/PlayerReadyManager.cs`

```csharp
using Unity.Netcode;
using System;
using System.Collections.Generic;

public class PlayerReadyManager : NetworkBehaviour
{
    public static PlayerReadyManager Instance { get; private set; }

    private NetworkList<ulong> readyPlayers;
    
    public event Action<int, int> OnReadyCountChanged; // (ready, total)
    public event Action OnAllPlayersReady;

    private void Awake()
    {
        Instance = this;
        readyPlayers = new NetworkList<ulong>();
    }

    public override void OnNetworkSpawn()
    {
        readyPlayers.OnListChanged += OnReadyListChanged;
    }

    private void OnReadyListChanged(NetworkListEvent<ulong> changeEvent)
    {
        int readyCount = readyPlayers.Count;
        int totalCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
        
        OnReadyCountChanged?.Invoke(readyCount, totalCount);

        if (IsServer && readyCount == totalCount && totalCount >= 2)
        {
            OnAllPlayersReady?.Invoke();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetReadyServerRpc(bool isReady, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (isReady && !readyPlayers.Contains(clientId))
        {
            readyPlayers.Add(clientId);
        }
        else if (!isReady && readyPlayers.Contains(clientId))
        {
            readyPlayers.Remove(clientId);
        }
    }

    public bool IsPlayerReady(ulong clientId)
    {
        return readyPlayers.Contains(clientId);
    }

    public void ResetAllReady()
    {
        if (!IsServer) return;
        readyPlayers.Clear();
    }
}
```

**Görevler:**
- [ ] PlayerReadyManager.cs oluştur
- [ ] LobbyRoomUI ready butonunu PlayerReadyManager ile entegre et
- [ ] Host için "All Ready" kontrolü ekle

---

### Adım 7: Team System 🟡

**Süre:** 2-3 saat

**Dosya:** `Scripts/Game/TeamManager.cs`

```csharp
using Unity.Netcode;
using System.Collections.Generic;
using System;

public class TeamManager : NetworkBehaviour
{
    public static TeamManager Instance { get; private set; }

    public const int TEAM_A = 0;
    public const int TEAM_B = 1;

    private NetworkList<ulong> teamAPlayers;
    private NetworkList<ulong> teamBPlayers;

    public event Action OnTeamsUpdated;

    private void Awake()
    {
        Instance = this;
        teamAPlayers = new NetworkList<ulong>();
        teamBPlayers = new NetworkList<ulong>();
    }

    public override void OnNetworkSpawn()
    {
        teamAPlayers.OnListChanged += _ => OnTeamsUpdated?.Invoke();
        teamBPlayers.OnListChanged += _ => OnTeamsUpdated?.Invoke();
    }

    public void AssignPlayerToTeam(ulong clientId, int team)
    {
        if (!IsServer) return;

        RemoveFromAllTeams(clientId);

        if (team == TEAM_A)
            teamAPlayers.Add(clientId);
        else
            teamBPlayers.Add(clientId);
    }

    public void AutoAssignPlayer(ulong clientId)
    {
        if (!IsServer) return;

        int team = teamAPlayers.Count <= teamBPlayers.Count ? TEAM_A : TEAM_B;
        AssignPlayerToTeam(clientId, team);
    }

    private void RemoveFromAllTeams(ulong clientId)
    {
        if (teamAPlayers.Contains(clientId))
            teamAPlayers.Remove(clientId);
        if (teamBPlayers.Contains(clientId))
            teamBPlayers.Remove(clientId);
    }

    public int GetPlayerTeam(ulong clientId)
    {
        if (teamAPlayers.Contains(clientId)) return TEAM_A;
        if (teamBPlayers.Contains(clientId)) return TEAM_B;
        return -1;
    }

    public List<ulong> GetTeamPlayers(int team)
    {
        NetworkList<ulong> source = team == TEAM_A ? teamAPlayers : teamBPlayers;
        List<ulong> result = new List<ulong>();
        foreach (var id in source) result.Add(id);
        return result;
    }

    public int GetTeamCount(int team)
    {
        return team == TEAM_A ? teamAPlayers.Count : teamBPlayers.Count;
    }
}
```

**Görevler:**
- [ ] TeamManager.cs oluştur
- [ ] PlayerSpawnManager ile entegre et
- [ ] Lobi UI'da takım gösterimi (opsiyonel)

---

### Adım 8: LobbyManager Güncellemesi 🔴

**Güncelleme:** `Scripts/Lobby/LobbyManager.cs`

StartGame metodunu güncelle:

```csharp
public async Task<bool> StartGame()
{
    if (!IsHost)
    {
        Debug.LogError("[LobbyManager] Only host can start the game!");
        return false;
    }

    try
    {
        // Loading screen göster
        LoadingScreenUI.Instance?.Show("Oyun hazırlanıyor...");

        string relayCode = await RelayManager.Instance.CreateRelay(hostLobby.MaxPlayers - 1);
        if (string.IsNullOrEmpty(relayCode))
        {
            LoadingScreenUI.Instance?.Hide();
            Debug.LogError("[LobbyManager] Failed to create relay!");
            return false;
        }

        LoadingScreenUI.Instance?.UpdateProgress(0.3f, "Relay bağlantısı kuruldu");

        await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>
            {
                { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
            }
        });

        LoadingScreenUI.Instance?.UpdateProgress(0.5f, "Lobi güncellendi");

        NetworkManager.Singleton.StartHost();
        
        LoadingScreenUI.Instance?.UpdateProgress(0.7f, "Host başlatıldı");

        // Sahne geçişi
        SceneController.Instance?.LoadGameScene();

        LoadingScreenUI.Instance?.UpdateProgress(1f, "Oyun başlıyor!");
        
        OnGameStarting?.Invoke();
        Debug.Log("[LobbyManager] Game started as HOST!");

        return true;
    }
    catch (Exception e)
    {
        LoadingScreenUI.Instance?.Hide();
        Debug.LogError($"[LobbyManager] Start game failed: {e.Message}");
        return false;
    }
}
```

---

## 📁 Yeni Klasör Yapısı

```
Assets/Scripts/
├── Game/                    ❌ YENİ KLASÖR
│   ├── GameManager.cs       ❌ YENİ
│   ├── SceneController.cs   ❌ YENİ
│   └── TeamManager.cs       ❌ YENİ
│
├── Lobby/
│   ├── LobbyManager.cs      ✅ GÜNCELLE
│   ├── PlayerReadyManager.cs❌ YENİ
│   └── TestLobby.cs         ✅ Mevcut
│
├── Network/
│   ├── ConnectionManager.cs ✅ GÜNCELLE
│   └── RelayManager.cs      ✅ Mevcut
│
├── Player/
│   ├── PlayerNetwork.cs     ✅ Mevcut
│   ├── PlayerSpawnManager.cs❌ YENİ
│   └── OwnerNetworkAnimator.cs ✅ Mevcut
│
└── UI/
    ├── LoadingScreenUI.cs   ❌ YENİ
    └── [mevcut UI dosyaları] ✅

Assets/Prefabs/
└── UI/
    └── LoadingScreen.prefab ❌ YENİ

Assets/Scenes/
├── MainMenu.unity           ✅ Mevcut
└── Game.unity               ❌ YENİ
```

---

## ⏱️ Tahmini Süre

| Adım | Görev | Süre | Öncelik |
|------|-------|------|---------|
| 1 | Sahne Yapısı | 1-2 saat | 🔴 |
| 2 | SceneController | 2-3 saat | 🔴 |
| 3 | Loading Screen | 1-2 saat | 🔴 |
| 4 | PlayerSpawnManager | 2-3 saat | 🔴 |
| 5 | GameManager | 3-4 saat | 🔴 |
| 6 | Ready System | 2-3 saat | 🟡 |
| 7 | Team System | 2-3 saat | 🟡 |
| 8 | LobbyManager Update | 1 saat | 🔴 |
| **Toplam** | | **14-21 saat** | |

---

## ✅ Checklist

### Kritik (🔴)
- [ ] Game.unity sahnesi oluşturuldu
- [ ] Build Settings güncellendi
- [ ] SceneController.cs oluşturuldu ve test edildi
- [ ] LoadingScreenUI.cs ve prefab oluşturuldu
- [ ] PlayerSpawnManager.cs oluşturuldu
- [ ] Spawn noktaları sahnede yerleştirildi
- [ ] GameManager.cs oluşturuldu
- [ ] LobbyManager.StartGame() güncellendi
- [ ] Sahne geçişi test edildi (Host + Client)

### Orta (🟡)
- [ ] PlayerReadyManager.cs oluşturuldu
- [ ] LobbyRoomUI ready sistemi entegre edildi
- [ ] TeamManager.cs oluşturuldu
- [ ] Takım ataması test edildi

### Düşük (🟢)
- [ ] Loading screen animasyonları
- [ ] Phase geçiş efektleri
- [ ] Takım seçim UI

---

## 🧪 Test Senaryoları

### Senaryo 1: Temel Akış
```
1. Host: MainMenu → Create Lobby → LobbyRoom
2. Client: MainMenu → Join Lobby → LobbyRoom
3. Host: Start Game butonuna bas
4. Her iki taraf: Loading screen görünsün
5. Her iki taraf: Game sahnesine geçsin
6. Her iki taraf: Doğru spawn noktasında spawn olsun
```

### Senaryo 2: Bağlantı Kopması
```
1. Oyun başladıktan sonra client bağlantısını kes
2. Host'ta client'ın düştüğü görülsün
3. Client yeniden bağlanabilsin (opsiyonel)
```

### Senaryo 3: Phase Geçişi
```
1. GameManager DayPhase başlatsın
2. Timer bitince NightPhase'e geçsin
3. Tüm client'larda senkronize olsun
```

---

## 🔗 Bağımlılıklar

```
┌─────────────────┐
│  LobbyManager   │
│                 │
│ StartGame()     │───────┐
└─────────────────┘       │
                          ▼
┌─────────────────┐  ┌─────────────────┐
│ LoadingScreenUI │  │ SceneController │
│                 │  │                 │
│ Show/Hide       │  │ LoadGameScene() │
└─────────────────┘  └────────┬────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │  Game.unity     │
                    │                 │
                    │ • GameManager   │
                    │ • SpawnManager  │
                    │ • TeamManager   │
                    └─────────────────┘
```

---

## ⚠️ Dikkat Edilecekler

| Konu | Açıklama |
|------|----------|
| **DontDestroyOnLoad** | NetworkManager, LobbyManager, RelayManager, LoadingScreenUI |
| **Sahne Senkronizasyonu** | `NetworkManager.SceneManager.LoadScene()` kullan |
| **Spawn Timing** | Player spawn OnClientConnected içinde yap |
| **IsServer Kontrolü** | Tüm authoritative işlemler IsServer kontrolü ile |
| **NetworkList** | Ready ve Team listelerinde NetworkList kullan |

---

## 🚀 Başlangıç Noktası

İlk olarak şu sırayla başla:

```
1. Assets/Scenes/Game.unity oluştur
2. Scripts/Game/ klasörü oluştur
3. SceneController.cs yaz
4. Test et: Lobi → Game geçişi çalışıyor mu?
5. PlayerSpawnManager.cs yaz
6. Test et: Oyuncular spawn oluyor mu?
7. GameManager.cs yaz
8. Test et: State değişimleri senkronize mi?
```

---

*Son Güncelleme: 13 Aralık 2025*  
*Versiyon: 1.0 - Faz 3 Başlangıç*

