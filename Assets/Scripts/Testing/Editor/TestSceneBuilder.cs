#if UNITY_EDITOR
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor utility: creates Test_Core.unity scene with all core managers wired up.
/// Menu: Tools > Testing > Create Test_Core Scene
/// </summary>
public static class TestSceneBuilder
{
    private const string SCENE_PATH = "Assets/Scenes/Tests/Test_Core.unity";
    private const string PHASE_SETTINGS_PATH = "Assets/Data/Settings/PhaseSettings.asset";
    private const string DAY_SETTINGS_PATH = "Assets/Data/Settings/DayPhaseSettings.asset";
    private const string PLAYER_PREFAB_PATH = "Assets/Prefabs/Player/Player.prefab";

    [MenuItem("Tools/Testing/Create Test_Core Scene")]
    public static void CreateTestCoreScene()
    {
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/Tests"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            AssetDatabase.CreateFolder("Assets/Scenes", "Tests");
        }

        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Main Camera
        var cameraGo = new GameObject("Main Camera");
        var cam = cameraGo.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cameraGo.transform.position = new Vector3(0f, 10f, -10f);
        cameraGo.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
        cameraGo.AddComponent<AudioListener>();

        // 2. Directional Light
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // 3. Ground Plane
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5f, 1f, 5f);

        // 4. NetworkManager
        var nmGo = new GameObject("NetworkManager");
        var nm = nmGo.AddComponent<NetworkManager>();
        nmGo.AddComponent<UnityTransport>();

        // Assign player prefab
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH);
        if (playerPrefab != null)
        {
            nm.NetworkConfig.PlayerPrefab = playerPrefab;
            Debug.Log($"[TestSceneBuilder] Player prefab assigned: {PLAYER_PREFAB_PATH}");
        }
        else
        {
            Debug.LogWarning($"[TestSceneBuilder] Player prefab not found at {PLAYER_PREFAB_PATH}");
        }

        // 5. GameManager
        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        var phaseSettings = AssetDatabase.LoadAssetAtPath<PhaseSettingsSO>(PHASE_SETTINGS_PATH);
        if (phaseSettings != null)
        {
            var so = new SerializedObject(gm);
            so.FindProperty("settings").objectReferenceValue = phaseSettings;
            so.ApplyModifiedProperties();
            Debug.Log("[TestSceneBuilder] PhaseSettingsSO assigned to GameManager");
        }
        else
        {
            Debug.LogWarning($"[TestSceneBuilder] PhaseSettingsSO not found at {PHASE_SETTINGS_PATH}");
        }

        // 6. TeamManager
        var tmGo = new GameObject("TeamManager");
        tmGo.AddComponent<TeamManager>();

        // 7. EconomyManager
        var emGo = new GameObject("EconomyManager");
        var em = emGo.AddComponent<EconomyManager>();
        var daySettings = AssetDatabase.LoadAssetAtPath<DayPhaseSettingsSO>(DAY_SETTINGS_PATH);
        if (daySettings != null)
        {
            var so = new SerializedObject(em);
            so.FindProperty("settings").objectReferenceValue = daySettings;
            so.ApplyModifiedProperties();
            Debug.Log("[TestSceneBuilder] DayPhaseSettingsSO assigned to EconomyManager");
        }
        else
        {
            Debug.LogWarning($"[TestSceneBuilder] DayPhaseSettingsSO not found at {DAY_SETTINGS_PATH}");
        }

        // 8. PlayerSpawnManager with spawn points
        var psmGo = new GameObject("PlayerSpawnManager");
        var psm = psmGo.AddComponent<PlayerSpawnManager>();

        var spawnA = new GameObject("SpawnA_1");
        spawnA.transform.SetParent(psmGo.transform);
        spawnA.transform.position = Vector3.zero;

        var spawnB = new GameObject("SpawnB_1");
        spawnB.transform.SetParent(psmGo.transform);
        spawnB.transform.position = new Vector3(5f, 0f, 0f);

        var psmSo = new SerializedObject(psm);
        var teamAPoints = psmSo.FindProperty("teamASpawnPoints");
        teamAPoints.arraySize = 1;
        teamAPoints.GetArrayElementAtIndex(0).objectReferenceValue = spawnA.transform;

        var teamBPoints = psmSo.FindProperty("teamBSpawnPoints");
        teamBPoints.arraySize = 1;
        teamBPoints.GetArrayElementAtIndex(0).objectReferenceValue = spawnB.transform;

        if (playerPrefab != null)
        {
            psmSo.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
        }
        psmSo.ApplyModifiedProperties();

        // 9. TestBootstrap
        var tbGo = new GameObject("TestBootstrap");
        tbGo.AddComponent<TestBootstrap>();

        // 10. DebugPanel
        var dpGo = new GameObject("DebugPanel");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        dpGo.AddComponent<DebugPanel>();
#endif

        // Save scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        AssetDatabase.Refresh();

        Debug.Log($"[TestSceneBuilder] Test_Core scene created at {SCENE_PATH}");
        EditorUtility.DisplayDialog("Test Scene Builder", $"Test_Core scene created at:\n{SCENE_PATH}", "OK");
    }
}
#endif
