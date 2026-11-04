using System;
using ActiveMotion3D.EndlessRunner.Input.PoseDetection;
using UnityEditor;
using UnityEngine;

public static class GlobalGesturePrefabBuilder
{
    [MenuItem("Tools/Gestures/Create Global Gesture Prefab")]
    public static void CreatePrefab()
    {
        const string folder = "Assets/ActiveMotion3D/Art/Resources";
        const string path = folder + "/GlobalGestureController.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            throw new InvalidOperationException("Global gesture prefab already exists.");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/ActiveMotion3D/Art", "Resources");
        GameObject root = new GameObject("GlobalGestureController");
        try
        {
            root.AddComponent<GlobalGestureController>();
            root.AddComponent<MediaPipeManager>();
            root.AddComponent<HandGestureManager>();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            AssetDatabase.SaveAssets();
            Debug.Log("Global gesture prefab created: " + path);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
