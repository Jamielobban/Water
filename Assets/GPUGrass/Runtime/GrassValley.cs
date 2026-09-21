using UnityEngine;

namespace GPUGrass
{
    [ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public sealed class GrassValley : MonoBehaviour
    {
        public const int Resolution = 129;
        [Min(10)] public float size = 100;
        [Range(0, 15)] public float hillHeight = 6;
        [Range(5, 35)] public float valleyWidth = 16;
        public Vector4[] Samples { get; private set; }
        public int Revision { get; private set; }
        public Bounds SurfaceBounds { get; private set; }
        Mesh mesh;
        Material material;
        bool dirty = true;
        void OnEnable() { Rebuild(); }
        void OnValidate() { dirty = true; }
        void Update() { if (dirty) Rebuild(); }
        float Height(float x, float z)
        {
            float center = Mathf.Sin(x * .055f) * 5;
            float bank = 1 - Mathf.Exp(-Mathf.Pow((z - center) / Mathf.Max(5, valleyWidth), 2));
            return hillHeight * bank * (.8f + .2f * Mathf.Sin(x * .065f + .5f))
                + .3f * Mathf.Sin(x * .09f) * Mathf.Sin(z * .07f);
        }
        public void Rebuild()
        {
            dirty = false;
            float span = Mathf.Max(10, size);
            int n = Resolution;
            var vertices = new Vector3[n * n];
            var uv = new Vector2[n * n];
            var triangles = new int[(n - 1) * (n - 1) * 6];
            Samples = new Vector4[n * n];
            for (int z = 0; z < n; z++) for (int x = 0; x < n; x++)
            {
                int index = z * n + x;
                float px = (x / (float)(n - 1) - .5f) * span;
                float pz = (z / (float)(n - 1) - .5f) * span;
                vertices[index] = new Vector3(px, Height(px, pz), pz);
                uv[index] = new Vector2(x / (float)(n - 1), z / (float)(n - 1));
            }
            int cursor = 0;
            for (int z = 0; z < n - 1; z++) for (int x = 0; x < n - 1; x++)
            {
                int a = z * n + x;
                triangles[cursor++] = a; triangles[cursor++] = a + n; triangles[cursor++] = a + 1;
                triangles[cursor++] = a + 1; triangles[cursor++] = a + n; triangles[cursor++] = a + n + 1;
            }
            if (!mesh) mesh = new Mesh { name = "Procedural Valley", hideFlags = HideFlags.DontSave };
            mesh.Clear(); mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var normals = mesh.normals;
            for (int i = 0; i < vertices.Length; i++) Samples[i] = new Vector4(normals[i].x, normals[i].y, normals[i].z, vertices[i].y);
            GetComponent<MeshFilter>().sharedMesh = mesh;
            var collider = GetComponent<MeshCollider>(); collider.sharedMesh = null; collider.sharedMesh = mesh;
            if (!GetComponent<MeshRenderer>().sharedMaterial)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { hideFlags = HideFlags.DontSave };
                material.SetColor("_BaseColor", new Color(.22f, .25f, .10f)); material.SetFloat("_Smoothness", 0);
                GetComponent<MeshRenderer>().sharedMaterial = material;
            }
            SurfaceBounds = mesh.bounds;
            Revision++;
        }
        void OnDestroy()
        {
            if (Application.isPlaying) { if (mesh) Destroy(mesh); if (material) Destroy(material); }
            else { if (mesh) DestroyImmediate(mesh); if (material) DestroyImmediate(material); }
        }
    }
}
