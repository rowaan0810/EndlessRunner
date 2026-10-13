using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ActiveMotion3D.Editor
{
    public static class AM3DBuildSettingsInstaller
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/ActiveMotion3D/Scenes/AM3D_MainMenu.unity",
            "Assets/ActiveMotion3D/Scenes/AM3D_ActiveMotionBase.unity"
        };

        [MenuItem("Tools/Active Motion 3D/Add Integration Scenes to Build")]
        public static void AddScenesToBuild()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            foreach (string path in ScenePaths)
            {
                if (scenes.All(scene => scene.path != path))
                    scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Active Motion 3D integration scenes were added without removing existing build scenes.");
        }

        public static void ValidateIntegration()
        {
            int missingScriptCount = 0;

            foreach (string path in ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                        missingScriptCount += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                }
            }

            if (missingScriptCount > 0)
                throw new System.InvalidOperationException($"Active Motion 3D scenes contain {missingScriptCount} missing script reference(s).");

            Debug.Log("AM3D_INTEGRATION_VALID: both scenes opened with zero missing scripts.");
        }
    }
}
