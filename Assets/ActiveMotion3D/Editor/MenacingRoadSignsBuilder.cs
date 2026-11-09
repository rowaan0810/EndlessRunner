using System.IO;
using UnityEditor;
using UnityEngine;
using ActiveMotion3D.EndlessRunner.Obstacles;

namespace ActiveMotion3D.EditorTools
{
    public static class MenacingRoadSignsBuilder
    {
        public const string Folder = "Assets/ActiveMotion3D/Art/Obstacles/MenacingRoadSigns";
        public const string PrefabPath = Folder + "/Resources/MenacingRoadSigns/MenacingRoadSignsV2.prefab";
        private const string Revision = "2.3";
        private static bool IsCurrent => File.Exists(PrefabPath) && File.Exists(Folder + "/design-version.txt") && File.ReadAllText(Folder + "/design-version.txt") == Revision;
        private const string RoutePath = "Assets/ActiveMotion3D/Art/Environment/Morioh/DesignKit/Generated/Models/RouteMarker.prefab";

        [InitializeOnLoadMethod]
        private static void EnsureAsset()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !IsCurrent) Build();
            };
        }

        [MenuItem("Tools/Active Motion 3D/Build Menacing Road Signs")]
        public static void Build()
        {
            if (IsCurrent) return; // Preserve hand edits once this design revision is generated.
            var route = AssetDatabase.LoadAssetAtPath<GameObject>(RoutePath);
            if (route == null) throw new FileNotFoundException("Existing RouteMarker sign required", RoutePath);
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            AssetDatabase.Refresh();
            var ink = Material("Ink", new Color(.055f,.025f,.105f));
            var cream = Material("RoadSignIvory", new Color(1f,.94f,.69f));
            var blue = Material("TurnBlue", new Color(.10f,.29f,.60f));
            var red = Material("StopRed", new Color(.70f,.20f,.29f));
            var yellow = Material("WarningYellow", new Color(1f,.70f,.12f));
            var lilac = Material("FloatingLavender", new Color(.64f,.47f,.78f));
            var metal = Material("SignMetal",new Color(.48f,.49f,.52f));
            var root = new GameObject("Menacing Road Signs - Four Variants");
            try
            {
                root.SetActive(false);
                var obstacle = root.AddComponent<MenacingRoadSigns>();
                var variants = new Transform[4];
                string[] names={"A - Wrong Turn","B - Dead End","C - Bent Direction","D - Floating Warning"};
                var poleMesh=ExtractPole(route);
                for(int i=0;i<4;i++)
                {
                    var variant=new GameObject(names[i]).transform;
                    variant.SetParent(root.transform,false);variants[i]=variant;
                    Box(variant,"Foot",new Vector3(0,.1f,0),new Vector3(.57f,.2f,.5f),ink);
                    var pole=new GameObject("Reused RouteMarker pole",typeof(MeshFilter),typeof(MeshRenderer));
                    pole.transform.SetParent(variant,false);
                    pole.GetComponent<MeshFilter>().sharedMesh=poleMesh;
                    pole.GetComponent<Renderer>().sharedMaterial=metal;
                    pole.transform.localScale=new Vector3(1.9f,i==3?.40f:.95f,1.9f);
                    pole.transform.localRotation=Quaternion.Euler(0,0,i==1?-9:i==0?5:0);
                    var board=new GameObject("Single sign board").transform;
                    board.SetParent(variant,false);
                    board.localPosition=new Vector3(i==1?.35f:0,2.75f,-.12f);
                    board.localRotation=Quaternion.Euler(0,0,i==0?-7:i==1?-8:i==3?6:0);
                    Vector2[] shape;
                    Material face;
                    if(i==0) {shape=Circle(1.05f,48);face=blue;}
                    else if(i==1) {shape=new[]{new Vector2(-1.06f,.83f),new Vector2(1.06f,.83f),new Vector2(0,-1.05f)};face=red;}
                    else if(i==2) {shape=new[]{new Vector2(0,1.12f),new Vector2(1.08f,0),new Vector2(0,-1.12f),new Vector2(-1.08f,0)};face=yellow;}
                    else {shape=new[]{new Vector2(-1.08f,-.70f),new Vector2(1.08f,-.70f),new Vector2(1.08f,.70f),new Vector2(-1.08f,.70f)};face=lilac;}
                    Polygon(board,names[i]+" outline",shape,ink,1.06f,.08f);
                    Polygon(board,names[i]+" rim",shape,metal,1.01f,0);
                    Polygon(board,names[i]+" face",shape,face,.94f,-.025f);
                    if(i==1) Box(board,"Dead end bar",new Vector3(0,.07f,-.07f),new Vector3(1.10f,.25f,.035f),cream);
                    else
                    {
                        Vector2[] path=i==0 ? new[]{new Vector2(-.64f,-.40f),new Vector2(-.12f,-.40f),new Vector2(-.12f,.13f),new Vector2(.53f,.13f)}
                            : i==2 ? new[]{new Vector2(-.12f,-.72f),new Vector2(.19f,-.32f),new Vector2(-.25f,.03f),new Vector2(.22f,.40f),new Vector2(.22f,.55f)}
                            : new[]{new Vector2(-.52f,-.40f),new Vector2(-.52f,.17f),new Vector2(-.30f,.43f),new Vector2(.19f,.43f),new Vector2(.48f,.17f),new Vector2(.48f,-.30f)};
                        for(int j=1;j<path.Length;j++) Stroke(board,path[j-1],path[j],.23f,i==0?cream:ink);
                        var tip=path[path.Length-1];var direction=(tip-path[path.Length-2]).normalized;var normal=new Vector2(-direction.y,direction.x);
                        Polygon(board,names[i]+" arrowhead",new[]{tip+direction*.28f,tip-direction*.23f+normal*.32f,tip-direction*.23f-normal*.32f},i==0?cream:ink,1,-.085f);
                    }
                    if(i!=1)
                    {
                        // Round the U-turn bends while keeping the directional sign silhouettes simple.
                        if(i==3)
                        {
                            Vector2[] joints={new Vector2(-.52f,.17f),new Vector2(-.30f,.43f),new Vector2(.19f,.43f),new Vector2(.48f,.17f)};
                            for(int j=0;j<joints.Length;j++)
                            {
                                var joint=new GameObject("U-turn joint").transform;joint.SetParent(board,false);
                                joint.localPosition=new Vector3(joints[j].x,joints[j].y,0);
                                Polygon(joint,"U-turn joint "+j,Circle(.115f,16),ink,1,-.09f);
                            }
                        }
                    }
                    for(int side=-1;side<=1;side+=2)
                        Box(board,"Mount bolt",new Vector3(0,side*(i==3?.58f:.78f),-.09f),new Vector3(.075f,.075f,.035f),metal);
                    // Sparse printed hatch marks give the flat plates a manga ink finish.
                    for(int h=0;h<3;h++)
                        Stroke(board,new Vector2(-.66f+h*.07f,.42f),new Vector2(-.55f+h*.07f,.58f),.012f,ink);
                }
                obstacle.Configure(variants,variants[3].Find("Single sign board"));
                var trigger=root.AddComponent<BoxCollider>();trigger.isTrigger=true;
                trigger.center=new Vector3(0,2f,0);trigger.size=new Vector3(2.45f,4f,.65f);
                var body=root.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
                body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
                int layer=LayerMask.NameToLayer("Obstacle");if(layer>=0) root.layer=layer;
                obstacle.Setup(Vector3.zero,1,ObstacleType.MenacingRoadSigns);
                root.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally { Object.DestroyImmediate(root); }
            MenacingRoadSignsValidation.Validate();
            File.WriteAllText(Folder + "/design-version.txt",Revision);
        }

        private static Mesh ExtractPole(GameObject route)
        {
            // Keep the narrow pole triangles from the existing sign model, discard its old board.
            var vertices=new System.Collections.Generic.List<Vector3>();
            var triangles=new System.Collections.Generic.List<int>();
            foreach(var filter in route.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;int offset=vertices.Count;
                var matrix=route.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                foreach(var v in mesh.vertices) vertices.Add(matrix.MultiplyPoint3x4(v));
                var indices=mesh.triangles;
                for(int t=0;t<indices.Length;t+=3)
                {
                    bool pole=true;
                    for(int k=0;k<3;k++) {var v=vertices[offset+indices[t+k]];pole &= Mathf.Abs(v.x)<.10f && Mathf.Abs(v.z)<.10f;}
                    if(pole) for(int k=0;k<3;k++) triangles.Add(offset+indices[t+k]);
                }
            }
            if(triangles.Count==0) throw new System.InvalidOperationException("RouteMarker pole mesh not found");
            var result=new Mesh {name="Reused RouteMarker pole"};result.SetVertices(vertices);result.SetTriangles(triangles,0);result.RecalculateNormals();result.RecalculateBounds();
            return SaveMesh(result,Folder+"/ReusedPoleV2.asset");
        }

        private static Vector2[] Circle(float radius,int count)
        {
            var result=new Vector2[count];for(int i=0;i<count;i++) {float a=i*Mathf.PI*2/count;result[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;}return result;
        }

        private static void Stroke(Transform parent,Vector2 a,Vector2 b,float width,Material material)
        {
            var d=b-a;var bar=Box(parent,"Graphic stroke",new Vector3((a.x+b.x)/2,(a.y+b.y)/2,-.07f),new Vector3(width,d.magnitude,.035f),material);
            bar.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg);
        }

        private static void Polygon(Transform parent,string name,Vector2[] points,Material material,float scale,float z)
        {
            var vertices=new Vector3[points.Length];for(int i=0;i<points.Length;i++)vertices[i]=new Vector3(points[i].x*scale,points[i].y*scale,0);
            var triangles=new int[(points.Length-2)*6];for(int i=0;i<points.Length-2;i++) {int n=i*6;triangles[n]=0;triangles[n+1]=i+1;triangles[n+2]=i+2;triangles[n+3]=i+2;triangles[n+4]=i+1;triangles[n+5]=0;}
            var mesh=new Mesh {name=name,vertices=vertices,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();
            mesh=SaveMesh(mesh,Folder+"/V2_"+name.Replace(' ','_')+".asset");
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(0,0,z);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material;
        }

        private static Mesh SaveMesh(Mesh mesh,string path)
        {
            mesh.name=Path.GetFileNameWithoutExtension(path);
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null) {AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;
        }

        private static Material Material(string name, Color color)
        {
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material,path);
            return material;
        }

        private static Transform Box(Transform parent,string name,Vector3 pos,Vector3 size,Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(box.GetComponent<Collider>());
            box.name=name; box.transform.SetParent(parent,false);
            box.transform.localPosition=pos; box.transform.localScale=size;
            box.GetComponent<Renderer>().sharedMaterial=material;
            return box.transform;
        }

    }
}
