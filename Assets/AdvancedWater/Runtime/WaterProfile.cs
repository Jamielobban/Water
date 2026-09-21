using UnityEngine;

namespace AdvancedWater
{
    public enum WaterQuality { Low, Medium, High, Cinematic }

    [System.Serializable]
    public struct WaterWave
    {
        [Range(-180, 180)] public float direction;
        [Min(0)] public float amplitude;
        [Min(0.1f)] public float wavelength;
        public WaterWave(float direction, float amplitude, float wavelength)
        { this.direction = direction; this.amplitude = amplitude; this.wavelength = wavelength; }
        public Vector4 Pack()
        {
            float angle = direction * Mathf.Deg2Rad;
            return new Vector4(Mathf.Cos(angle), Mathf.Sin(angle), Mathf.Max(0, amplitude), Mathf.Max(0.1f, wavelength));
        }
    }

    [CreateAssetMenu(menuName = "Advanced Water/Water Profile", fileName = "Water Profile")]
    public sealed class WaterProfile : ScriptableObject
    {
        [Tooltip("Appearance and texture slots live on this material. Duplicate it for independent art direction.")]
        public Material material;
        [Tooltip("Optional generated-grid resolution for this style. Zero uses the Water Mesh resolution.")]
        [Range(0,256)] public int meshResolutionOverride;
        public WaterQuality quality = WaterQuality.High;
        public WaterWave wave1 = new WaterWave(15, 0.35f, 14);
        public WaterWave wave2 = new WaterWave(65, 0.18f, 7);
        public WaterWave wave3 = new WaterWave(-35, 0.08f, 3.5f);
        public WaterWave wave4 = new WaterWave(110, 0.035f, 1.7f);
        [Range(0, 1)] public float steepness = 0.55f;
        [Range(0, 3)] public float waveSpeed = 1;
        [Header("Wave Shape")]
        [Tooltip("Bends wave fronts and varies their amplitude in slowly evolving wave groups.")]
        [Range(0, 1)] public float shapeVariation = 0.35f;
        [Tooltip("World-space frequency of the broad wave-shape variation. Typical range: 0.08-0.25.")]
        [Range(0.01f, 0.5f)] public float shapeScale = 0.14f;
        [Tooltip("Makes crests narrower and troughs broader without increasing maximum wave height.")]
        [Range(0, 1)] public float crestSharpness = 0.35f;
        [Header("Interaction")]
        [Range(0, 2)] public float rippleStrength = 1f;
        [Min(0.1f)] public float rippleLifetime = 5;
        [Header("Underwater")]
        public bool underwater = true;
        public Color underwaterColor = new Color(0.015f, 0.18f, 0.23f, 1);
        [Min(0.001f)] public float underwaterDensity = 0.09f;
        [Range(0, 0.03f)] public float underwaterDistortion = 0.002f;
        [Range(0.01f, 0.5f)] public float waterlineWidth = 0.08f;

        // Keep the four authored bands, but split realistic seas into directional sidebands.
        public const int WaveCount = 12;
        [Tooltip("Spreads each wave into three directions and wavelengths to break repeating fronts.")]
        [Range(0, 1)] public float directionalSpread;

        public void GetWaves(Vector4[] destination)
        {
            destination[0] = wave1.Pack(); destination[1] = wave2.Pack();
            destination[2] = wave3.Pack(); destination[3] = wave4.Pack();
            if (destination.Length < WaveCount) return;
            float spread = Mathf.Clamp01(directionalSpread);
            for (int i = 0; i < 4; i++)
            {
                Vector4 band = destination[i];
                float angle = (19 + i * 7.3f) * Mathf.Deg2Rad;
                float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
                destination[i + 4] = new Vector4(band.x*c-band.y*s, band.x*s+band.y*c,
                    band.z*spread*0.27f, band.w*(0.73f+i*0.031f));
                destination[i + 8] = new Vector4(band.x*c+band.y*s, -band.x*s+band.y*c,
                    band.z*spread*0.18f, band.w*(1.21f+i*0.047f));
                band.z *= 1-spread*0.45f;
                destination[i] = band;
            }
        }

        public float MaximumDisplacement => (Mathf.Max(0, wave1.amplitude) + Mathf.Max(0, wave2.amplitude)
            + Mathf.Max(0, wave3.amplitude) + Mathf.Max(0, wave4.amplitude)) * (1 + shapeVariation * 0.5f);
    }
}
