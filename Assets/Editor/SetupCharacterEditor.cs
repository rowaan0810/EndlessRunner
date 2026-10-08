using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace EndlessRunner.EditorScripts
{
    public class SetupCharacterEditor : Editor
    {
        private const string characterFolder = "Assets/Art/Character";
        private const string controllerPath = characterFolder + "/PlayerAnimController.controller";

        [MenuItem("Tools/Endless Runner/Setup 3D Character")]
        public static void SetupCharacter()
        {
            if (!Directory.Exists(characterFolder))
            {
                Debug.LogError($"Could not find folder {characterFolder}. Please ensure the FBX files are there.");
                return;
            }

            // 1. Setup Humanoid Rigs
            SetupHumanoidRigs();

            // 2. Build Animator Controller
            AnimatorController controller = BuildAnimatorController();

            // 3. Update Player Object in Scene
            if (controller != null)
            {
                UpdatePlayerInScene(controller);
            }

            Debug.Log("Character setup complete!");
        }

        private static void SetupHumanoidRigs()
        {
            string[] fbxFiles = Directory.GetFiles(characterFolder, "*.fbx");
            foreach (string file in fbxFiles)
            {
                ModelImporter importer = AssetImporter.GetAtPath(file) as ModelImporter;
                if (importer != null && importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    
                    // If it's the animation files, enable loop time on the clips
                    if (file.ToLower().Contains("run") || file.ToLower().Contains("jump") || file.ToLower().Contains("roll"))
                    {
                        ModelImporterClipAnimation[] defaultClips = importer.defaultClipAnimations;
                        if (defaultClips != null && defaultClips.Length > 0)
                        {
                            foreach (var clip in defaultClips)
                            {
                                clip.loopTime = file.ToLower().Contains("run"); // Only loop run
                                clip.lockRootHeightY = true;
                                clip.lockRootPositionXZ = true;
                                clip.lockRootRotation = true;
                            }
                            importer.clipAnimations = defaultClips;
                        }
                    }

                    importer.SaveAndReimport();
                    Debug.Log($"Configured Humanoid Rig for: {Path.GetFileName(file)}");
                }
            }
        }

        private static AnimatorController BuildAnimatorController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller != null)
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }

            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("isGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("isDucking", AnimatorControllerParameterType.Bool);

            var rootStateMachine = controller.layers[0].stateMachine;

            // Extract clips
            AnimationClip runClip = GetClipByName("Run");
            AnimationClip jumpClip = GetClipByName("Jump");
            AnimationClip rollClip = GetClipByName("Roll");

            if (runClip == null) Debug.LogWarning("Could not find Running animation clip.");
            if (jumpClip == null) Debug.LogWarning("Could not find Jumping animation clip.");
            if (rollClip == null) Debug.LogWarning("Could not find Roll/Slide animation clip.");

            // Create States
            var runState = rootStateMachine.AddState("Run");
            runState.motion = runClip;
            rootStateMachine.defaultState = runState;

            var jumpState = rootStateMachine.AddState("Jump");
            jumpState.motion = jumpClip;

            var rollState = rootStateMachine.AddState("Roll");
            rollState.motion = rollClip;

            // Transitions: Any -> Jump
            var jumpTrans = rootStateMachine.AddAnyStateTransition(jumpState);
            jumpTrans.AddCondition(AnimatorConditionMode.IfNot, 0, "isGrounded");
            jumpTrans.duration = 0.15f;

            // Jump -> Run
            var jumpToRun = jumpState.AddTransition(runState);
            jumpToRun.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
            jumpToRun.duration = 0.1f;

            // Any -> Roll
            var rollTrans = rootStateMachine.AddAnyStateTransition(rollState);
            rollTrans.AddCondition(AnimatorConditionMode.If, 0, "isDucking");
            rollTrans.duration = 0.15f;

            // Roll -> Run
            var rollToRun = rollState.AddTransition(runState);
            rollToRun.AddCondition(AnimatorConditionMode.IfNot, 0, "isDucking");
            rollToRun.duration = 0.2f;

            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip GetClipByName(string searchHint)
        {
            string[] fbxFiles = Directory.GetFiles(characterFolder, "*.fbx");
            foreach (string file in fbxFiles)
            {
                if (file.ToLower().Contains(searchHint.ToLower()) || 
                   (searchHint == "Roll" && file.ToLower().Contains("stand to roll")))
                {
                    Object[] assets = AssetDatabase.LoadAllAssetsAtPath(file);
                    foreach (Object asset in assets)
                    {
                        if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                        {
                            return clip;
                        }
                    }
                }
            }
            return null;
        }

        private static void UpdatePlayerInScene(AnimatorController controller)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                Debug.Log("Player GameObject not found. Creating a new one...");
                playerObj = new GameObject("Player");
                playerObj.transform.position = Vector3.zero;
                try { playerObj.tag = "Player"; } catch { }
            }
            
            // 1. Disable Capsule
            Transform capsule = playerObj.transform.Find("Capsule");
            if (capsule != null)
            {
                capsule.gameObject.SetActive(false);
            }
            else
            {
                var mesh = playerObj.GetComponent<MeshRenderer>();
                if (mesh != null) mesh.enabled = false;
            }

            // 2. Setup Animator on Root
            Animator animator = playerObj.GetComponent<Animator>();
            if (animator == null) animator = playerObj.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            // 3. Clear old character models
            var oldSelector = playerObj.GetComponent<EndlessRunner.Player.CharacterSelector>();
            if (oldSelector != null) Object.DestroyImmediate(oldSelector);
            
            // Remove any existing children that are models (we assume they don't have our core scripts)
            for (int i = playerObj.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = playerObj.transform.GetChild(i);
                if (child.name != "Capsule") Object.DestroyImmediate(child.gameObject);
            }

            // 4. Add all new Character Models
            EndlessRunner.Player.CharacterSelector selector = playerObj.AddComponent<EndlessRunner.Player.CharacterSelector>();
            System.Collections.Generic.List<GameObject> modelsList = new System.Collections.Generic.List<GameObject>();
            System.Collections.Generic.List<Avatar> avatarsList = new System.Collections.Generic.List<Avatar>();

            string[] fbxFiles = Directory.GetFiles(characterFolder, "*.fbx");
            foreach (string file in fbxFiles)
            {
                string lower = file.ToLower();
                // Skip animation files
                if (lower.Contains("run") || lower.Contains("jump") || lower.Contains("roll")) continue;

                GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(file);
                if (modelPrefab == null) continue;

                GameObject modelInstance = PrefabUtility.InstantiatePrefab(modelPrefab, playerObj.transform) as GameObject;
                modelInstance.name = Path.GetFileNameWithoutExtension(file);
                
                // Align feet to capsule bottom (Player pivot is already at ground level)
                modelInstance.transform.localPosition = Vector3.zero;

                // Fix material shader for URP (so it's not colorless/white)
                Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>();
                Shader urpShader = EndlessRunner.SceneSetup.FindWorkingShader();
                foreach (var rnd in renderers)
                {
                    if (rnd.sharedMaterials == null) continue;
                    foreach (var mat in rnd.sharedMaterials)
                    {
                        if (mat != null) 
                        {
                            // Try to grab the texture from either MainTex or BaseMap
                            Texture tex = null;
                            if (mat.HasProperty("_BaseMap")) tex = mat.GetTexture("_BaseMap");
                            if (tex == null && mat.HasProperty("_MainTex")) tex = mat.GetTexture("_MainTex");
                            
                            mat.shader = urpShader;
                            
                            // Re-apply the texture to URP's _BaseMap property
                            if (tex != null && mat.HasProperty("_BaseMap"))
                            {
                                mat.SetTexture("_BaseMap", tex);
                            }
                            // Also ensure the base color is white so it doesn't tint the texture grey
                            if (mat.HasProperty("_BaseColor"))
                            {
                                mat.SetColor("_BaseColor", Color.white);
                            }

                            UnityEditor.EditorUtility.SetDirty(mat); // Ensure the material saves
                        }
                    }
                }

                // Get avatar
                Animator modelAnimator = modelInstance.GetComponent<Animator>();
                if (modelAnimator != null)
                {
                    avatarsList.Add(modelAnimator.avatar);
                    Object.DestroyImmediate(modelAnimator); // Prevent double animators
                }
                else
                {
                    avatarsList.Add(null);
                }

                // Hide initially
                modelInstance.SetActive(modelsList.Count == 0); 
                modelsList.Add(modelInstance);
            }

            if (modelsList.Count > 0)
            {
                animator.avatar = avatarsList[0];
            }

            selector.models = modelsList.ToArray();
            selector.avatars = avatarsList.ToArray();

            // 5. Hook up PlayerAnimator
            var playerAnim = playerObj.GetComponent<EndlessRunner.Player.PlayerAnimator>();
            if (playerAnim != null)
            {
                playerAnim.enabled = true;
            }
            
            // Mark scene as dirty so the changes are saved
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(playerObj.scene);

            Debug.Log($"Player updated successfully in the scene with {modelsList.Count} character models!");
        }
    }
}
