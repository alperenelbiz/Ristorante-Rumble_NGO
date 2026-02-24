# PR #4 Review — Faz 0-3: Lobby, Game Flow ve Day Phase Core Loop

**+10,619 / -16,409** across lobby, game flow, day phase systems.

---

## CRITICAL (5)

### C1. `EconomyManager.cs` — Event leak, no unsubscribe
Anonymous lambdas on `OnValueChanged` in `OnNetworkSpawn`, but **no `OnNetworkDespawn`** to clean up. Can't unsubscribe anonymous lambdas → memory leak + stale callbacks.

### C2. `GameManager.cs` — PhaseTimer written every frame
`PhaseTimer.Value -= Time.deltaTime` runs in `Update` with **no throttling** (~60 NetworkVariable writes/sec). `CookingStation` throttles at 10Hz, but the timer does not. Excessive network traffic.

### C3. `GameManager.cs` — Timer event never reaches clients
`GameEvents.PhaseTimerUpdated()` fires inside `if (!IsServer) return` guard. `DayPhaseHUD` subscribes to this event → **non-host clients never see the timer update**. Need to fire from `PhaseTimer.OnValueChanged` callback instead.

### C4. `DayPhaseHUD.cs` — Self-disables permanently
`OnGameStateChanged` calls `gameObject.SetActive(false)` → triggers `OnDisable` → **unsubscribes all events**. When DayPhase starts again, no event will re-enable it. Toggle a child panel instead, or subscribe in `Awake`/`OnDestroy`.

### C5. `PlayerInteraction.cs` — Carry state race condition
Client reads `CarriedItem.Value` to gate interactions, but value only updates after server RPC round-trip. Rapid interactions can send duplicate RPCs before state reflects.

---

## WARNING (10)

| # | File | Issue |
|---|------|-------|
| W1 | Multiple singletons | No duplicate protection — `CameraManager`, `EconomyManager`, `RestaurantManager` etc. all use bare `Instance = this` |
| W2 | `CameraManager.SetFollowTarget` | No null check on `target` → NRE on `target.name` |
| W3 | `SceneController.cs` | `OnEnable` + `SubscribeToSceneEvents()` both subscribe to `OnLoadComplete` → possible double fire |
| W4 | `CookingStation.cs` | Cook progress drifts between syncs — reads old synced base each frame instead of accumulating locally |
| W5 | `PlayerInteraction.cs` RPCs | **No team validation** — Team B player can cook on Team A's station |
| W6 | `PickUpIngredientRpc` | No proximity/range check, no bounds check on `ingredientIndex` — server blindly trusts client |
| W7 | `GameManager.EndRound` | Jumps straight to `StartDayPhase()` without `DayPhaseCleanup()` or Transition. `RoundEnd`/`GameOver` states unused |
| W8 | `RecipeSelectUI.cs` | No null checks on `LocalClient.PlayerObject.GetComponent<>` chain |
| W9 | `TeamManager.cs` | Same anonymous lambda leak as C1 — `OnListChanged` subscribed, never unsubscribed |
| W10 | `CookingProgressUI` + `DayPhaseHUD` | `Camera.main` called every frame in Update (does `FindObjectWithTag` internally) — cache it |

---

## NIT (7)

- **Magic numbers** — Status `0,1,2,3` and ItemType `0,1,2` scattered across files. Use enum or byte constants
- **SO fields** — `IngredientSO`/`RecipeSO` use `public` fields vs `[SerializeField] private` convention
- **Mixed input** — `RecipeSelectUI` uses legacy `Input.GetKeyDown(Escape)` while rest uses new Input System
- **`Table.IsOccupied`** — plain C# property, will need `NetworkVariable` when customer system integrates
- **Array exposure** — `Restaurant.CookingStations` returns raw array reference
- **Fragile singletons** — No `if (Instance != null) Destroy(gameObject)` guard on most managers

---

## Priority Fix Order

1. **C3 + C4** — most user-visible (timer broken for clients, HUD dies permanently)
2. **C1 + W9** — event leaks (store delegates in fields, unsubscribe in `OnNetworkDespawn`)
3. **C2** — throttle `PhaseTimer` writes (match `CookingStation`'s 10Hz pattern)
4. **W5 + W6** — server-side validation (team check, proximity check) before it becomes tech debt
