using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ActiveMotion3D.EndlessRunner.Player;

namespace ActiveMotion3D.Editor
{
    public static class StandModelImporter
    {
        private static readonly string[] ModelPaths =
        {
            "Assets/ActiveMotion3D/Art/Character/D4C/D4C.fbx",
            "Assets/ActiveMotion3D/Art/Character/KillerQueen/Killer Queen.fbx"
        };

        [MenuItem("Tools/Active Motion 3D/Import Stand Models")]
        public static void Import()
        {
            foreach (string path in ModelPaths)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    Debug.LogError($"Stand import: no model importer for {path}");
                    continue;
                }

                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = false;
                importer.SaveAndReimport();

                Avatar avatar = null;
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is Avatar found) avatar = found;

                Debug.Log($"Stand import: {path}, avatar present={avatar != null}, " +
                    $"valid={avatar != null && avatar.isValid}, humanoid={avatar != null && avatar.isHuman}");
            }
        }

        [MenuItem("Tools/Active Motion 3D/Install Stand Characters")]
        public static void InstallCharacters()
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError("Save the currently open scene before installing Stand characters.");
                return;
            }

            GameObject[] assets = new GameObject[2];
            Avatar[] avatars = new Avatar[2];
            for (int i = 0; i < ModelPaths.Length; i++)
            {
                assets[i] = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPaths[i]);
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(ModelPaths[i]))
                    if (asset is Avatar avatar) avatars[i] = avatar;
                if (assets[i] == null || avatars[i] == null || !avatars[i].isValid || !avatars[i].isHuman)
                {
                    Debug.LogError($"Stand import is incomplete: {ModelPaths[i]}");
                    return;
                }
            }

            string gameplayPath = "Assets/ActiveMotion3D/Scenes/AM3D_ActiveMotionBase.unity";
            string menuPath = "Assets/ActiveMotion3D/Scenes/AM3D_MainMenu.unity";
            Scene gameplay = EditorSceneManager.OpenScene(gameplayPath);
            CharacterSelector gameSelector = Object.FindAnyObjectByType<CharacterSelector>();
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            if (gameSelector == null || player == null)
            {
                Debug.LogError("Gameplay scene is missing the player or character selector.");
                return;
            }
            if (gameSelector.models != null && gameSelector.models.Length == 2 &&
                gameSelector.models[0] != null && gameSelector.models[1] != null &&
                gameSelector.models[0].name == "D4C" && gameSelector.models[1].name == "Killer Queen")
            {
                Debug.Log("D4C and Killer Queen are already installed.");
                return;
            }

            GameObject[] oldModels = gameSelector.models;
            GameObject[] sceneModels = new GameObject[2];
            for (int i = 0; i < assets.Length; i++)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(assets[i], gameplay);
                model.name = i == 0 ? "D4C" : "Killer Queen";
                model.transform.SetParent(player.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                Animator nestedAnimator = model.GetComponent<Animator>();
                if (nestedAnimator != null) nestedAnimator.enabled = false;

                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int r = 1; r < renderers.Length; r++) bounds.Encapsulate(renderers[r].bounds);
                    if (bounds.size.y > 0.01f)
                    {
                        model.transform.localScale *= 2.2f / bounds.size.y;
                        bounds = renderers[0].bounds;
                        for (int r = 1; r < renderers.Length; r++) bounds.Encapsulate(renderers[r].bounds);
                        model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, 0f);
                    }
                }
                model.SetActive(i == 0);
                sceneModels[i] = model;
            }

            gameSelector.models = sceneModels;
            gameSelector.avatars = avatars;
            gameSelector.displayNames = new[] { "D4C", "Killer Queen" };
            Animator playerAnimator = player.GetComponent<Animator>();
            if (playerAnimator != null) playerAnimator.avatar = avatars[0];
            foreach (GameObject oldModel in oldModels)
                if (oldModel != null && oldModel.scene == gameplay) oldModel.SetActive(false);
            EditorUtility.SetDirty(gameSelector);
            EditorSceneManager.MarkSceneDirty(gameplay);
            EditorSceneManager.SaveScene(gameplay);

            Scene menu = EditorSceneManager.OpenScene(menuPath);
            CharacterSelector menuSelector = Object.FindAnyObjectByType<CharacterSelector>();
            if (menuSelector == null)
            {
                Debug.LogError("Main menu is missing its character selector.");
                return;
            }
            menuSelector.models = assets;
            menuSelector.avatars = avatars;
            menuSelector.displayNames = new[] { "D4C", "Killer Queen" };
            EditorUtility.SetDirty(menuSelector);
            EditorSceneManager.MarkSceneDirty(menu);
            EditorSceneManager.SaveScene(menu);
            Debug.Log("Installed D4C and Killer Queen in both ActiveMotion3D scenes.");
        }

        [MenuItem("Tools/Active Motion 3D/Apply Stand Materials")]
        public static void ApplyMaterials()
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError("Save the currently open scene before applying Stand materials.");
                return;
            }

            Material d4c = GetMaterial("Assets/ActiveMotion3D/Art/Character/D4C/D4C.mat",
                "Assets/ActiveMotion3D/Art/Character/D4C/7d4c_diffuse.png");
            Material queenBody = GetMaterial("Assets/ActiveMotion3D/Art/Character/KillerQueen/KillerQueenBody.mat",
                "Assets/ActiveMotion3D/Art/Character/KillerQueen/t_4klq01_body01_d.png");
            Material queenEye = GetMaterial("Assets/ActiveMotion3D/Art/Character/KillerQueen/KillerQueenEye.mat",
                "Assets/ActiveMotion3D/Art/Character/KillerQueen/t_4klq01_bod01_iris.png");
            if (d4c == null || queenBody == null || queenEye == null) return;

            Scene scene = EditorSceneManager.OpenScene("Assets/ActiveMotion3D/Scenes/AM3D_ActiveMotionBase.unity");
            CharacterSelector selector = Object.FindAnyObjectByType<CharacterSelector>();
            if (selector == null || selector.models == null || selector.models.Length != 2)
            {
                Debug.LogError("Install Stand characters before applying materials.");
                return;
            }

            Animator playerAnimator = selector.GetComponent<Animator>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(ModelPaths[0]))
                if (asset is Avatar avatar && playerAnimator != null) playerAnimator.avatar = avatar;

            foreach (Renderer renderer in selector.models[0].GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = d4c;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            foreach (Renderer renderer in selector.models[1].GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = renderer.name.Contains("Layer 2") ? queenEye : queenBody;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Applied D4C and Killer Queen textures in the gameplay scene.");
        }

        private static Material GetMaterial(string materialPath, string texturePath)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                Debug.LogError($"Missing Stand texture: {texturePath}");
                return null;
            }
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.mainTexture = texture;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
