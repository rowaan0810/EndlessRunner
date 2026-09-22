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
        private const string playerPrefabPath = "Assets/Prefabs/Player.prefab";

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

            // 3. Update Player Prefab
            if (controller != null)
            {
                UpdatePlayerPrefab(controller);
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

        private static void UpdatePlayerPrefab(AnimatorController controller)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Could not find Player prefab at {playerPrefabPath}");
                return;
            }

            GameObject prefabInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            
            // 1. Disable Capsule
            Transform capsule = prefabInstance.transform.Find("Capsule");
            if (capsule != null)
            {
                capsule.gameObject.SetActive(false);
            }
            else
            {
                var mesh = prefabInstance.GetComponent<MeshRenderer>();
                if (mesh != null) mesh.enabled = false;
            }

            // 2. Add Character Model
            string modelPath = characterFolder + "/Ch28_nonPBR.fbx";
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelPrefab == null)
            {
                Debug.LogError($"Could not find character model at {modelPath}");
                Object.DestroyImmediate(prefabInstance);
                return;
            }

            Transform oldModel = prefabInstance.transform.Find("Ch28_nonPBR");
            if (oldModel != null) Object.DestroyImmediate(oldModel.gameObject);

            GameObject modelInstance = PrefabUtility.InstantiatePrefab(modelPrefab, prefabInstance.transform) as GameObject;
            modelInstance.name = "Ch28_nonPBR";
            
            // Player collider is typically 2m high with pivot at center. 
            // Mixamo characters usually have pivot at feet.
            // Move character down by 1 unit to align feet with bottom of capsule collider.
            modelInstance.transform.localPosition = new Vector3(0, -1f, 0);
            
            // 3. Setup Animator
            Animator animator = prefabInstance.GetComponent<Animator>();
            if (animator == null) animator = prefabInstance.AddComponent<Animator>();
            
            Animator modelAnimator = modelInstance.GetComponent<Animator>();
            if (modelAnimator != null)
            {
                animator.avatar = modelAnimator.avatar;
                Object.DestroyImmediate(modelAnimator); // Prevent double animators
            }

            animator.runtimeAnimatorController = controller;

            // 4. Hook up PlayerAnimator
            var playerAnim = prefabInstance.GetComponent<EndlessRunner.Player.PlayerAnimator>();
            if (playerAnim != null)
            {
                playerAnim.enabled = true;
            }

            PrefabUtility.SaveAsPrefabAsset(prefabInstance, playerPrefabPath);
            Object.DestroyImmediate(prefabInstance);
            
            Debug.Log("Player prefab updated successfully with new character model!");
        }
    }
}
