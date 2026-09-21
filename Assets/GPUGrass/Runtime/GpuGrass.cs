using UnityEngine;
using UnityEngine.Rendering;

namespace GPUGrass
{
    [ExecuteAlways]
    public sealed class GpuGrass : MonoBehaviour
    {
        [Range(1000, 1000000)] public int bladeCount = 240000;
        [Min(10)] public float fieldSize = 100;
        public Vector3 lodDistances = new Vector3(18, 40, 85);
        [Range(0, 3)] public float wind = 1;
        [Header("Wind Flow")]
        [Tooltip("Linear texture: red/green encode world X/Z direction (0..1 maps to -1..1), blue controls strength.")]
        public Texture2D windFlowMap;
        [Range(0, 360)] public float windDirection = 23;
        [Range(0, 1)] public float flowInfluence = 1;
        [Range(0, 1)] public float directionVariation = .5f;
        [Min(1)] public float windPatternSize = 25;
        public bool debugLOD;
        public GrassStyle style = GrassStyle.SoftMeadow;
        public string StyleName => GrassStyleSettings.Label(style);
        public void NextStyle() { style = (GrassStyle)(((int)style + 1) % System.Enum.GetValues(typeof(GrassStyle)).Length); RefreshPaint(); }
        public bool UsesSculptedShader => !flowers && (int)style >= (int)GrassStyle.CelField;
        [Tooltip("Render daisies, poppies and tall cosmos instead of grass. Use a separate field/map to layer flowers over grass.")]
        public bool flowers;
        [Tooltip("Optional painted density/type map. Without one, the original meadow bands are used.")]
        public GrassPaintMap paintMap;
        public GrassValley surface;
        [Range(0, 1)] public float slopeAlignment = .65f;
        [Range(0, 85)] public float maxSlope = 55;
        ComputeBuffer surfaceBuffer;
        GrassValley appliedSurface;
        int surfaceRevision;
        ComputeBuffer paintBuffer;
        bool placementDirty = true;
        GrassPaintMap appliedMap;
        bool allocatedFlowers;
        GrassStyle allocatedStyle;
        ComputeShader compute;
        Material material;
        ComputeBuffer source;
        readonly ComputeBuffer[] visible = new ComputeBuffer[3];
        readonly ComputeBuffer[] arguments = new ComputeBuffer[3];
        readonly MaterialPropertyBlock[] properties = new MaterialPropertyBlock[3];
        readonly int[] segments = { 5, 2, 1 };
        readonly string[] names = { "_Near", "_Mid", "_Far" };
        readonly Plane[] planes = new Plane[6];
        readonly Vector4[] planeVectors = new Vector4[6];
        int allocatedCount;
        float allocatedSize;

        void OnEnable() { RenderPipelineManager.beginCameraRendering += Render; }
        void OnValidate() { RefreshPaint(); }
        public void RefreshPaint() { placementDirty = true; }
        void OnDisable() { RenderPipelineManager.beginCameraRendering -= Render; Release(); }
        void Release()
        {
            source?.Release(); source = null;
            paintBuffer?.Release(); paintBuffer = null;
            surfaceBuffer?.Release(); surfaceBuffer = null;
            for (int i = 0; i < 3; i++) { visible[i]?.Release(); arguments[i]?.Release(); visible[i] = null; arguments[i] = null; }
            if (material) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
            if (compute) { if (Application.isPlaying) Destroy(compute); else DestroyImmediate(compute); }
        }
        bool Initialize()
        {
            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsInstancing) return false;
            Release();
            var asset = Resources.Load<ComputeShader>("Grass");
            var shader = Resources.Load<Shader>(flowers ? "Flowers" : (UsesSculptedShader ? "SculptedMeadow" : "Grass"));
            if (!asset || !shader || !shader.isSupported) return false;
            compute = Instantiate(asset);
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            allocatedCount = Mathf.Clamp(bladeCount, 1000, 1000000);
            allocatedSize = Mathf.Max(10, fieldSize);
            allocatedFlowers = flowers;
            allocatedStyle = style;
            source = new ComputeBuffer(allocatedCount, 48);
            for (int i = 0; i < 3; i++)
            {
                visible[i] = new ComputeBuffer(allocatedCount, 48, ComputeBufferType.Append);
                arguments[i] = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);
                int petals = i == 0 ? 12 : (i == 1 ? 8 : 4);
                int detail = !flowers && style == GrassStyle.LowPoly ? Mathf.Min(2, segments[i]) : segments[i];
                if (UsesSculptedShader) detail = style == GrassStyle.CurledRibbons ? (i == 0 ? 8 : (i == 1 ? 4 : 2)) : Mathf.Min(2, segments[i]);
                if (UsesSculptedShader && style == GrassStyle.AnimeMeadow) detail = i == 0 ? 6 : (i == 1 ? 3 : 1);
                int vertices = detail * 6 + (flowers ? petals * 9 + (i < 2 ? 12 : 0) : 0);
                int sides = i == 0 ? 6 : (i == 1 ? 4 : 3);
                if (UsesSculptedShader && style == GrassStyle.CrystalGarden) vertices = sides * 9;
                if (UsesSculptedShader && style == GrassStyle.ReedBed) vertices += sides * 9;
                arguments[i].SetData(new uint[] { (uint)vertices, 0, 0, 0 });
                properties[i] = new MaterialPropertyBlock();
                properties[i].SetBuffer("_Blades", visible[i]);
                properties[i].SetInt("_Segments", detail);
                properties[i].SetInt("_Lod", i);
                properties[i].SetInt("_Petals", petals);
                properties[i].SetInt("_Sides", sides);
                properties[i].SetInt("_Shape", (int)style - (int)GrassStyle.CelField);
            }
            paintBuffer = new ComputeBuffer(GrassPaintMap.Resolution * GrassPaintMap.Resolution, 16);
            surfaceBuffer = new ComputeBuffer(GrassValley.Resolution * GrassValley.Resolution, 16);
            GeneratePlacement();
            return true;
        }
        void GeneratePlacement()
        {
            int generate = compute.FindKernel("Generate");
            if (surface && surface.Samples == null) surface.Rebuild();
            if (surface) surfaceBuffer.SetData(surface.Samples);
            else surfaceBuffer.SetData(new Vector4[GrassValley.Resolution * GrassValley.Resolution]);
            compute.SetBuffer(generate, "_Surface", surfaceBuffer);
            compute.SetInt("_UseSurface", surface ? 1 : 0);
            compute.SetInt("_SurfaceResolution", GrassValley.Resolution);
            compute.SetFloat("_SurfaceSize", surface ? Mathf.Max(10, surface.size) : allocatedSize);
            compute.SetFloat("_Alignment", slopeAlignment);
            compute.SetFloat("_MinNormalY", Mathf.Cos(maxSlope * Mathf.Deg2Rad));
            appliedSurface = surface; surfaceRevision = surface ? surface.Revision : 0;
            bool validMap = paintMap && paintMap.pixels != null && paintMap.pixels.Length == GrassPaintMap.Resolution * GrassPaintMap.Resolution;
            if (validMap) paintBuffer.SetData(paintMap.pixels);
            else paintBuffer.SetData(new Vector4[GrassPaintMap.Resolution * GrassPaintMap.Resolution]);
            compute.SetBuffer(generate, "_Paint", paintBuffer);
            compute.SetInt("_PaintResolution", GrassPaintMap.Resolution);
            compute.SetInt("_UsePaint", validMap ? 1 : 0);
            compute.SetInt("_Flowers", flowers ? 1 : 0);
            compute.SetFloat("_StyleDensity", !flowers && style == GrassStyle.CrystalGarden ? .16f : (!flowers && style == GrassStyle.ReedBed ? .28f : (!flowers && style == GrassStyle.CurledRibbons ? .4f : 1)));
            var appearance = GrassStyleSettings.Get(style);
            compute.SetVector("_StyleSize", flowers ? Vector4.one : new Vector4(appearance.height, appearance.width, 0, 0));
            compute.SetInt("_Count", allocatedCount); compute.SetFloat("_Size", allocatedSize);
            compute.SetBuffer(generate, "_Source", source);
            compute.Dispatch(generate, (allocatedCount + 127) / 128, 1, 1);
            appliedMap = paintMap;
            placementDirty = false;
        }
        void Render(ScriptableRenderContext context, Camera camera)
        {
            if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView) return;
            if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;
            if (source == null || allocatedStyle != style || allocatedFlowers != flowers || allocatedCount != Mathf.Clamp(bladeCount, 1000, 1000000) || !Mathf.Approximately(allocatedSize, Mathf.Max(10, fieldSize)))
                if (!Initialize()) return;
            if (placementDirty || appliedMap != paintMap || appliedSurface != surface || (surface && surfaceRevision != surface.Revision)) GeneratePlacement();
            int kernel = compute.FindKernel("Cull");
            GeometryUtility.CalculateFrustumPlanes(camera, planes);
            for (int i = 0; i < 6; i++) planeVectors[i] = new Vector4(planes[i].normal.x, planes[i].normal.y, planes[i].normal.z, planes[i].distance);
            compute.SetVectorArray("_Planes", planeVectors);
            compute.SetVector("_CameraPosition", camera.transform.position);
            float near = Mathf.Max(1, lodDistances.x), mid = Mathf.Max(near + 1, lodDistances.y);
            compute.SetVector("_Distances", new Vector4(near, mid, Mathf.Max(mid + 1, lodDistances.z), 0));
            compute.SetBuffer(kernel, "_Source", source);
            for (int i = 0; i < 3; i++) { visible[i].SetCounterValue(0); compute.SetBuffer(kernel, names[i], visible[i]); }
            compute.Dispatch(kernel, (allocatedCount + 127) / 128, 1, 1);
            for (int i = 0; i < 3; i++)
            {
                ComputeBuffer.CopyCount(visible[i], arguments[i], sizeof(uint));
                properties[i].SetFloat("_Wind", wind); properties[i].SetFloat("_DebugLOD", debugLOD ? 1 : 0);
                float direction = windDirection * Mathf.Deg2Rad;
                properties[i].SetTexture("_WindFlowMap", windFlowMap ? windFlowMap : Texture2D.grayTexture);
                properties[i].SetVector("_WindFlowSettings", new Vector4(windFlowMap ? flowInfluence : 0, Mathf.Max(10, fieldSize), directionVariation, Mathf.Max(1, windPatternSize)));
                properties[i].SetVector("_WindBaseDirection", new Vector4(Mathf.Cos(direction), Mathf.Sin(direction), 0, 0));
                var appearance = GrassStyleSettings.Get(style);
                properties[i].SetColor("_StyleRoot", appearance.root);
                properties[i].SetColor("_StyleTip", appearance.tip);
                properties[i].SetVector("_StyleMotion", new Vector4(appearance.curve, appearance.windSpeed, appearance.windScale, appearance.toon));
                properties[i].SetFloat("_StyleEmission", appearance.emission);
                Bounds bounds = surface ? surface.SurfaceBounds : new Bounds(Vector3.zero, new Vector3(allocatedSize, 0, allocatedSize));
                bounds.Expand(new Vector3(12, 16, 12));
                Graphics.DrawProceduralIndirect(material, bounds,
                    MeshTopology.Triangles, arguments[i], 0, camera, properties[i], ShadowCastingMode.Off, true, gameObject.layer);
            }
        }
    }
}
