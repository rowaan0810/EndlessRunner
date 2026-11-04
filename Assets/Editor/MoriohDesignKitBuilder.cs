using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ActiveMotion3D.EndlessRunner.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Explicit authoring commands. Never runs on import, startup, or entering Play.</summary>
public static class MoriohDesignKitBuilder
{
    private const string Kit = "Assets/ActiveMotion3D/Art/Environment/Morioh/DesignKit";
    private const string Source = Kit + "/Source";
    private const string Generated = Kit + "/Generated";
    private const string ThemePath = Generated + "/MoriohDesignTheme.asset";
    private const string LiveTheme = "Assets/ActiveMotion3D/Art/Config/MoriohThemeConfig.asset";
    private const string FullScene = "Assets/ActiveMotion3D/Scenes/AM3D_Morioh_Full.unity";
    private const string ExistingMaterials = "Assets/ActiveMotion3D/Materials/Morioh/";
    private static readonly string[] ModuleNames = { "ResidentialLane", "ShoppingCorner", "CafeAndBakery", "ClockPlaza" };

    [Serializable] private sealed class SourceIndex { public ModelInfo[] models; }
    [Serializable] private sealed class ModelInfo { public string name; }

    [MenuItem("Tools/Morioh/Design Kit/1 - Create Editable Art Assets")]
    public static void BuildArt()
    {
        RequireEditMode();
        if (AssetDatabase.LoadAssetAtPath<MoriohThemeConfig>(ThemePath) != null)
            throw new InvalidOperationException("Design assets already exist. Edit their layouts/materials, then use Rebuild Modules.");
        if (Directory.Exists(Generated))
            throw new InvalidOperationException("Generated folder already exists. Inspect any partial build before retrying.");
        SourceIndex index = JsonUtility.FromJson<SourceIndex>(File.ReadAllText(Source + "/manifest.json"));
        if (index?.models == null || index.models.Length != 32)
            throw new InvalidOperationException("Expected the complete 32-model source kit.");
        foreach (ModelInfo model in index.models)
            if (!File.Exists(Source + "/Models/" + model.name + ".obj"))
                throw new FileNotFoundException(model.name);
        Font font = Require<Font>("Assets/ActiveMotion3D/Art/FontEX/Bevan-Regular.ttf");
        Require<Shader>(Kit + "/LavenderCloudSky.shader");
        Require<Shader>(Kit + "/HorizonInk.shader");
        Require<MoriohThemeConfig>(LiveTheme);
        foreach (string sub in new[] { "Materials", "Models", "Modules", "Layouts", "Activities" })
            EnsureFolder(Generated + "/" + sub);

        Dictionary<string, Material> palette = CreatePalette();
        var models = new Dictionary<string, GameObject>();
        foreach (ModelInfo info in index.models)
            models.Add(info.name, CreateModelPrefab(info.name, palette));

        Material sky = CreateSky();
        MoriohActivityArt activities = ScriptableObject.CreateInstance<MoriohActivityArt>();
        activities.name = "MoriohActivityArt";
        activities.gateDecoration = models["PoseGateFrame"];
        activities.waitingSilhouette = palette["Gold"];
        activities.readySilhouette = CreateMaterial("PoseReady", new Color(.64f, .94f, .72f), .12f);
        activities.openingBacking = palette["Ink"];
        activities.instructionFont = font;
        activities.punchHitEffect = CreateBurst("PunchHit", "HIT!", palette["Gold"], font);
        activities.coinPickupEffect = CreateBurst("CoinPickup", "+1", palette["Gold"], font);
        AssetDatabase.CreateAsset(activities, Generated + "/Activities/MoriohActivityArt.asset");
        GameObject punch = CreatePunchVisual(models["HighSideCrest"], activities, palette, font);
        GameObject horizon = CreateHorizon(models["ContinuousHorizon"]);

        GameObject[] modules = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            MoriohModuleLayout layout = CreateLayout(i, models);
            AssetDatabase.CreateAsset(layout, Generated + "/Layouts/" + ModuleNames[i] + ".asset");
            modules[i] = BuildModule(i, layout);
        }
        MoriohThemeConfig theme = UnityEngine.Object.Instantiate(Require<MoriohThemeConfig>(LiveTheme));
        theme.name = "MoriohDesignTheme";
        theme.streetModules = modules;
        theme.lowBarrierVisual = models["JumpPromenadeRail"];
        theme.highBarrierVisual = models["DuckShopCanopy"];
        theme.fullBarrierVisual = models["DodgeDeliveryScreen"];
        theme.coinVisual = models["MoriohCrestCoin"];
        theme.punchTargetVisual = punch;
        theme.activityArt = activities;
        theme.skyboxMaterial = sky;
        theme.horizonPrefab = horizon;
        theme.horizonOffset = Vector3.zero;
        theme.horizonScale = Vector3.one;
        theme.horizonFollowsCameraXZ = true;
        theme.cameraBackground = new Color(.58f, .52f, .67f);
        theme.fogColor = new Color(.66f, .59f, .71f);
        theme.fogDensity = .008f;
        theme.ambientSky = new Color(.69f, .65f, .8f);
        theme.ambientEquator = new Color(.54f, .48f, .64f);
        theme.ambientGround = new Color(.28f, .24f, .36f);
        theme.sunColor = new Color(1f, .88f, .75f);
        theme.sunIntensity = 1.1f;
        theme.palette = palette.Values.ToArray();
        AssetDatabase.CreateAsset(theme, ThemePath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = theme;
        Debug.Log("Created the Morioh design assets. Scenes are unchanged. Review the four Layout assets, then run 2 - Apply To Full Scene.");
    }

    [MenuItem("Tools/Morioh/Design Kit/2 - Apply To Full Scene")]
    public static void ApplyToFullScene()
    {
        RequireEditMode();
        MoriohThemeConfig desired = Require<MoriohThemeConfig>(ThemePath);
        if (desired.streetModules == null || desired.streetModules.Length != 4 ||
            desired.streetModules.Any(p => p == null) || desired.activityArt == null ||
            desired.punchTargetVisual == null || desired.skyboxMaterial == null || desired.horizonPrefab == null)
            throw new InvalidOperationException("The design theme is incomplete.");
        Scene open = SceneManager.GetSceneByPath(FullScene);
        if (open.IsValid() && open.isLoaded && open.isDirty)
            throw new InvalidOperationException("Save or discard your own pending Full scene edits before applying the design.");
        ValidateFullScene();
        string recovery = "Assets/_Recovery/MoriohDesign_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        EnsureFolder(recovery);
        if (!AssetDatabase.CopyAsset(FullScene, recovery + "/AM3D_Morioh_Full.unity") ||
            !AssetDatabase.CopyAsset(LiveTheme, recovery + "/MoriohThemeConfig.asset"))
            throw new IOException("Could not make both recovery copies; nothing was applied.");
        MoriohThemeConfig live = Require<MoriohThemeConfig>(LiveTheme);
        string oldName = live.name;
        EditorUtility.CopySerialized(desired, live);
        live.name = oldName;
        EditorUtility.SetDirty(live);
        AssetDatabase.SaveAssets();
        MoriohFullBuilder.ApplyThemeToFullScene();
        Debug.Log("Applied Morioh artwork to Full only. Recovery scene and config: " + recovery +
                  ". Track roots, lane settings, gameplay flags, UI, input and build settings were preserved.");
    }

    [MenuItem("Tools/Morioh/Design Kit/Rebuild Modules From Edited Layouts")]
    public static void RebuildModules()
    {
        RequireEditMode();
        Require<MoriohThemeConfig>(ThemePath);
        for (int i = 0; i < 4; i++)
            BuildModule(i, Require<MoriohModuleLayout>(Generated + "/Layouts/" + ModuleNames[i] + ".asset"));
        AssetDatabase.SaveAssets();
        Debug.Log("Rebuilt only the four generated module prefabs. Existing Full scene instances update through their prefab links.");
    }

    private static void ValidateFullScene()
    {
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(FullScene);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(FullScene, OpenSceneMode.Additive);
        try
        {
            Transform track = scene.GetRootGameObjects().Single(r => r.name == "TrackManager").transform;
            var manager = track.GetComponent<ActiveMotion3D.EndlessRunner.Track.TrackManager>();
            SerializedObject trackData = new SerializedObject(manager);
            if (!Mathf.Approximately(trackData.FindProperty("laneWidth").floatValue, 3.2f) ||
                !Mathf.Approximately(trackData.FindProperty("segmentLength").floatValue, 20f))
                throw new InvalidOperationException("This art kit requires the existing 20-unit track and 3.2-unit lanes.");
            foreach (string name in new[] { "Main Camera", "Directional Light", "ObstacleSpawner" })
                if (!scene.GetRootGameObjects().Any(r => r.name == name))
                    throw new InvalidOperationException("Missing scene root " + name);
            string[] original = { "Road", "Ground", "LaneL", "LaneR", "EdgeL", "EdgeR", "BuildingL", "BuildingR" };
            for (int i = 0; i < 8; i++)
            {
                Transform segment = track.Find("TrackSegment_" + i.ToString("00"));
                if (segment == null || segment.GetComponent<ActiveMotion3D.EndlessRunner.Track.TrackSegment>() == null ||
                    original.Any(n => segment.Find(n) == null))
                    throw new InvalidOperationException("TrackSegment_" + i.ToString("00") + " does not match the authored scene.");
            }
            var spawner = scene.GetRootGameObjects().Single(r => r.name == "ObstacleSpawner")
                .GetComponent<ActiveMotion3D.EndlessRunner.Obstacles.ObstacleSpawner>();
            if (spawner == null || !Mathf.Approximately(new SerializedObject(spawner).FindProperty("laneWidth").floatValue, 3.2f))
                throw new InvalidOperationException("Spawner lane width must remain 3.2 units.");
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static Dictionary<string, Material> CreatePalette()
    {
        var colors = new Dictionary<string, Color32>
        {
            { "Ink", new Color32(49,39,64,255) }, { "Cream", new Color32(245,226,186,255) },
            { "Mint", new Color32(159,207,180,255) }, { "Peach", new Color32(236,170,161,255) },
            { "Lilac", new Color32(183,163,210,255) }, { "Roof", new Color32(93,81,130,255) },
            { "Glass", new Color32(84,143,155,255) }, { "Wood", new Color32(98,66,82,255) },
            { "Gold", new Color32(232,191,76,255) }, { "Leaf", new Color32(141,78,173,255) },
            { "Blossom", new Color32(201,132,205,255) }, { "Road", new Color32(75,73,88,255) },
            { "Paving", new Color32(191,182,194,255) }, { "White", new Color32(247,242,216,255) },
            { "Red", new Color32(193,73,111,255) }, { "Metal", new Color32(97,108,125,255) },
            { "FarHill", new Color32(145,137,168,255) }, { "NearHill", new Color32(113,111,145,255) },
            { "Town", new Color32(107,99,136,255) }, { "Sign", new Color32(255,255,255,255) },
            { "DistantGround", new Color32(168,150,181,255) }
        };
        var result = new Dictionary<string, Material>();
        foreach (var pair in colors)
        {
            Material material = CreateMaterial(pair.Key, pair.Value, pair.Key == "Glass" ? .5f : .12f);
            result.Add(pair.Key, material);
        }
        foreach (string name in new[] { "Mint", "Peach", "Lilac" })
            result[name].mainTexture = Require<Material>(ExistingMaterials + "Morioh_StuccoMint.mat").mainTexture;
        result["Roof"].mainTexture = Require<Material>(ExistingMaterials + "Morioh_RoofCeramic.mat").mainTexture;
        result["Leaf"].mainTexture = Require<Material>(ExistingMaterials + "Morioh_JacarandaLeaves.mat").mainTexture;
        result["Sign"].shader = Shader.Find("Unlit/Texture");
        result["Sign"].mainTexture = ConfigureTexture("NeighborhoodSigns", false, true);
        result["Paving"].mainTexture = ConfigureTexture("SidewalkPavers", false, false);
        foreach (string name in new[] { "FarHill", "NearHill", "Town", "DistantGround" })
            result[name].shader = Require<Shader>(Kit + "/HorizonInk.shader");
        foreach (Material material in result.Values) EditorUtility.SetDirty(material);
        return result;
    }

    private static Material CreateMaterial(string name, Color color, float smoothness)
    {
        Material material = new Material(Shader.Find("Standard")) { name = name, color = color, enableInstancing = true };
        material.SetFloat("_Glossiness", smoothness);
        // State adapters use per-material property blocks; no per-frame material allocation.
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", Color.black);
        AssetDatabase.CreateAsset(material, Generated + "/Materials/" + name + ".mat");
        return material;
    }

    private static Material CreateSky()
    {
        Material sky = new Material(Require<Shader>(Kit + "/LavenderCloudSky.shader")) { name = "LavenderCloudSky" };
        sky.SetTexture("_Clouds", ConfigureTexture("LavenderCloudPanorama", true, true));
        AssetDatabase.CreateAsset(sky, Generated + "/Materials/LavenderCloudSky.mat");
        return sky;
    }

    private static Texture2D ConfigureTexture(string name, bool transparent, bool clamp)
    {
        string path = Source + "/Textures/" + name + ".png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Import source textures first: " + path);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaIsTransparency = transparent;
        importer.mipmapEnabled = true;
        importer.maxTextureSize = 2048;
        importer.wrapModeU = name == "LavenderCloudPanorama" ? TextureWrapMode.Repeat : clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        importer.wrapModeV = clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        importer.SaveAndReimport();
        return Require<Texture2D>(path);
    }

    private static GameObject CreateModelPrefab(string name, Dictionary<string, Material> materials)
    {
        string path = Source + "/Models/" + name + ".obj";
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) throw new InvalidOperationException("Import source models first: " + path);
        importer.globalScale = 1f;
        importer.importNormals = ModelImporterNormals.Calculate;
        importer.normalSmoothingAngle = 0f;
        importer.addCollider = false;
        importer.importAnimation = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        importer.SaveAndReimport();
        GameObject root = new GameObject(name);
        try
        {
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(Require<GameObject>(path));
            model.transform.SetParent(root.transform, false);
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    string materialName = slots[i] != null ? slots[i].name : "";
                    if (!materials.TryGetValue(materialName, out Material replacement))
                        throw new InvalidOperationException("Unmapped OBJ material '" + materialName + "' on " + name);
                    slots[i] = replacement;
                }
                renderer.sharedMaterials = slots;
                if (name == "ContinuousHorizon")
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
            AssertVisualOnly(root);
            return PrefabUtility.SaveAsPrefabAsset(root, Generated + "/Models/" + name + ".prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static GameObject CreatePunchVisual(GameObject source, MoriohActivityArt art, Dictionary<string, Material> palette, Font font)
    {
        GameObject root = new GameObject("Morioh_HighSideTarget_Visual");
        try
        {
            InstantiateVisual(source, root.transform, Vector3.zero, Vector3.zero, Vector3.one, "Crest");
            MoriohPunchVisual adapter = root.AddComponent<MoriohPunchVisual>();
            adapter.art = art;
            var slots = new List<MoriohMaterialSlot>();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    if (renderer.sharedMaterials[i] == palette["Lilac"] || renderer.sharedMaterials[i] == palette["Cream"])
                        slots.Add(new MoriohMaterialSlot { renderer = renderer, materialIndex = i });
            adapter.stateSlots = slots.ToArray();
            if (slots.Count == 0) throw new InvalidOperationException("Target state material slots were not imported.");
            adapter.sideLabel = Label(root.transform, "SideCue", "LEFT", new Vector3(0,-.8f,-.25f), .08f, font, art.instructionColor);
            AssertVisualOnly(root);
            return PrefabUtility.SaveAsPrefabAsset(root, Generated + "/Activities/Morioh_HighSideTarget_Visual.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static GameObject CreateBurst(string name, string text, Material gold, Font font)
    {
        GameObject root = new GameObject(name);
        try
        {
            root.AddComponent<MoriohBurstEffect>();
            Label(root.transform, "ComicPunctuation", text, Vector3.zero, .18f, font, gold.color);
            ParticleSystem particles = new GameObject("Sparkles").AddComponent<ParticleSystem>();
            particles.transform.SetParent(root.transform, false);
            var main = particles.main;
            main.loop = false;
            main.duration = .4f;
            main.startLifetime = .35f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(.035f, .09f);
            main.startColor = new ParticleSystem.MinMaxGradient(gold.color, new Color(1f,.95f,.8f));
            main.maxParticles = 16;
            var emission = particles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .15f;
            Material particleMaterial = AssetDatabase.LoadAssetAtPath<Material>(Generated + "/Materials/ComicParticles.mat");
            if (particleMaterial == null)
            {
                particleMaterial = new Material(Shader.Find("Particles/Standard Unlit")) { name = "ComicParticles" };
                AssetDatabase.CreateAsset(particleMaterial, Generated + "/Materials/ComicParticles.mat");
            }
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
            return PrefabUtility.SaveAsPrefabAsset(root, Generated + "/Activities/" + name + ".prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static GameObject CreateHorizon(GameObject model)
    {
        GameObject root = new GameObject("Morioh_Horizon");
        try
        {
            InstantiateVisual(model, root.transform, Vector3.zero, Vector3.zero, Vector3.one, "HillsAndTown");
            root.AddComponent<MoriohHorizonFollower>();
            return PrefabUtility.SaveAsPrefabAsset(root, Generated + "/Morioh_Horizon.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static MoriohModuleLayout CreateLayout(int variant, Dictionary<string, GameObject> models)
    {
        MoriohModuleLayout layout = ScriptableObject.CreateInstance<MoriohModuleLayout>();
        var p = new List<MoriohPlacement>();
        void Add(string model, float x, float z, float yaw = 0, float scale = 1f, float y = 0)
        {
            p.Add(new MoriohPlacement { name = model + "_" + p.Count, visualPrefab = models[model],
                localPosition = new Vector3(x,y,z), localEulerAngles = new Vector3(0,yaw,0), localScale = Vector3.one * scale });
        }
        void Building(string model, int side, float z) => Add(model, side*10.8f, z, side < 0 ? -90 : 90);
        Add("StreetBase20m", 0, 0);
        for (int side = -1; side <= 1; side += 2)
        {
            Add("UtilityPole", side*7.15f, 0);
            Add("OverheadWireSpan20m", side*7.15f, 0);
            Add("StreetLamp", side*7.2f, variant % 2 == 0 ? 6 : 16, side < 0 ? 0 : 180);
            Add("RouteMarker", side*6.7f, 18, side < 0 ? -90 : 90);
        }
        if (variant == 0)
        {
            layout.designNotes = "Quiet narrow-lot residential lane. Hipped ceramic roof contrasts with a flat parapet house; balconies, shutters, air conditioners, entry canopies and purple flowering trees.";
            Building("HippedRoofResidence",-1,3.4f); Building("SplitLevelResidence",-1,14.3f);
            Building("SplitLevelResidence",1,4.8f); Building("HippedRoofResidence",1,14.6f);
            Add("PurpleFlowerTree",-7.9f,8.6f); Add("PurpleFlowerTree",8.4f,9.7f);
            Add("NeighborhoodBicycle",-7.9f,12,-90); Add("PostBox",7.8f,2,90);
            Add("FlowerPlanter",-6.6f,5.8f,90); Add("FlowerPlanter",6.6f,17,90);
        }
        else if (variant == 1)
        {
            layout.designNotes = "Shopping corner: low convenience store, radio shop, general store, side-street apron and zebra crossing. Low flat shop roof opens the skyline; mixed setbacks break the row.";
            Building("NijiConvenience",-1,4.5f); Building("YunagiRadio",-1,15.5f);
            Building("MoriohGeneralStore",1,4.5f); Building("SplitLevelResidence",1,15);
            Add("SideStreetApron",-11.4f,10.2f); Add("ZebraCrossing",0,10.2f);
            Add("VendingMachine",-7.8f,7.5f,-90); Add("NeighborhoodBicycle",8,7.7f,90);
            Add("PurpleFlowerTree",8.4f,11.2f); Add("PostBox",-6.5f,14,-90);
            Add("GuardRail",6.3f,12,90);
        }
        else if (variant == 2)
        {
            layout.designNotes = "Cafe and bakery promenade: mono-pitch shop roofs, striped awnings, sidewalk tables, bread displays, bookshop and planted seating. Shopfronts face the runner's street.";
            Building("SumireCafe",-1,4.7f); Building("BooksAndRecords",-1,14.8f);
            Building("HoshiBakery",1,5.4f); Building("HippedRoofResidence",1,15);
            Add("PurpleFlowerTree",-8.3f,9.6f); Add("PurpleFlowerTree",8.3f,10.4f);
            Add("NeighborhoodBench",-7.5f,11.7f,-90); Add("NeighborhoodBench",7.5f,12.7f,90);
            Add("FlowerPlanter",-6.5f,7.6f,90); Add("FlowerPlanter",6.5f,8.5f,90);
            Add("VendingMachine",8,17.8f,90);
        }
        else
        {
            layout.designNotes = "Clock plaza and station kiosk: a lower open left edge with a clock landmark, circular paving, benches and trees; taller radio shop and residence form the opposite edge. Original neighborhood landmark, not a copied franchise model.";
            Add("ClockPlazaPaving",-11.4f,10);
            Add("ClockLandmark",-10.3f,9,-90);
            Add("StationKiosk",-11.6f,17,-90);
            Building("YunagiRadio",1,4.5f); Building("HippedRoofResidence",1,15);
            Add("PurpleFlowerTree",-9.4f,2.9f); Add("PurpleFlowerTree",-14.3f,13);
            Add("NeighborhoodBench",-8.7f,12.2f,-90); Add("NeighborhoodBench",-13.8f,6.3f,90);
            Add("GuardRail",-6.4f,7,90); Add("FlowerPlanter",-6.5f,12,90);
            Add("PostBox",-7.8f,16,-90); Add("VendingMachine",8,8.4f,90);
        }
        layout.placements = p.ToArray();
        return layout;
    }

    private static GameObject BuildModule(int index, MoriohModuleLayout layout)
    {
        string name = "Morioh_Street_20m_" + ModuleNames[index];
        GameObject root = new GameObject(name);
        try
        {
            if (layout.placements == null) throw new InvalidOperationException("Layout is empty.");
            foreach (MoriohPlacement placement in layout.placements)
            {
                if (placement == null || placement.visualPrefab == null)
                    throw new InvalidOperationException("A layout placement has no prefab: " + layout.name);
                InstantiateVisual(placement.visualPrefab, root.transform, placement.localPosition,
                    placement.localEulerAngles, placement.localScale, placement.name);
            }
            AssertVisualOnly(root);
            return PrefabUtility.SaveAsPrefabAsset(root, Generated + "/Modules/" + name + ".prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void InstantiateVisual(GameObject prefab, Transform parent, Vector3 position, Vector3 angles, Vector3 scale, string name)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.localPosition = position;
        instance.transform.localEulerAngles = angles;
        instance.transform.localScale = scale;
    }

    private static TextMesh Label(Transform parent, string name, string text, Vector3 position, float size, Font font, Color color)
    {
        GameObject go = new GameObject(name, typeof(TextMesh));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        TextMesh label = go.GetComponent<TextMesh>();
        label.font = font;
        label.fontSize = 64;
        label.characterSize = size;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.text = text;
        label.color = color;
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        return label;
    }

    private static void AssertVisualOnly(GameObject root)
    {
        if (root.GetComponentsInChildren<Collider>(true).Length != 0 || root.GetComponentsInChildren<Rigidbody>(true).Length != 0)
            throw new InvalidOperationException("Visual asset contains physics components: " + root.name);
    }

    private static void RequireEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before authoring assets.");
    }
    private static T Require<T>(string path) where T : UnityEngine.Object
    {
        T value = AssetDatabase.LoadAssetAtPath<T>(path);
        if (value == null) throw new InvalidOperationException("Missing dependency: " + path);
        return value;
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\','/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
