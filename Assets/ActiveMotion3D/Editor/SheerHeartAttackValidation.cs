using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ActiveMotion3D.EndlessRunner.Core;
using ActiveMotion3D.EndlessRunner.Obstacles;
using ActiveMotion3D.EndlessRunner.Player;

namespace ActiveMotion3D.EditorTools
{
    public static class SheerHeartAttackValidation
    {
        [MenuItem("Tools/Active Motion 3D/Validate Sheer Heart Attack")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Run validation outside Play Mode.");
            SheerHeartAttackBuilder.Build();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SheerHeartAttackBuilder.PrefabPath);
            var preview = new PreviewRenderUtility();
            GameObject instance = null, managerObject = null, spawnerObject = null, playerObject = null;
            try
            {
                Check(prefab != null, "Prefab exists");
                instance = UnityEngine.Object.Instantiate(prefab);
                preview.AddSingleGO(instance);
                var hazard = instance.GetComponent<SheerHeartAttack>();
                Check(hazard.Type == ObstacleType.SheerHeartAttack, "Serialized obstacle type");
                Check(hazard.GetComponent<BoxCollider>().isTrigger, "Trigger collider");
                Check(hazard.GetComponent<Rigidbody>().isKinematic, "Kinematic rigidbody");
                Check(instance.GetComponentsInChildren<Renderer>().Length > 60, "Editable reference-inspired model");

                // Render the authored model, without touching or saving the open scene.
                preview.camera.transform.position = new Vector3(2.2f,1.9f,-3.2f);
                preview.camera.transform.LookAt(new Vector3(0,.65f,0));
                preview.camera.nearClipPlane=.05f;
                preview.camera.farClipPlane=20f;
                preview.camera.fieldOfView=35;
                preview.camera.backgroundColor=new Color(.065f,.08f,.12f);
                preview.camera.clearFlags=CameraClearFlags.SolidColor;
                preview.lights[0].intensity=1.4f;
                preview.lights[0].transform.rotation=Quaternion.Euler(35,35,0);
                preview.lights[1].intensity=.8f;
                preview.ambientColor=new Color(.45f,.45f,.45f);
                preview.BeginStaticPreview(new Rect(0,0,768,768));
                preview.Render(true);
                var texture=preview.EndStaticPreview();
                Directory.CreateDirectory("Docs/SheerHeartAttack");
                File.WriteAllBytes("Docs/SheerHeartAttack/preview.png",texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);

                Check(GameManager.Instance == null, "Validation runs outside a live game");
                managerObject=new GameObject("ValidationManager");preview.AddSingleGO(managerObject);
                var manager=managerObject.AddComponent<GameManager>();
                Call(manager,"Awake");
                manager.StartGame();
                playerObject=new GameObject("ValidationPlayer");preview.AddSingleGO(playerObject);
                var player=playerObject.AddComponent<PlayerController>();
                Set(player,"currentLane",2);
                var collision=playerObject.AddComponent<PlayerCollision>();
                spawnerObject=new GameObject("ValidationSpawner");preview.AddSingleGO(spawnerObject);
                var spawner=spawnerObject.AddComponent<ObstacleSpawner>();
                Set(spawner,"spawnSheerHeartAttack",true);
                Set(spawner,"sheerHeartAttackTarget",player);
                Set(spawner,"sheerHeartAttackPrefab",prefab.GetComponent<SheerHeartAttack>());
                Set(spawner,"nextSheerHeartAttackTime",0f);
                Check((bool)Call(spawner,"UpdateSheerHeartAttack"),"Encounter reserves track");
                var spawned=(SheerHeartAttack)Get(spawner,"sheerHeartAttack");
                Check(spawned != null && spawned.Lane==2 && Mathf.Approximately(spawned.transform.position.x,3.2f),"Targets player lane once");
                Set(player,"currentLane",0);
                Call(spawner,"UpdateSheerHeartAttack");
                Check(spawned.Lane==2,"Lane commitment allows dodge");
                spawned.Setup(new Vector3(0,0,10),1,ObstacleType.SheerHeartAttack);
                Call(spawned,"OnEnable");
                collision.IsShielded=true;
                Call(collision,"OnTriggerEnter",spawned.GetComponent<Collider>());
                Check(!collision.IsShielded && manager.State==GameState.Playing,"Shield absorbs explosive");
                Check(!spawned.GetComponent<Collider>().enabled,"Detonation disables repeat collisions");
                spawner.ResetSpawner();
                Check(!spawned.gameObject.activeSelf,"Restart recycles explosive");
                Check((float)Get(spawner,"nextSheerHeartAttackTime")==16f,"Restart restores first delay");
                spawned.Setup(Vector3.zero,1,ObstacleType.SheerHeartAttack);
                Call(spawned,"OnEnable");
                Check(spawned.GetComponent<Collider>().enabled,"Reuse restores collision");
                manager.PauseGame();
                Vector3 before=spawned.transform.position;
                Call(spawned,"Update");
                Check(spawned.transform.position==before,"Pause stops movement");
                manager.ResumeGame();
                Call(collision,"OnTriggerEnter",spawned.GetComponent<Collider>());
                Check(manager.State==GameState.GameOver,"Unshielded impact ends run");
                Check(manager.GameOverPresentationDelay >= .5f,"Fatal explosion gets visible presentation time");
                var burst=(ParticleSystem)Get(spawned,"explosion");
                Check(burst.main.useUnscaledTime,"Impact particles ignore frozen game time");
                instance.SetActive(false);
                Time.timeScale=0f;
                burst.Simulate(.12f,true,false);
                Check(burst.particleCount>0,"Visible burst particles after fatal collision");
                Call(spawned,"Update");
                Check(spawned.gameObject.activeSelf,"Impact stays active while particles live");
                preview.BeginStaticPreview(new Rect(0,0,768,768));
                preview.Render(true);
                var impact=preview.EndStaticPreview();
                File.WriteAllBytes("Docs/SheerHeartAttack/impact.png",impact.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(impact);
                Time.timeScale=1f;
                var results=new GameObject("ValidationResults");preview.AddSingleGO(results);
                results.SetActive(false);
                var ui=spawnerObject.AddComponent<ActiveMotion3D.EndlessRunner.UI.GameOverScreen>();
                Set(ui,"panel",results);
                var reveal=(System.Collections.IEnumerator)Call(ui,"ShowAfterImpact",manager.GameOverPresentationDelay);
                Check(reveal.MoveNext() && reveal.Current is WaitForSecondsRealtime && !results.activeSelf,
                    "Results wait in real time while the explosion is visible");
                reveal.MoveNext();
                Check(results.activeSelf,"Results appear after impact interval");
                Call(ui,"Hide");
                var staleReveal=(System.Collections.IEnumerator)Call(ui,"ShowAfterImpact",.85f);
                staleReveal.MoveNext();
                manager.StartGame();
                staleReveal.MoveNext();
                Check(!results.activeSelf,"Restart prevents stale delayed results");
                manager.TriggerGameOver();
                Check(manager.GameOverPresentationDelay==0f,"Ordinary collisions keep immediate results");
                File.WriteAllText("Docs/SheerHeartAttack/validation.txt","PASS: prefab, model render, lane targeting and commitment, encounter reservation, shield, detonation, restart, reuse, pause, fatal collision, visible impact particles, unscaled particle time, impact presentation delay, ordinary collision timing.\nEditor component smoke checks and impact render; no webcam or standalone build exercised.");
                Debug.Log("Sheer Heart Attack validation PASS");
            }
            catch(Exception e)
            {
                Directory.CreateDirectory("Docs/SheerHeartAttack");
                File.WriteAllText("Docs/SheerHeartAttack/validation.txt","FAIL: "+e);
                throw;
            }
            finally
            {
                if(managerObject!=null){Call(managerObject.GetComponent<GameManager>(),"OnDestroy");}
                preview.Cleanup();
            }
        }
        private static object Call(object obj,string method,params object[] args) => obj.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(obj,args);
        private static void Set(object obj,string field,object value) => obj.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
        private static object Get(object obj,string field) => obj.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(obj);
        private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    }
}
