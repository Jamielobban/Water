using UnityEngine;

namespace GPUGrass
{
    [CreateAssetMenu(menuName = "GPU Grass/Paint Map")]
    public sealed class GrassPaintMap : ScriptableObject
    {
        public const int Resolution = 128;
        [HideInInspector] public Vector4[] pixels = new Vector4[Resolution * Resolution];
    }
}
