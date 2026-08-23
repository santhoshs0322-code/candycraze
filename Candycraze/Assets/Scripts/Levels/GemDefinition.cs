// ============================================================
// GemDefinition.cs
// ScriptableObject that describes one gem type.
// Create one asset per gem colour in:
//   Assets/ScriptableObjects/Gems/
// ============================================================

using UnityEngine;

namespace CandyCraze
{
    [CreateAssetMenu(
        fileName = "GemDefinition_New",
        menuName  = "CandyCraze/Gem Definition")]
    public class GemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique integer ID matching the GemType enum value.")]
        public int      GemTypeID;

        [Tooltip("Human-readable name shown in debug logs.")]
        public string   GemName = "Gem";

        [Header("Visuals")]
        [Tooltip("Normal sprite shown on the board.")]
        public Sprite   NormalSprite;

        [Tooltip("Highlighted sprite (when selected).")]
        public Sprite   HighlightSprite;

        [Tooltip("Tint colour used for effects / particles.")]
        public Color    GemColor = Color.white;

        [Header("Prefab")]
        [Tooltip("Prefab instantiated on the board.  Must have a GemView component.")]
        public GameObject GemPrefab;

        [Header("Particle Effects")]
        [Tooltip("Particle effect played when this gem is matched and destroyed.")]
        public GameObject DestroyParticlePrefab;

        [Header("Audio")]
        [Tooltip("Sound played when this gem is matched (overrides global match sound if set).")]
        public AudioClip  MatchSound;
    }
}
