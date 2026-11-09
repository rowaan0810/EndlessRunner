using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ActiveMotion3D.EndlessRunner.Obstacles;
using ActiveMotion3D.EndlessRunner.Core;
using ActiveMotion3D.EndlessRunner.Player;

namespace ActiveMotion3D.EditorTools
{
    public static class MenacingRoadSignsValidation
    {
        [MenuItem("Tools/Active Motion 3D/Validate Menacing Road Signs")]
        public static void Validate()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(MenacingRoadSignsBuilder.PrefabPath);
            Check(prefab!=null,"V2 prefab exists");
            var preview=new PreviewRenderUtility();
            GameObject model=null,spawnerObject=null,managerObject=null,playerObject=null;
            float originalTimeScale=Time.timeScale;
            var randomState=UnityEngine.Random.state;
            try
            {
                Directory.CreateDirectory("Docs/MenacingRoadSigns");
                model=UnityEngine.Object.Instantiate(prefab);preview.AddSingleGO(model);model.SetActive(true);
                var sign=model.GetComponent<MenacingRoadSigns>();
                Check(sign.Type==ObstacleType.MenacingRoadSigns,"Type serialized");
                Check(model.GetComponentsInChildren<Collider>(true).Length==1,"One stable collision volume");
                Check(model.GetComponent<BoxCollider>().isTrigger,"Trigger collider");
                Check(model.GetComponent<BoxCollider>().size.x<3.2f,"Lane clearance");
                Check(model.GetComponent<Rigidbody>().isKinematic,"Kinematic rigidbody");
                Check(Resources.Load<GameObject>("MenacingRoadSigns/SkyLettering")==null,"Removed lettering resource");
                Check(typeof(ObstacleSpawner).GetMethod("UpdateSkyLettering",BindingFlags.Instance|BindingFlags.NonPublic)==null,"No lettering spawner");
                preview.camera.transform.position=new Vector3(.8f,2.8f,-8.5f);
                preview.camera.transform.LookAt(new Vector3(0,1.85f,0));
                preview.camera.fieldOfView=34;preview.camera.nearClipPlane=.05f;preview.camera.farClipPlane=60;
                preview.camera.clearFlags=CameraClearFlags.SolidColor;
                preview.camera.backgroundColor=new Color(.83f,.81f,.75f);
                preview.lights[0].intensity=1.2f;preview.lights[1].intensity=.8f;
                var sheet=new Texture2D(2048,640,TextureFormat.RGB24,false);
                for(int i=0;i<4;i++)
                {
                    sign.SelectVariant(i);
                    Check(sign.VariantIndex==i,"Variant selection");
                    int active=0;foreach(Transform child in model.transform)if(child.gameObject.activeSelf)active++;
                    Check(active==1,"Exactly one separate sign visible");
                    if(i==3) Check(model.transform.GetChild(3).Find("Single sign board").localPosition.y>2.5f,"Hovering sign is above its separate pole");
                    preview.BeginStaticPreview(new Rect(0,0,512,640));preview.Render(true);
                    var texture=preview.EndStaticPreview();
                    sheet.SetPixels(i*512,0,512,640,texture.GetPixels());
                    File.WriteAllBytes("Docs/MenacingRoadSigns/variant-"+(char)('A'+i)+".png",texture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(texture);
                }
                sheet.Apply();File.WriteAllBytes("Docs/MenacingRoadSigns/approved-variants.png",sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);model.SetActive(false);
                spawnerObject=new GameObject("Sign validation spawner");preview.AddSingleGO(spawnerObject);
                var spawner=spawnerObject.AddComponent<ObstacleSpawner>();
                var spawn=typeof(ObstacleSpawner).GetMethod("SpawnMenacingRoadSigns",BindingFlags.Instance|BindingFlags.NonPublic);
                Check((bool)spawn.Invoke(spawner,null),"V2 resource spawns");
                var spawned=spawnerObject.GetComponentInChildren<MenacingRoadSigns>();
                Check(Mathf.Approximately(spawned.transform.position.x,(spawned.Lane-1)*3.2f),"Spawn lane alignment");
                int id=spawned.GetInstanceID();
                spawner.ResetSpawner();Check(!spawned.gameObject.activeSelf,"Restart clears signs");
                spawn.Invoke(spawner,null);Check(spawnerObject.GetComponentInChildren<MenacingRoadSigns>().GetInstanceID()==id,"Sign pooling");
                // Exercise the real player collision handler for each of the four variants.
                Check(GameManager.Instance==null,"Run validation outside an active game");
                managerObject=new GameObject("Sign collision validation manager");preview.AddSingleGO(managerObject);
                var manager=managerObject.AddComponent<GameManager>();
                typeof(GameManager).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,null);
                playerObject=new GameObject("Sign collision validation player");preview.AddSingleGO(playerObject);
                var player=playerObject.AddComponent<PlayerCollision>();
                var capsule=playerObject.AddComponent<CapsuleCollider>();capsule.height=1.8f;capsule.radius=.3f;capsule.center=new Vector3(0,.9f,0);
                int playerLayer=LayerMask.NameToLayer("Player");if(playerLayer>=0)playerObject.layer=playerLayer;
                var hit=typeof(PlayerCollision).GetMethod("OnTriggerEnter",BindingFlags.Instance|BindingFlags.NonPublic);
                var trigger=model.GetComponent<BoxCollider>();
                Check(!Physics.GetIgnoreLayerCollision(playerObject.layer,model.layer),"Player/sign layers permit collision");
                for(int i=0;i<4;i++)
                {
                    model.SetActive(true);sign.SelectVariant(i);
                    Physics.SyncTransforms();
                    Check(Physics.ComputePenetration(capsule,Vector3.zero,Quaternion.identity,
                        trigger,Vector3.zero,Quaternion.identity,out _,out _),"Sign collision volume overlaps runner");
                    Check(!Physics.ComputePenetration(capsule,new Vector3(3.2f,0,0),Quaternion.identity,
                        trigger,Vector3.zero,Quaternion.identity,out _,out _),"Adjacent lane stays clear");
                    manager.StartGame();player.IsShielded=false;
                    hit.Invoke(player,new object[]{trigger});
                    Check(manager.State==GameState.GameOver,"Unshielded impact ends run for variant "+i);
                    manager.StartGame();model.SetActive(true);player.IsShielded=true;
                    hit.Invoke(player,new object[]{trigger});
                    Check(manager.State==GameState.Playing && !player.IsShielded && !model.activeSelf,"Shield absorbs sign collision for variant "+i);
                }
                File.WriteAllText("Docs/MenacingRoadSigns/validation-v2.txt","PASS: lettering spawner and resource removed; four separate variants; trigger collider and kinematic body; player collision layers; runner overlap and adjacent-lane clearance; all four variants cause Game Over without a shield and consume a shield on impact; resource loading, restart and pool reuse. Targeted Editor validation; not a full Play Mode or standalone run.\n");
                Debug.Log("Menacing Road Signs V2 validation PASS");
            }
            finally
            {
                UnityEngine.Random.state=randomState;
                if(model!=null) UnityEngine.Object.DestroyImmediate(model);
                if(playerObject!=null) UnityEngine.Object.DestroyImmediate(playerObject);
                if(managerObject!=null) UnityEngine.Object.DestroyImmediate(managerObject);
                Time.timeScale=originalTimeScale;
                if(spawnerObject!=null) UnityEngine.Object.DestroyImmediate(spawnerObject);
                preview.Cleanup();
            }
        }
        private static void Check(bool condition,string message)
        {
            if(!condition) throw new InvalidOperationException("Menacing Road Signs V2: "+message);
        }
    }
}
