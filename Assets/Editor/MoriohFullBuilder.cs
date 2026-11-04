using System;
using System.IO;
using ActiveMotion3D.EndlessRunner.Environment;
using ActiveMotion3D.EndlessRunner.Obstacles;
using ActiveMotion3D.EndlessRunner.Track;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

/// <summary>Editor-only authoring of the isolated full Morioh scene.</summary>
public static class MoriohFullBuilder
{
    private const string PhaseScene = "Assets/ActiveMotion3D/Scenes/AM3D_Morioh_Phase1.unity";
    private const string FullScene = "Assets/ActiveMotion3D/Scenes/AM3D_Morioh_Full.unity";
    private const string StreetFolder = "Assets/ActiveMotion3D/Art/Environment/Morioh";
    private const string ObstacleFolder = "Assets/ActiveMotion3D/Art/Obstacles/Morioh";
    private const string ConfigFolder = "Assets/ActiveMotion3D/Art/Config";
    private const string ConfigPath = ConfigFolder + "/MoriohThemeConfig.asset";
    private const string StreetA = StreetFolder + "/Morioh_Street_20m.prefab";

    [MenuItem("Tools/Morioh/Build Full Scene")]
    public static void BuildFullScene()
    {
        if (File.Exists(FullScene) || File.Exists(ConfigPath))
            throw new InvalidOperationException("Full Morioh outputs already exist; refusing to overwrite them.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PhaseScene) == null)
            throw new InvalidOperationException("Phase 1 scene is missing.");

        EnsureFolder(ConfigFolder);
        Material mint = MaterialAt("Morioh_Mint");
        Material peach = MaterialAt("Morioh_Peach");
        Material lavender = MaterialAt("Morioh_Lavender");
        Material purple = MaterialAt("Morioh_PurpleVegetation");
        Material gold = MaterialAt("Morioh_Gold");
        Material ink = MaterialAt("Morioh_Ink");
        Material cyan = Require<Material>("Assets/ActiveMotion3D/Materials/AM3D_Cyan.mat");
        Material magenta = Require<Material>("Assets/ActiveMotion3D/Materials/AM3D_Magenta.mat");

        GameObject a = Require<GameObject>(StreetA);
        GameObject b = CreateStreetVariant("Morioh_Street_20m_B", mint, lavender, peach, "QUIET STREET", "RADIO", "MORIOH 4", "STARDUST");
        GameObject c = CreateStreetVariant("Morioh_Street_20m_C", peach, mint, lavender, "VIOLET WAY", "ODD HOURS", "MORIOH", "CROSSROAD");
        GameObject d = CreateStreetVariant("Morioh_Street_20m_D", lavender, peach, mint, "GOLDEN HOUR", "MORIOH", "RADIO", "SIDE STREET");

        GameObject high = CreateHighBarrier(cyan, magenta, gold, ink);
        GameObject full = CreateFullBarrier(cyan, magenta, gold, ink);
        GameObject coin = CreateCoinVisual(gold, lavender, ink);

        MoriohThemeConfig config = ScriptableObject.CreateInstance<MoriohThemeConfig>();
        config.streetModules = new[] { a, b, c, d };
        config.lowBarrierVisual = Require<GameObject>(ObstacleFolder + "/Morioh_LowBarrier_Visual.prefab");
        config.highBarrierVisual = high;
        config.fullBarrierVisual = full;
        config.coinVisual = coin;
        config.palette = new[] { mint, peach, lavender, purple, gold, ink };
        SetTwilightDefaults(config);
        AssetDatabase.CreateAsset(config, ConfigPath);
        AssetDatabase.SaveAssets();

        if (!AssetDatabase.CopyAsset(PhaseScene, FullScene))
            throw new InvalidOperationException("Could not copy Phase 1 scene.");
        AssetDatabase.Refresh();
        ApplyThemeToFullScene();
        Debug.Log("Full Morioh scene created: " + FullScene);
    }

    [MenuItem("Tools/Morioh/Apply Config To Full Scene")]
    public static void ApplyThemeToFullScene()
    {
        MoriohThemeConfig config = Require<MoriohThemeConfig>(ConfigPath);
        if (config.streetModules == null || config.streetModules.Length == 0)
            throw new InvalidOperationException("Assign street modules in MoriohThemeConfig.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FullScene) == null)
            throw new InvalidOperationException("Full Morioh scene is missing.");

        Scene previousActive = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(FullScene);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(FullScene, OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            RenderSettings.fog = config.fogEnabled;
            RenderSettings.fogColor = config.fogColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = config.fogDensity;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = config.ambientSky;
            RenderSettings.ambientEquatorColor = config.ambientEquator;
            RenderSettings.ambientGroundColor = config.ambientGround;
            GameObject cameraRoot = FindRoot(scene, "Main Camera");
            Camera camera = cameraRoot.GetComponent<Camera>();
            camera.clearFlags = config.skyboxMaterial != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            camera.backgroundColor = config.cameraBackground;
            RenderSettings.skybox = config.skyboxMaterial;
            ApplyHorizon(scene, config, camera);
            Light sun = FindRoot(scene, "Directional Light").GetComponent<Light>();
            sun.color = config.sunColor;
            sun.intensity = config.sunIntensity;

            Transform track = FindRoot(scene, "TrackManager").transform;
            string[] oldNames = { "Road", "Ground", "LaneL", "LaneR", "EdgeL", "EdgeR", "BuildingL", "BuildingR" };
            for (int i = 0; i < 8; i++)
            {
                Transform segment = track.Find("TrackSegment_" + i.ToString("00"));
                if (segment == null || segment.GetComponent<TrackSegment>() == null)
                    throw new InvalidOperationException("Missing authored TrackSegment_" + i.ToString("00"));
                foreach (string name in oldNames)
                {
                    Transform child = segment.Find(name);
                    if (child == null) throw new InvalidOperationException("Missing original child " + name);
                    Renderer renderer = child.GetComponent<Renderer>();
                    if (renderer != null) renderer.enabled = false;
                }
                GameObject desired = config.streetModules[i % config.streetModules.Length];
                if (desired == null) throw new InvalidOperationException("Street module " + i + " is null.");
                Transform current = null;
                foreach (Transform child in segment)
                {
                    if (child.name.StartsWith("Morioh_Street_20m", StringComparison.Ordinal))
                    {
                        current = child;
                        break;
                    }
                }
                if (current != null && PrefabUtility.GetCorrespondingObjectFromSource(current.gameObject) == desired)
                    continue;
                if (current != null) UnityEngine.Object.DestroyImmediate(current.gameObject);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(desired, scene);
                instance.transform.SetParent(segment, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
            }

            ObstacleSpawner spawner = FindRoot(scene, "ObstacleSpawner").GetComponent<ObstacleSpawner>();
            SerializedObject serialized = new SerializedObject(spawner);
            SetVisual(serialized, "lowBarrierVisual", config.lowBarrierVisual);
            SetVisual(serialized, "highBarrierVisual", config.highBarrierVisual);
            SetVisual(serialized, "fullBarrierVisual", config.fullBarrierVisual);
            SetVisual(serialized, "coinVisual", config.coinVisual);
            SetVisual(serialized, "punchTargetVisual", config.punchTargetVisual);
            SetVisual(serialized, "activityArt", config.activityArt);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            if (previousActive.IsValid() && previousActive.isLoaded)
                SceneManager.SetActiveScene(previousActive);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }
    }

    [MenuItem("Tools/Morioh/Set Twilight Atmosphere")]
    public static void SetTwilightAtmosphere()
    {
        MoriohThemeConfig config = Require<MoriohThemeConfig>(ConfigPath);
        SetTwilightDefaults(config);
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        ApplyThemeToFullScene();
    }

    private static void SetTwilightDefaults(MoriohThemeConfig config)
    {
        config.cameraBackground = new Color(0.43f, 0.43f, 0.65f);
        config.fogEnabled = true;
        config.fogColor = new Color(0.45f, 0.39f, 0.57f);
        config.fogDensity = 0.008f;
        config.ambientSky = new Color(0.64f, 0.61f, 0.76f);
        config.ambientEquator = new Color(0.44f, 0.39f, 0.55f);
        config.ambientGround = new Color(0.22f, 0.19f, 0.32f);
        config.sunColor = new Color(1f, 0.83f, 0.68f);
        config.sunIntensity = 1.25f;
    }

    [MenuItem("Tools/Morioh/Fit Street Signs")]
    public static void FitStreetSigns()
    {
        string[] paths =
        {
            StreetA,
            StreetFolder + "/Morioh_Street_20m_B.prefab",
            StreetFolder + "/Morioh_Street_20m_C.prefab",
            StreetFolder + "/Morioh_Street_20m_D.prefab"
        };
        foreach (string path in paths)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (TextMesh label in root.GetComponentsInChildren<TextMesh>(true))
                    label.characterSize = 0.15f;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
    }

    private static GameObject CreateStreetVariant(string name, Material left, Material right, Material second,
        string sign1, string sign2, string sign3, string sign4)
    {
        string path = StreetFolder + "/" + name + ".prefab";
        if (File.Exists(path)) throw new InvalidOperationException("Variant already exists: " + path);
        GameObject root = PrefabUtility.LoadPrefabContents(StreetA);
        try
        {
            root.name = name;
            SetFacade(root, "LeftStreetscape/MintFacade", left, 6.4f);
            SetFacade(root, "LeftStreetscape/PeachFacade", second, 7.2f);
            SetFacade(root, "RightStreetscape/LavenderFacade", right, 5.8f);
            SetFacade(root, "RightStreetscape/MintFacade", left, 6.8f);
            SetMaterial(root, "SidewalkL", right);
            SetMaterial(root, "SidewalkR", left);
            SetSign(root, "LeftStreetscape/Sign_MORIOH", sign1, second);
            SetSign(root, "LeftStreetscape/Sign_ODD_HOURS", sign2, right);
            SetSign(root, "RightStreetscape/Sign_ROUTE_4", sign3, left);
            SetSign(root, "RightStreetscape/Sign_DOWNTOWN", sign4, second);
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void SetFacade(GameObject root, string path, Material material, float height)
    {
        Transform facade = root.transform.Find(path);
        if (facade == null) throw new InvalidOperationException("Missing facade " + path);
        Vector3 scale = facade.localScale;
        scale.y = height;
        facade.localScale = scale;
        Vector3 position = facade.localPosition;
        position.y = height * 0.5f;
        facade.localPosition = position;
        facade.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void SetSign(GameObject root, string path, string label, Material panelMaterial)
    {
        Transform sign = root.transform.Find(path);
        if (sign == null) throw new InvalidOperationException("Missing sign " + path);
        sign.Find("Panel").GetComponent<Renderer>().sharedMaterial = panelMaterial;
        sign.Find("Label").GetComponent<TextMesh>().text = label;
    }

    private static void SetMaterial(GameObject root, string path, Material material)
    {
        Transform child = root.transform.Find(path);
        if (child == null) throw new InvalidOperationException("Missing street child " + path);
        child.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static GameObject CreateHighBarrier(Material cyan, Material magenta, Material gold, Material ink)
    {
        string path = ObstacleFolder + "/Morioh_HighBarrier_Visual.prefab";
        GameObject root = new GameObject("Morioh_HighBarrier_Visual");
        try
        {
            // Normalized X: ObstacleSpawner expands Visual to 10.1 units.
            Cube("InkCanopy", root.transform, Vector3.zero, new Vector3(1, 0.78f, 0.28f), ink);
            Cube("LavenderBand", root.transform, new Vector3(0, 0, -0.16f), new Vector3(0.98f, 0.58f, 0.04f), magenta);
            Cube("GoldenTop", root.transform, new Vector3(0, 0.43f, -0.01f), new Vector3(1, 0.1f, 0.3f), gold);
            Cube("CyanBottom", root.transform, new Vector3(0, -0.43f, -0.01f), new Vector3(1, 0.1f, 0.3f), cyan);
            for (int i = -4; i <= 4; i++)
            {
                GameObject stripe = Cube("ComicStripe_" + (i + 4), root.transform,
                    new Vector3(i * 0.11f, 0, -0.19f), new Vector3(0.018f, 0.57f, 0.03f), gold);
                stripe.transform.localRotation = Quaternion.Euler(0, 0, 25);
            }
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static GameObject CreateFullBarrier(Material cyan, Material magenta, Material gold, Material ink)
    {
        string path = ObstacleFolder + "/Morioh_FullBarrier_Visual.prefab";
        GameObject root = new GameObject("Morioh_FullBarrier_Visual");
        try
        {
            // Normalized X: ObstacleSpawner expands Visual to 2.56 units.
            Cube("InkFrame", root.transform, Vector3.zero, new Vector3(1, 3.9f, 0.27f), ink);
            Cube("PurplePanel", root.transform, new Vector3(0, 0, -0.15f), new Vector3(0.91f, 3.63f, 0.04f), magenta);
            Cube("CyanHeader", root.transform, new Vector3(0, 1.54f, -0.18f), new Vector3(0.91f, 0.42f, 0.05f), cyan);
            Cube("GoldBase", root.transform, new Vector3(0, -1.57f, -0.18f), new Vector3(0.91f, 0.28f, 0.05f), gold);
            for (int i = 0; i < 3; i++)
            {
                GameObject chevron = Cube("GraphicChevron_" + i, root.transform,
                    new Vector3(0, 0.65f - i * 0.8f, -0.2f), new Vector3(0.14f, 0.65f, 0.035f), gold);
                chevron.transform.localRotation = Quaternion.Euler(0, 0, 35);
            }
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static GameObject CreateCoinVisual(Material gold, Material lavender, Material ink)
    {
        string path = ObstacleFolder + "/Morioh_Coin_Visual.prefab";
        GameObject root = new GameObject("Morioh_Coin_Visual");
        try
        {
            Cylinder("InkRim", root.transform, Vector3.zero, new Vector3(0.72f, 0.05f, 0.72f), ink);
            Cylinder("GoldFace", root.transform, new Vector3(0.01f, 0, 0), new Vector3(0.61f, 0.055f, 0.61f), gold);
            GameObject gem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gem.name = "LavenderCenter";
            gem.transform.SetParent(root.transform, false);
            gem.transform.localPosition = new Vector3(0.07f, 0, 0);
            gem.transform.localScale = new Vector3(0.12f, 0.16f, 0.16f);
            UnityEngine.Object.DestroyImmediate(gem.GetComponent<Collider>());
            gem.GetComponent<Renderer>().sharedMaterial = lavender;
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }

    private static void Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localRotation = Quaternion.Euler(0, 0, 90);
        obj.transform.localScale = scale;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        obj.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Material MaterialAt(string name) => Require<Material>("Assets/ActiveMotion3D/Materials/Morioh/" + name + ".mat");

    private static T Require<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException("Missing asset: " + path);
        return asset;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
        throw new InvalidOperationException("Missing scene root: " + name);
    }

    private static void SetVisual(SerializedObject serialized, string name, UnityEngine.Object prefab)
    {
        SerializedProperty field = serialized.FindProperty(name);
        if (field == null) throw new InvalidOperationException("Missing spawner field: " + name);
        field.objectReferenceValue = prefab;
    }

    private static void ApplyHorizon(Scene scene, MoriohThemeConfig config, Camera camera)
    {
        GameObject current = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "Morioh_Horizon") current = root;
        if (config.horizonPrefab == null)
        {
            if (current != null) UnityEngine.Object.DestroyImmediate(current);
            return;
        }
        if (current != null && PrefabUtility.GetCorrespondingObjectFromSource(current) != config.horizonPrefab)
        {
            UnityEngine.Object.DestroyImmediate(current);
            current = null;
        }
        if (current == null) current = (GameObject)PrefabUtility.InstantiatePrefab(config.horizonPrefab, scene);
        current.name = "Morioh_Horizon";
        current.transform.position = config.horizonOffset;
        current.transform.localScale = config.horizonScale;
        MoriohHorizonFollower follower = current.GetComponent<MoriohHorizonFollower>();
        if (follower != null)
        {
            follower.target = camera.transform;
            follower.offset = config.horizonOffset;
            follower.followHorizontalCameraPosition = config.horizonFollowsCameraXZ;
        }
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
