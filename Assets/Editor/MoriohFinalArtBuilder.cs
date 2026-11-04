using System;
using System.IO;
using ActiveMotion3D.EndlessRunner.Environment;
using UnityEditor;
using UnityEngine;

/// <summary>Creates original, visual-only Morioh models and installs them in the four recycling modules.</summary>
public static class MoriohFinalArtBuilder
{
    private const string Art = "Assets/ActiveMotion3D/Art/Environment/Morioh";
    private const string Models = Art + "/Models";
    private const string Textures = Art + "/Textures";
    private const string Materials = "Assets/ActiveMotion3D/Materials/Morioh";
    private const string Config = "Assets/ActiveMotion3D/Art/Config/MoriohThemeConfig.asset";

    [MenuItem("Tools/Morioh/Detail Final Obstacle Art")]
    public static void DetailObstacles()
    {
        Material gold = LoadMaterial("Morioh_Gold");
        Material ink = LoadMaterial("Morioh_Ink");
        Material cream = LoadMaterial("Morioh_CreamTrim");
        string folder = "Assets/ActiveMotion3D/Art/Obstacles/Morioh/";
        DetailBarrier(folder + "Morioh_LowBarrier_Visual.prefab", root =>
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Cube("Detail_EndPost", root, new Vector3(side * 0.48f, 0, -0.08f), new Vector3(0.07f, 0.76f, 0.27f), ink);
                Sphere("Detail_GoldFinial", root, new Vector3(side * 0.48f, 0.42f, -0.08f), new Vector3(0.12f, 0.12f, 0.12f), gold);
            }
            Sphere("Detail_CenterSeal", root, new Vector3(0, 0, -0.23f), new Vector3(0.18f, 0.18f, 0.05f), cream);
        });
        DetailBarrier(folder + "Morioh_HighBarrier_Visual.prefab", root =>
        {
            for (int side = -1; side <= 1; side += 2)
                Cube("Detail_Bracket", root, new Vector3(side * 0.47f, 0, -0.22f), new Vector3(0.08f, 0.66f, 0.12f), gold);
            Sphere("Detail_CenterSeal", root, new Vector3(0, 0, -0.24f), new Vector3(0.12f, 0.12f, 0.05f), cream);
        });
        DetailBarrier(folder + "Morioh_FullBarrier_Visual.prefab", root =>
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Cube("Detail_SideRib", root, new Vector3(side * 0.45f, 0, -0.2f), new Vector3(0.06f, 3.5f, 0.14f), gold);
                for (int row = -1; row <= 1; row++)
                    Sphere("Detail_Rivet", root, new Vector3(side * 0.44f, row * 1.25f, -0.29f), new Vector3(0.11f, 0.11f, 0.05f), cream);
            }
        });
        AssetDatabase.SaveAssets();
    }

    private static void DetailBarrier(string path, Action<Transform> decorate)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (root.transform.Find("Detail_CenterSeal") != null || root.transform.Find("Detail_SideRib") != null)
                return;
            decorate(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    [MenuItem("Tools/Morioh/Face Final Buildings Toward Street")]
    public static void FaceBuildingsTowardStreet()
    {
        string[] names = { "Morioh_Street_20m", "Morioh_Street_20m_B", "Morioh_Street_20m_C", "Morioh_Street_20m_D" };
        foreach (string name in names)
        {
            string path = Art + "/" + name + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                for (int side = 0; side < 2; side++)
                {
                    Transform streetscape = root.transform.Find(side == 0 ? "LeftStreetscape" : "RightStreetscape");
                    foreach (Transform child in streetscape)
                        if (child.name.Contains("_Plot"))
                            child.localRotation = Quaternion.Euler(0, side == 0 ? -90 : 90, 0);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Morioh/Fit Final Art Signs")]
    public static void FitSigns()
    {
        FitSign(Models + "/Morioh_CornerShop.prefab", "ShopName", 0.065f);
        FitSign(Models + "/Morioh_Arcade.prefab", "ProjectingLetter", 0.1f);
        AssetDatabase.SaveAssets();
    }

    private static void FitSign(string path, string objectName, float characterSize)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (TextMesh text in root.GetComponentsInChildren<TextMesh>(true))
                if (text.name == objectName) text.characterSize = characterSize;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    [MenuItem("Tools/Morioh/Build Final Art")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Models + "/Morioh_Residence.prefab") != null)
            throw new InvalidOperationException("Final art already exists; refusing to overwrite authored prefabs.");
        EnsureFolder(Models);
        AssetDatabase.Refresh();

        Material mint = TintedTexture("Morioh_StuccoMint", "Morioh_Stucco_Albedo", new Color(0.75f, 1f, 0.86f));
        Material peach = TintedTexture("Morioh_StuccoPeach", "Morioh_Stucco_Albedo", new Color(1f, 0.77f, 0.72f));
        Material lilac = TintedTexture("Morioh_StuccoLilac", "Morioh_Stucco_Albedo", new Color(0.84f, 0.77f, 1f));
        Material roof = TintedTexture("Morioh_RoofCeramic", "Morioh_RoofTiles_Albedo", Color.white);
        Material leaf = TintedTexture("Morioh_JacarandaLeaves", "Morioh_Jacaranda_Albedo", Color.white);
        Material ink = LoadMaterial("Morioh_Ink");
        Material gold = LoadMaterial("Morioh_Gold");
        Material cyan = LoadMaterial("Morioh_Mint");
        Material purple = LoadMaterial("Morioh_Lavender");
        Material glass = Flat("Morioh_WindowGlass", new Color(0.29f, 0.54f, 0.67f), 0.7f);
        Material timber = Flat("Morioh_Timber", new Color(0.32f, 0.23f, 0.35f), 0.15f);
        Material cream = Flat("Morioh_CreamTrim", new Color(1f, 0.88f, 0.68f), 0.2f);

        GameObject residence = Residence(mint, roof, glass, timber, cream, ink);
        GameObject shop = Shop(peach, roof, glass, cream, ink, gold, purple);
        GameObject arcade = Arcade(lilac, roof, glass, cream, ink, cyan);
        GameObject tree = Jacaranda(timber, leaf, purple);
        GameObject[] buildings = { residence, shop, arcade };
        string[] names = { "Morioh_Street_20m", "Morioh_Street_20m_B", "Morioh_Street_20m_C", "Morioh_Street_20m_D" };
        for (int i = 0; i < names.Length; i++) UpgradeStreet(names[i], i, buildings, tree);

        DetailObstacles();

        MoriohThemeConfig config = AssetDatabase.LoadAssetAtPath<MoriohThemeConfig>(Config);
        if (config == null) throw new InvalidOperationException("Morioh theme config is missing.");
        config.palette = new[] { mint, peach, lilac, roof, leaf, glass, timber, cream, ink, gold, cyan, purple };
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Morioh final art created. Open AM3D_Morioh_Full to see all eight updated street instances.");
    }

    private static GameObject Residence(Material wall, Material roof, Material glass, Material wood, Material trim, Material ink)
    {
        GameObject root = new GameObject("Morioh_Residence");
        try
        {
            Cube("StuccoWalls", root.transform, new Vector3(0, 3.1f, 0), new Vector3(6.3f, 6.2f, 6.2f), wall);
            Cube("Foundation", root.transform, new Vector3(0, 0.23f, -0.12f), new Vector3(6.7f, 0.45f, 6.6f), ink);
            Cube("RoofEave", root.transform, new Vector3(0, 6.28f, 0), new Vector3(7.2f, 0.22f, 7f), trim);
            Roof(root.transform, new Vector3(0, 6.4f, 0), roof);
            Cube("FrontDoor", root.transform, new Vector3(0, 1.16f, -3.17f), new Vector3(1.26f, 2.3f, 0.12f), wood);
            Cube("DoorFrame", root.transform, new Vector3(0, 2.33f, -3.2f), new Vector3(1.5f, 0.18f, 0.2f), trim);
            Cube("DoorKnob", root.transform, new Vector3(0.45f, 1.13f, -3.27f), new Vector3(0.11f, 0.11f, 0.09f), trim);
            for (int floor = 0; floor < 2; floor++)
                for (int side = -1; side <= 1; side += 2)
                    Window(root.transform, new Vector3(side * 1.85f, 1.65f + floor * 2.65f, -3.17f), glass, trim, ink);
            Cube("BalconySlab", root.transform, new Vector3(0, 3.25f, -3.62f), new Vector3(4.6f, 0.2f, 1.08f), trim);
            for (int i = -4; i <= 4; i++)
                Cube("BalconyRail_" + i, root.transform, new Vector3(i * 0.52f, 3.73f, -4.08f), new Vector3(0.07f, 0.86f, 0.07f), ink);
            Cube("BalconyTop", root.transform, new Vector3(0, 4.16f, -4.08f), new Vector3(4.55f, 0.09f, 0.09f), ink);
            return PrefabUtility.SaveAsPrefabAsset(root, Models + "/Morioh_Residence.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static GameObject Shop(Material wall, Material roof, Material glass, Material trim, Material ink, Material gold, Material stripe)
    {
        GameObject root = new GameObject("Morioh_CornerShop");
        try
        {
            Cube("PastelShopWalls", root.transform, new Vector3(0, 2.8f, 0), new Vector3(6.5f, 5.6f, 6.1f), wall);
            Cube("ShopBase", root.transform, new Vector3(0, 0.25f, -0.12f), new Vector3(6.9f, 0.5f, 6.5f), ink);
            Roof(root.transform, new Vector3(0, 5.85f, 0), roof);
            Cube("ShopWindowLeft", root.transform, new Vector3(-1.75f, 1.55f, -3.13f), new Vector3(2.5f, 2.55f, 0.1f), glass);
            Cube("ShopWindowRight", root.transform, new Vector3(1.75f, 1.55f, -3.13f), new Vector3(2.5f, 2.55f, 0.1f), glass);
            Cube("CenterDoor", root.transform, new Vector3(0, 1.36f, -3.2f), new Vector3(1.12f, 2.73f, 0.15f), ink);
            Cube("DoorGlazing", root.transform, new Vector3(0, 1.67f, -3.29f), new Vector3(0.86f, 1.88f, 0.03f), glass);
            Cube("SignBoard", root.transform, new Vector3(0, 4.32f, -3.2f), new Vector3(5.8f, 0.82f, 0.16f), ink);
            Cube("SignInset", root.transform, new Vector3(0, 4.32f, -3.3f), new Vector3(5.52f, 0.62f, 0.04f), gold);
            Text("ShopName", root.transform, "SORA RADIO", new Vector3(-2.25f, 4.18f, -3.36f), 0.065f, ink);
            Cube("AwningCap", root.transform, new Vector3(0, 3.26f, -3.73f), new Vector3(6f, 0.13f, 1.35f), trim);
            for (int i = 0; i < 8; i++)
                Cube("AwningStripe_" + i, root.transform, new Vector3(-2.6f + i * 0.75f, 3.22f, -4.42f), new Vector3(0.72f, 0.16f, 0.16f), i % 2 == 0 ? stripe : trim);
            return PrefabUtility.SaveAsPrefabAsset(root, Models + "/Morioh_CornerShop.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static GameObject Arcade(Material wall, Material roof, Material glass, Material trim, Material ink, Material accent)
    {
        GameObject root = new GameObject("Morioh_Arcade");
        try
        {
            Cube("ArcadeWalls", root.transform, new Vector3(0, 3.35f, 0), new Vector3(6.5f, 6.7f, 6.1f), wall);
            Roof(root.transform, new Vector3(0, 6.95f, 0), roof);
            Cube("Cornice", root.transform, new Vector3(0, 6.68f, -0.14f), new Vector3(7f, 0.28f, 6.6f), ink);
            Cube("GroundFloorGlass", root.transform, new Vector3(0, 1.42f, -3.15f), new Vector3(5.8f, 2.45f, 0.11f), glass);
            for (int i = -2; i <= 2; i++)
                Cube("GroundMullion_" + i, root.transform, new Vector3(i * 1.15f, 1.5f, -3.24f), new Vector3(0.12f, 2.66f, 0.14f), ink);
            for (int i = -1; i <= 1; i++) Window(root.transform, new Vector3(i * 1.9f, 4.6f, -3.15f), glass, trim, ink);
            Cube("ProjectingSign", root.transform, new Vector3(2.9f, 3.55f, -4.02f), new Vector3(0.85f, 1.7f, 1.85f), accent);
            Text("ProjectingLetter", root.transform, "M", new Vector3(2.62f, 3.15f, -5.01f), 0.1f, ink);
            Cube("EntranceStep", root.transform, new Vector3(0, 0.15f, -3.62f), new Vector3(2f, 0.3f, 0.9f), trim);
            return PrefabUtility.SaveAsPrefabAsset(root, Models + "/Morioh_Arcade.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static GameObject Jacaranda(Material wood, Material leaf, Material accent)
    {
        GameObject root = new GameObject("Morioh_Jacaranda");
        try
        {
            Cylinder("Trunk", root.transform, new Vector3(0, 1.55f, 0), new Vector3(0.33f, 1.55f, 0.33f), wood);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(angle) * 1.15f, 4.1f + (i % 2) * 0.3f, Mathf.Sin(angle) * 1.15f);
                Sphere("BlossomCluster_" + i, root.transform, p, new Vector3(2.35f, 1.7f, 2.15f), i % 2 == 0 ? leaf : accent);
            }
            Sphere("UpperCanopy", root.transform, new Vector3(0, 5.05f, 0), new Vector3(2.3f, 1.6f, 2.1f), leaf);
            return PrefabUtility.SaveAsPrefabAsset(root, Models + "/Morioh_Jacaranda.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void UpgradeStreet(string name, int variant, GameObject[] buildings, GameObject tree)
    {
        string path = Art + "/" + name + ".prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            for (int side = 0; side < 2; side++)
            {
                Transform streetscape = root.transform.Find(side == 0 ? "LeftStreetscape" : "RightStreetscape");
                if (streetscape == null) throw new InvalidOperationException("Streetscape missing in " + name);
                foreach (Transform old in streetscape) old.gameObject.SetActive(false);
                float x = side == 0 ? -12.2f : 12.2f;
                for (int plot = 0; plot < 2; plot++)
                {
                    GameObject prefab = buildings[(variant + side + plot) % buildings.Length];
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.scene);
                    instance.transform.SetParent(streetscape, false);
                    instance.transform.localPosition = new Vector3(x, 0, 1.5f + plot * 10f);
                    instance.transform.localRotation = Quaternion.Euler(0, side == 0 ? -90 : 90, 0);
                    instance.name = prefab.name + "_Plot" + plot;
                }
                GameObject bloom = (GameObject)PrefabUtility.InstantiatePrefab(tree, root.scene);
                bloom.transform.SetParent(streetscape, false);
                bloom.transform.localPosition = new Vector3(side == 0 ? -8.6f : 8.6f, 0, 7.1f);
                bloom.name = "Jacaranda_Boulevard";
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void Window(Transform parent, Vector3 p, Material glass, Material trim, Material ink)
    {
        Cube("WindowGlass", parent, p, new Vector3(1.32f, 1.35f, 0.1f), glass);
        Cube("WindowSill", parent, p + new Vector3(0, -0.75f, -0.09f), new Vector3(1.58f, 0.14f, 0.22f), trim);
        Cube("WindowTop", parent, p + new Vector3(0, 0.75f, -0.09f), new Vector3(1.58f, 0.12f, 0.22f), trim);
        Cube("WindowMullion", parent, p + new Vector3(0, 0, -0.08f), new Vector3(0.09f, 1.38f, 0.12f), ink);
    }

    private static void Roof(Transform parent, Vector3 p, Material material)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject slope = Cube("CeramicRoofSlope", parent, p + new Vector3(side * 1.78f, 0.78f, 0), new Vector3(3.85f, 0.16f, 7.2f), material);
            slope.transform.localRotation = Quaternion.Euler(0, 0, side * 24f);
        }
        Cube("RoofRidge", parent, p + new Vector3(0, 1.58f, 0), new Vector3(0.25f, 0.18f, 7.3f), material);
    }

    private static GameObject Cube(string name, Transform parent, Vector3 p, Vector3 size, Material material)
        => Primitive(PrimitiveType.Cube, name, parent, p, size, material);

    private static GameObject Sphere(string name, Transform parent, Vector3 p, Vector3 size, Material material)
        => Primitive(PrimitiveType.Sphere, name, parent, p, size, material);

    private static GameObject Cylinder(string name, Transform parent, Vector3 p, Vector3 size, Material material)
        => Primitive(PrimitiveType.Cylinder, name, parent, p, size, material);

    private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 p, Vector3 size, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = p;
        go.transform.localScale = size;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    private static void Text(string name, Transform parent, string content, Vector3 p, float size, Material material)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = p;
        TextMesh text = go.AddComponent<TextMesh>();
        text.text = content;
        text.characterSize = size;
        text.fontSize = 64;
        text.color = material.color;
    }

    private static Material TintedTexture(string name, string textureName, Color tint)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/" + textureName + ".png");
        if (texture == null) throw new InvalidOperationException("Missing texture: " + textureName);
        Material material = Flat(name, tint, 0.18f);
        material.mainTexture = texture;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material Flat(string name, Color color, float smoothness)
    {
        string path = Materials + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        Shader shader = Shader.Find("Standard");
        if (shader == null) throw new InvalidOperationException("Built-in Standard shader unavailable.");
        Material material = new Material(shader) { name = name, color = color };
        material.SetFloat("_Glossiness", smoothness);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material LoadMaterial(string name)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/" + name + ".mat");
        if (material == null) throw new InvalidOperationException("Missing project material " + name);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
