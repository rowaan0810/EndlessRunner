using ActiveMotion3D.EndlessRunner.Collectibles;
using UnityEngine;

namespace ActiveMotion3D.EndlessRunner.Environment
{
    [System.Serializable]
    public sealed class MoriohMaterialSlot
    {
        public Renderer renderer;
        public int materialIndex;
    }
    /// <summary>Presentation adapter for the existing left/right punch target state.</summary>
    public sealed class MoriohPunchVisual : MonoBehaviour
    {
        public MoriohActivityArt art;
        public MoriohMaterialSlot[] stateSlots;
        public TextMesh sideLabel;
        public bool keepFacingRunner = true;
        private MaterialPropertyBlock properties;

        public void Show(PunchTargetSide side, bool active)
        {
            if (art == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            Color color = active ? art.targetActive : art.targetIdle;
            properties.SetColor("_Color", color);
            properties.SetColor("_EmissionColor", color * (active ? art.targetEmission : 0f));
            foreach (MoriohMaterialSlot slot in stateSlots)
                if (slot != null && slot.renderer != null)
                    slot.renderer.SetPropertyBlock(properties, slot.materialIndex);
            if (sideLabel != null)
            {
                sideLabel.text = active ? art.activeTargetLabel :
                    side == PunchTargetSide.Left ? art.leftTargetLabel : art.rightTargetLabel;
                sideLabel.color = art.instructionColor;
            }
        }

        public void PlayHit()
        {
            if (art != null && art.punchHitEffect != null)
                Instantiate(art.punchHitEffect, transform.position, Quaternion.identity);
        }

        private void LateUpdate()
        {
            if (keepFacingRunner) transform.rotation = Quaternion.identity;
        }
    }
}
