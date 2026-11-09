using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ActiveMotion3D.EndlessRunner.Obstacles;

namespace ActiveMotion3D.EditorTools
{
    /// <summary>Creates editable, self-contained artwork without changing the open scene.</summary>
    public static class SheerHeartAttackBuilder
    {
        public const string Folder = "Assets/ActiveMotion3D/Art/Obstacles/SheerHeartAttack";
        public const string PrefabPath = Folder + "/SheerHeartAttack.prefab";

        [InitializeOnLoadMethod]
        private static void EnsureAsset() => EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !File.Exists(PrefabPath)) Build();
        };

        [MenuItem("Tools/Active Motion 3D/Build Sheer Heart Attack")]
        public static void Build()
        {
            // Preserve authored edits and the component fileID referenced by the scene.
            if (File.Exists(PrefabPath)) return;
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            Material blue = Material("CeruleanArmor", new Color(.035f, .48f, .65f), .65f);
            Material dark = Material("TrackAndSeams", new Color(.025f, .045f, .065f), .3f);
            Material bone = Material("SkullIvory", new Color(.85f, .87f, .77f), .2f);
            Material green = Material("FuseGreen", new Color(.35f, .58f, .09f), .6f);
            Material gold = Material("DaggerGold", new Color(.95f, .66f, .15f), .7f);
            Material glow = Material("WarningAmber", new Color(1f, .18f, .015f), .2f, 3f);
            var root = new GameObject("Sheer Heart Attack");
            root.SetActive(false);
            var hazard = root.AddComponent<SheerHeartAttack>();
            var model = new GameObject("Model").transform;
            model.SetParent(root.transform, false);
            Part(model, "ArmoredCore", PrimitiveType.Sphere, new Vector3(0,.62f,0), new Vector3(1.22f,1.05f,1.48f), blue);
            // Layered blue armor, with dark gaps between the panels.
            for (int ring = 0; ring < 3; ring++)
                for (int side = 0; side < 8; side++)
                {
                    float a = side * Mathf.PI / 4f;
                    float radius = ring == 2 ? .40f : .51f;
                    var plate = Part(model, "Armor_"+ring+"_"+side, PrimitiveType.Cube,
                        new Vector3(Mathf.Sin(a)*radius, .35f + ring*.27f, Mathf.Cos(a)*radius*1.2f),
                        new Vector3(.39f,.29f,.18f), blue);
                    plate.localRotation = Quaternion.Euler(-ring*12f, side*45f, 0);
                }
            Part(model,"FuseCollar",PrimitiveType.Cylinder,new Vector3(0,1.09f,0),new Vector3(.56f,.07f,.56f),green);
            Part(model,"FuseWell",PrimitiveType.Cylinder,new Vector3(0,1.17f,0),new Vector3(.43f,.025f,.43f),dark);
            Part(model,"DetonatorBolt",PrimitiveType.Cylinder,new Vector3(0,1.28f,0),new Vector3(.14f,.1f,.14f),gold);
            Part(model,"ArmedLamp",PrimitiveType.Sphere,new Vector3(0,1.39f,0),Vector3.one*.13f,glow);
            var rollers = new List<Transform>();
            for(int side=-1;side<=1;side+=2)
            {
                Part(model,"ContinuousTrack",PrimitiveType.Cube,new Vector3(side*.61f,.25f,0),new Vector3(.28f,.41f,1.29f),dark);
                for(int i=0;i<4;i++)
                {
                    var wheel = Part(model,"RoadWheel",PrimitiveType.Cylinder,new Vector3(side*.77f,.25f,-.45f+i*.3f),new Vector3(.33f,.035f,.33f),dark);
                    wheel.localRotation=Quaternion.Euler(0,0,90);
                    rollers.Add(wheel);
                }
                for(int i=0;i<9;i++)
                {
                    float z=-.57f+i*.14f;
                    Part(model,"TrackTread",PrimitiveType.Cube,new Vector3(side*.61f,.045f,z),new Vector3(.3f,.07f,.065f),green);
                    Part(model,"TrackTread",PrimitiveType.Cube,new Vector3(side*.61f,.46f,z),new Vector3(.3f,.06f,.065f),dark);
                }
                Part(model,"TrackFender",PrimitiveType.Cube,new Vector3(side*.62f,.53f,0),new Vector3(.4f,.13f,1.42f),blue);
            }
            // Face looks down -Z, toward the runner.
            Part(model,"SkullCranium",PrimitiveType.Sphere,new Vector3(0,.73f,-.69f),new Vector3(.55f,.59f,.19f),bone);
            for(int side=-1;side<=1;side+=2)
            {
                var horn=Part(model,"SkullHorn",PrimitiveType.Capsule,new Vector3(side*.24f,.96f,-.68f),new Vector3(.13f,.15f,.1f),bone);
                horn.localRotation=Quaternion.Euler(0,0,-side*18);
                var eye=Part(model,"EyeSocket",PrimitiveType.Sphere,new Vector3(side*.145f,.76f,-.788f),new Vector3(.18f,.16f,.055f),dark);
                eye.localRotation=Quaternion.Euler(0,0,side*18);
                Part(model,"Cheekbone",PrimitiveType.Sphere,new Vector3(side*.23f,.60f,-.735f),new Vector3(.13f,.2f,.13f),bone);
            }
            Part(model,"NoseCavity",PrimitiveType.Sphere,new Vector3(0,.65f,-.799f),new Vector3(.085f,.1f,.03f),dark);
            Part(model,"OpenMouth",PrimitiveType.Sphere,new Vector3(0,.47f,-.72f),new Vector3(.38f,.32f,.18f),dark);
            Part(model,"LowerJaw",PrimitiveType.Cube,new Vector3(0,.31f,-.72f),new Vector3(.36f,.07f,.15f),bone);
            for(int i=0;i<7;i++)
            {
                Part(model,"UpperTooth",PrimitiveType.Capsule,new Vector3((i-3)*.047f,.572f,-.818f),new Vector3(.04f,.045f,.04f),bone);
                Part(model,"LowerTooth",PrimitiveType.Capsule,new Vector3((i-3)*.043f,.354f,-.805f),new Vector3(.037f,.035f,.04f),bone);
            }
            Part(model,"ForeheadDagger",PrimitiveType.Cube,new Vector3(0,.94f,-.803f),new Vector3(.027f,.21f,.025f),gold);
            Part(model,"DaggerGuard",PrimitiveType.Cube,new Vector3(0,.98f,-.82f),new Vector3(.12f,.026f,.025f),gold);
            var collider=root.AddComponent<BoxCollider>();
            collider.isTrigger=true;
            collider.center=new Vector3(0,.52f,0);
            collider.size=new Vector3(1.38f,1.04f,1.42f);
            var body=root.AddComponent<Rigidbody>();
            body.isKinematic=true;
            body.useGravity=false;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            int layer=LayerMask.NameToLayer("Obstacle");
            if(layer>=0) root.layer=layer;
            var exhaust=Particles(root.transform,"ExhaustSparks",glow,false);
            exhaust.transform.localPosition=new Vector3(0,.45f,.75f);
            var explosion=Particles(root.transform,"Explosion",glow,true);
            var impactShader=Shader.Find("ActiveMotion3D/SheerHeartAttack/ImpactParticles");
            if(impactShader != null)
            {
                string impactPath=Folder+"/ImpactParticles.mat";
                var impactMaterial=AssetDatabase.LoadAssetAtPath<Material>(impactPath);
                if(impactMaterial==null)
                {
                    impactMaterial=new Material(impactShader);
                    AssetDatabase.CreateAsset(impactMaterial,impactPath);
                }
                explosion.GetComponent<ParticleSystemRenderer>().sharedMaterial=impactMaterial;
            }
            explosion.transform.localPosition=new Vector3(0,.55f,0);
            hazard.Configure(model,rollers.ToArray(),exhaust,explosion);
            hazard.Setup(Vector3.zero,1,ObstacleType.SheerHeartAttack);
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab.GetComponent<SheerHeartAttack>(),out string guid,out long id);
            Directory.CreateDirectory("Temp/SheerHeartAttack");
            File.WriteAllText("Temp/SheerHeartAttack/asset.txt",guid+"\n"+id);
            Debug.Log("Sheer Heart Attack prefab built: "+guid+" / "+id);
        }

        private static Transform Part(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
        {
            var part=GameObject.CreatePrimitive(type);
            part.name=name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent,false);
            part.transform.localPosition=position;
            part.transform.localScale=scale;
            part.GetComponent<Renderer>().sharedMaterial=material;
            return part.transform;
        }

        private static Material Material(string name,Color color,float metallic,float emission=0)
        {
            string path=Folder+"/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat!=null)return mat;
            mat=new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color=color;
            mat.SetFloat("_Metallic",metallic);
            mat.SetFloat("_Smoothness",.5f);
            if(emission>0){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*emission);}
            AssetDatabase.CreateAsset(mat,path);
            return mat;
        }

        private static ParticleSystem Particles(Transform parent,string name,Material material,bool burst)
        {
            var obj=new GameObject(name);
            obj.transform.SetParent(parent,false);
            var ps=obj.AddComponent<ParticleSystem>();
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;
            main.playOnAwake=false;main.loop=!burst;main.duration=burst?.65f:1f;
            main.startLifetime=burst?new ParticleSystem.MinMaxCurve(.3f,.85f):new ParticleSystem.MinMaxCurve(.15f,.35f);
            main.startSpeed=burst?new ParticleSystem.MinMaxCurve(2f,6f):new ParticleSystem.MinMaxCurve(.3f,1.2f);
            main.startSize=burst?new ParticleSystem.MinMaxCurve(.08f,.25f):new ParticleSystem.MinMaxCurve(.025f,.065f);
            main.startColor=new ParticleSystem.MinMaxGradient(new Color(1,.18f,.01f),new Color(1,.85f,.15f));
            main.maxParticles=burst?80:24;
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission;
            emission.rateOverTime=burst?0:22;
            if(burst) emission.SetBursts(new[]{new ParticleSystem.Burst(0f,65)});
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.12f;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1f,AnimationCurve.Linear(0,1,1,0));
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
            return ps;
        }
    }
}
