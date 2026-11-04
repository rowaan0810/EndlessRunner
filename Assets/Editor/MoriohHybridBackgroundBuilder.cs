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

/// <summary>Explicit installation of a visual-only painted / 3D hybrid background.</summary>
public static class MoriohHybridBackgroundBuilder
{
    private const string Art = "Assets/ActiveMotion3D/Art/Environment/Morioh/HybridBackground";
    private const string Output = Art + "/Generated";
    private const string ProfilePath = Output + "/MoriohBackgroundProfile.asset";
    private const string FullScene = "Assets/ActiveMotion3D/Scenes/AM3D_Morioh_Full.unity";
    private const string LiveTheme = "Assets/ActiveMotion3D/Art/Config/MoriohThemeConfig.asset";
    private const string DesignTheme = "Assets/ActiveMotion3D/Art/Environment/Morioh/DesignKit/Generated/MoriohDesignTheme.asset";
    private const string InkShader = "Assets/ActiveMotion3D/Art/Environment/Morioh/DesignKit/HorizonInk.shader";

    [MenuItem("Tools/Morioh/Background/1 - Build Hybrid Background Assets")]
    public static void Build()
    {
        RequireEditMode();
        if (AssetDatabase.LoadAssetAtPath<MoriohBackgroundProfile>(ProfilePath) != null)
            throw new InvalidOperationException("The hybrid profile already exists. Edit it, then use Rebuild From Profile.");
        Texture2D sky = ImportPainting("PaintedLavenderSky", false);
        Texture2D clouds = ImportPainting("AnimeCloudAtlas", true);
        Texture2D hills = ImportPainting("PaintedWoodedHills", true);
        foreach (string shader in new[] { "PaintedSky", "IllustratedCloud", "PaintedHills" })
            Require<Shader>(Art + "/" + shader + ".shader");
        Require<Shader>(InkShader);
        Require<GameObject>(Art + "/Models/TownSilhouette.obj");
        if (Directory.Exists(Output))
        {
            EnsureFolder("Assets/_Recovery");
            string recovery = "Assets/_Recovery/MoriohHybridIncomplete_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string error = AssetDatabase.MoveAsset(Output, recovery);
            if (!string.IsNullOrEmpty(error)) throw new IOException(error);
            Debug.Log("Preserved incomplete hybrid output at " + recovery);
        }
        foreach (string folder in new[] { "Materials", "Meshes" }) EnsureFolder(Output + "/" + folder);
        MoriohBackgroundProfile profile = ScriptableObject.CreateInstance<MoriohBackgroundProfile>();
        profile.name = "MoriohBackgroundProfile";
        profile.paintedSky = sky;
        profile.cloudAtlas = clouds;
        profile.paintedHills = hills;
        profile.cloudBands = new[] { MakeBand("Distant wisps",18,450f,.035f,10f,8f,new Color(.94f,.95f,1f,.78f),6f),
                                     MakeBand("Near drifting wisps",12,400f,.08f,14f,11f,Color.white,10f) };
        profile.skyMaterial = MaterialAsset("PaintedLavenderSky", Require<Shader>(Art + "/PaintedSky.shader"));
        profile.skyMaterial.SetTexture("_MainTex", sky);
        profile.skyMaterial.SetColor("_HorizonColor", profile.horizonColor);
        profile.cloudMaterial = MaterialAsset("IllustratedClouds", Require<Shader>(Art + "/IllustratedCloud.shader"));
        profile.cloudMaterial.SetTexture("_MainTex", clouds);
        profile.farHillMaterial = MaterialAsset("FarPaintedHills", Require<Shader>(Art + "/PaintedHills.shader"));
        profile.nearHillMaterial = MaterialAsset("NearPaintedHills", Require<Shader>(Art + "/PaintedHills.shader"));
        profile.farHillMaterial.SetTexture("_MainTex", hills);
        profile.farHillMaterial.SetColor("_Tint", profile.farHillTint);
        profile.farHillMaterial.SetFloat("_Offset", .12f);
        profile.nearHillMaterial.SetTexture("_MainTex", hills);
        profile.nearHillMaterial.SetColor("_Tint", profile.nearHillTint);
        profile.nearHillMaterial.SetFloat("_Offset", .48f);
        CreateTownPrefab();
        AssetDatabase.CreateAsset(profile, ProfilePath);
        RebuildFromProfile();
        foreach (Material material in new[] { profile.skyMaterial, profile.cloudMaterial, profile.farHillMaterial, profile.nearHillMaterial })
            EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        Selection.activeObject = profile;
        Debug.Log("Hybrid background created: painted sky, 30 drifting cloud cards, two painted hill layers and detailed town. Run Background / 2 - Apply To Full Scene.");
    }

    [MenuItem("Tools/Morioh/Background/Rebuild From Profile")]
    public static void RebuildFromProfile()
    {
        RequireEditMode();
        MoriohBackgroundProfile profile = Require<MoriohBackgroundProfile>(ProfilePath);
        if (profile.cloudAtlas == null || profile.cloudBands == null || profile.skyMaterial == null ||
            profile.cloudMaterial == null || profile.farHillMaterial == null || profile.nearHillMaterial == null)
            throw new InvalidOperationException("Assign the paintings, cloud bands and materials in the background profile.");
        int columns = Mathf.Max(1,profile.atlasColumns), rows = Mathf.Max(1,profile.atlasRows);
        Mesh[] sprites = new Mesh[columns*rows];
        for (int i=0;i<sprites.Length;i++) sprites[i]=SaveMesh("CloudSprite_"+i, SpriteMesh(i,columns,rows,profile.atlasInset));
        Mesh ring = SaveMesh("PaintedHillRing", HillRing());
        GameObject root = new GameObject("Morioh_Horizon");
        try
        {
            root.AddComponent<MoriohHorizonFollower>();
            MoriohBackgroundController controller = root.AddComponent<MoriohBackgroundController>();
            controller.profile = profile;
            controller.cloudSpriteMeshes = sprites;
            controller.farHills = MeshObject("FarPaintedHills",root.transform,ring,profile.farHillMaterial).transform;
            controller.nearHills = MeshObject("NearPaintedHills",root.transform,ring,profile.nearHillMaterial).transform;
            // Keep the backdrop outside Town so raising the skyline cannot raise
            // a giant ground surface through the gameplay street.
            Mesh groundMesh = SaveMesh("DistantGround", GroundPlane());
            MeshObject("DistantGround",root.transform,groundMesh,Require<Material>(Output+"/Materials/Ground.mat"))
                .transform.localPosition = Vector3.down * .35f;
            GameObject town = (GameObject)PrefabUtility.InstantiatePrefab(Require<GameObject>(Output+"/TownSilhouette.prefab"),root.transform);
            controller.town = town.transform;
            controller.cloudRigs = new MoriohCloudRig[profile.cloudBands.Length];
            for (int b=0;b<profile.cloudBands.Length;b++)
            {
                MoriohCloudBand band = profile.cloudBands[b];
                GameObject group = new GameObject(band?.name ?? "CloudBand");
                group.transform.SetParent(root.transform,false);
                int count=band?.clouds?.Length ?? 0;
                var cards=new MoriohCloudCard[count];
                for (int i=0;i<count;i++)
                {
                    GameObject card=MeshObject("Wisp_"+i,group.transform,sprites[0],profile.cloudMaterial);
                    cards[i]=new MoriohCloudCard { transform=card.transform,meshFilter=card.GetComponent<MeshFilter>(),renderer=card.GetComponent<Renderer>() };
                }
                controller.cloudRigs[b]=new MoriohCloudRig { cards=cards };
            }
            controller.ApplyLayout();
            if (root.GetComponentsInChildren<Collider>(true).Length!=0 || root.GetComponentsInChildren<Rigidbody>(true).Length!=0)
                throw new InvalidOperationException("The background must have no physics components.");
            profile.backgroundPrefab=PrefabUtility.SaveAsPrefabAsset(root,Output+"/Morioh_HybridBackground.prefab");
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    [MenuItem("Tools/Morioh/Background/2 - Apply To Full Scene")]
    public static void Apply()
    {
        RequireEditMode();
        MoriohBackgroundProfile profile=Require<MoriohBackgroundProfile>(ProfilePath);
        if (profile.skyMaterial==null || profile.backgroundPrefab==null)
            throw new InvalidOperationException("Build the complete background first.");
        Require<MoriohThemeConfig>(LiveTheme);
        Scene previous=SceneManager.GetActiveScene();
        Scene scene=SceneManager.GetSceneByPath(FullScene);
        bool openedHere=!scene.IsValid() || !scene.isLoaded;
        if (!openedHere && scene.isDirty) throw new InvalidOperationException("Save your pending Full scene edits before applying the background.");
        if (openedHere) scene=EditorSceneManager.OpenScene(FullScene,OpenSceneMode.Additive);
        try
        {
            Camera camera=scene.GetRootGameObjects().Where(r=>r.name=="Main Camera").Select(r=>r.GetComponent<Camera>()).FirstOrDefault();
            if (camera==null || camera.farClipPlane<600f)
                throw new InvalidOperationException("Expected the existing Main Camera and far clip of at least 600. No camera lens settings were changed.");
            GameObject old=scene.GetRootGameObjects().FirstOrDefault(r=>r.name=="Morioh_Horizon");
            if (old!=null && old.GetComponent<MoriohHorizonFollower>()==null)
                throw new InvalidOperationException("Morioh_Horizon is not the known background root; inspect it before replacement.");
            string recovery="Assets/_Recovery/MoriohHybrid_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            EnsureFolder(recovery);
            if (!AssetDatabase.CopyAsset(FullScene,recovery+"/AM3D_Morioh_Full.unity") ||
                !AssetDatabase.CopyAsset(LiveTheme,recovery+"/MoriohThemeConfig.asset"))
                throw new IOException("Could not make recovery copies; background was not applied.");
            MoriohThemeConfig design=AssetDatabase.LoadAssetAtPath<MoriohThemeConfig>(DesignTheme);
            if (design!=null && !AssetDatabase.CopyAsset(DesignTheme,recovery+"/MoriohDesignTheme.asset"))
                throw new IOException("Could not back up the design theme.");
            Material ground=Require<Material>(Output+"/Materials/Ground.mat");
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(profile.skyMaterial),recovery+"/PaintedLavenderSky.mat") ||
                !AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(ground),recovery+"/Ground.mat"))
                throw new IOException("Could not back up the matching sky/ground colors.");
            SceneManager.SetActiveScene(scene);
            if (old!=null && PrefabUtility.GetCorrespondingObjectFromSource(old)!=profile.backgroundPrefab)
            { UnityEngine.Object.DestroyImmediate(old);old=null; }
            if (old==null) old=(GameObject)PrefabUtility.InstantiatePrefab(profile.backgroundPrefab,scene);
            old.name="Morioh_Horizon";
            old.transform.position=Vector3.zero;
            MoriohHorizonFollower follower=old.GetComponent<MoriohHorizonFollower>();
            follower.target=camera.transform;
            follower.offset=Vector3.zero;
            follower.followHorizontalCameraPosition=true;
            RenderSettings.skybox=profile.skyMaterial;
            profile.skyMaterial.SetColor("_HorizonColor",profile.horizonColor);
            ground.color=profile.horizonColor;
            EditorUtility.SetDirty(profile.skyMaterial);
            EditorUtility.SetDirty(ground);
            RenderSettings.fogColor=profile.horizonColor;
            RenderSettings.fogDensity=profile.fogDensity;
            camera.clearFlags=CameraClearFlags.Skybox;
            Skybox overrideSky=camera.GetComponent<Skybox>();
            if (overrideSky!=null) overrideSky.material=profile.skyMaterial;
            UpdateTheme(Require<MoriohThemeConfig>(LiveTheme),profile);
            if (design!=null) UpdateTheme(design,profile);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Full scene; recovery is at "+recovery);
            AssetDatabase.SaveAssets();
            Debug.Log("Hybrid background installed in Full only. Recovery copies: "+recovery+". Cloud drift is visible in Play Mode.");
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (openedHere) EditorSceneManager.CloseScene(scene,true);
        }
    }

    private static void UpdateTheme(MoriohThemeConfig theme,MoriohBackgroundProfile profile)
    {
        theme.skyboxMaterial=profile.skyMaterial;
        theme.horizonPrefab=profile.backgroundPrefab;
        theme.horizonOffset=Vector3.zero;
        theme.horizonScale=Vector3.one;
        theme.horizonFollowsCameraXZ=true;
        theme.fogColor=profile.horizonColor;
        theme.fogDensity=profile.fogDensity;
        EditorUtility.SetDirty(theme);
    }

    private static MoriohCloudBand MakeBand(string name,int count,float radius,float speed,float width,float elevation,Color tint,float offset)
    {
        var band=new MoriohCloudBand { name=name,radius=radius,driftDegreesPerSecond=speed,tint=tint,clouds=new MoriohCloudPlacement[count] };
        for(int i=0;i<count;i++) band.clouds[i]=new MoriohCloudPlacement { spriteIndex=(i*5)%12,
            azimuthDegrees=-180f+i*360f/count+offset,elevationDegrees=elevation+i%4*2f,widthDegrees=width+i%3*1.5f };
        return band;
    }

    private static Texture2D ImportPainting(string name,bool alpha)
    {
        string path=Art+"/Textures/"+name+".png";
        TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null) throw new InvalidOperationException("Refresh Unity assets before building: "+path);
        importer.textureType=TextureImporterType.Default;
        importer.textureShape=TextureImporterShape.Texture2D;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency=alpha;
        importer.sRGBTexture=true;
        importer.mipmapEnabled=true;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.maxTextureSize=4096;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.wrapMode=TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return Require<Texture2D>(path);
    }

    private static void CreateTownPrefab()
    {
        string path=Art+"/Models/TownSilhouette.obj";
        var importer=AssetImporter.GetAtPath(path) as ModelImporter;
        if(importer==null) throw new InvalidOperationException("Import the town OBJ first.");
        importer.globalScale=1;
        importer.importNormals=ModelImporterNormals.Calculate;
        importer.normalSmoothingAngle=0;
        importer.addCollider=false;
        importer.importAnimation=false;
        importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        importer.materialLocation=ModelImporterMaterialLocation.InPrefab;
        importer.SaveAndReimport();
        var colors=new Dictionary<string,Color32> { {"TownNear",new Color32(112,107,143,255)},
            {"TownMid",new Color32(139,129,163,255)},{"Roof",new Color32(93,89,124,255)},
            {"Window",new Color32(179,171,190,255)},{"Trim",new Color32(89,84,118,255)},
            {"Ground",new Color32(168,150,181,255)} };
        var palette=new Dictionary<string,Material>();
        foreach(var pair in colors)
        { Material mat=MaterialAsset(pair.Key,Require<Shader>(InkShader));mat.color=pair.Value;EditorUtility.SetDirty(mat);palette.Add(pair.Key,mat); }
        GameObject root=(GameObject)PrefabUtility.InstantiatePrefab(Require<GameObject>(path));
        try
        {
            foreach(Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++)
                {
                    if(slots[i]==null || !palette.TryGetValue(slots[i].name,out Material replacement))
                        throw new InvalidOperationException("Unknown town material slot on "+renderer.name);
                    slots[i]=replacement;
                }
                renderer.sharedMaterials=slots;
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                renderer.receiveShadows=false;
            }
            PrefabUtility.SaveAsPrefabAsset(root,Output+"/TownSilhouette.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static Material MaterialAsset(string name,Shader shader)
    {
        Material material=new Material(shader) {name=name};
        AssetDatabase.CreateAsset(material,Output+"/Materials/"+name+".mat");
        return material;
    }
    private static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material material)
    {
        GameObject go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
        go.transform.SetParent(parent,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;
        Renderer renderer=go.GetComponent<Renderer>();
        renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;
        renderer.receiveShadows=false;
        return go;
    }
    private static Mesh SaveMesh(string name,Mesh mesh)
    {
        string path=Output+"/Meshes/"+name+".asset";
        Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing!=null) {EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;}
        mesh.name=name;AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    private static Mesh SpriteMesh(int index,int columns,int rows,float inset)
    {
        float pad=Mathf.Clamp(inset,0,.2f),x=index%columns,y=index/columns;
        float u0=(x+pad)/columns,u1=(x+1-pad)/columns,v0=1-(y+1-pad)/rows,v1=1-(y+pad)/rows;
        Mesh mesh=new Mesh();
        mesh.vertices=new[] { new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0) };
        mesh.uv=new[] {new Vector2(u0,v0),new Vector2(u1,v0),new Vector2(u1,v1),new Vector2(u0,v1)};
        mesh.triangles=new[] {0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    private static Mesh HillRing()
    {
        const int count=128;
        var vertices=new Vector3[(count+1)*2];var uv=new Vector2[vertices.Length];var triangles=new int[count*6];
        for(int i=0;i<=count;i++)
        {
            float a=i*Mathf.PI*2/count;Vector3 p=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
            vertices[i*2]=p;vertices[i*2+1]=p+Vector3.up;uv[i*2]=new Vector2(i/(float)count,0);uv[i*2+1]=new Vector2(i/(float)count,1);
            if(i==count)continue;
            int t=i*6,v=i*2;triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+3;triangles[t+3]=v;triangles[t+4]=v+3;triangles[t+5]=v+2;
        }
        Mesh mesh=new Mesh {vertices=vertices,uv=uv,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    private static Mesh GroundPlane()
    {
        Mesh mesh=new Mesh();
        mesh.vertices=new[] {new Vector3(-450,0,-450),new Vector3(-450,0,450),new Vector3(450,0,450),new Vector3(450,0,-450)};
        mesh.triangles=new[] {0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    private static void RequireEditMode()
    {if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode before background authoring.");}
    private static T Require<T>(string path) where T:UnityEngine.Object
    {T value=AssetDatabase.LoadAssetAtPath<T>(path);if(value==null)throw new InvalidOperationException("Missing background dependency: "+path);return value;}
    private static void EnsureFolder(string path)
    {if(AssetDatabase.IsValidFolder(path))return;EnsureFolder(Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));}
}
