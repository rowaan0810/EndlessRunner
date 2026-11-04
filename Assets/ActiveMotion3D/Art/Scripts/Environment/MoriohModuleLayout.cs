using System;
using UnityEngine;

namespace ActiveMotion3D.EndlessRunner.Environment
{
    [Serializable]
    public sealed class MoriohPlacement
    {
        public string name;
        public GameObject visualPrefab;
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale = Vector3.one;
    }

    [CreateAssetMenu(menuName = "Active Motion 3D/Morioh Module Layout")]
    public sealed class MoriohModuleLayout : ScriptableObject
    {
        [TextArea] public string designNotes;
        [Tooltip("Authoring placements only; each module remains 20 units with 3.2-unit lanes.")]
        public MoriohPlacement[] placements;
    }
}
