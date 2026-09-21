using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedWater
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class WaterMesh : MonoBehaviour
    {
        public enum SurfaceMode { Lake, Ocean }
        public SurfaceMode mode = SurfaceMode.Lake;
        [Min(1)] public float size = 150;
        [Range(16, 256)] public int resolution = 128;
        [Tooltip("Ocean only: concentrate vertices near the camera using a continuous stretched grid.")]
        [Range(1, 4)] public float oceanDensity = 2.5f;
        public Transform followTarget;
        Mesh generated;
        Mesh previous;
        float lastSize, lastDensity;
        int lastResolution;
        SurfaceMode lastMode;
        WaterSurface water;

        int EffectiveResolution
        {
            get
            {
                if (!water) water=GetComponent<WaterSurface>();
                return water && water.profile && water.profile.meshResolutionOverride>0
                    ? Mathf.Clamp(water.profile.meshResolutionOverride,16,256) : Mathf.Clamp(resolution,16,256);
            }
        }

        void OnEnable() { previous = GetComponent<MeshFilter>().sharedMesh; Rebuild(); }
        void OnDisable()
        {
            if (GetComponent<MeshFilter>().sharedMesh == generated) GetComponent<MeshFilter>().sharedMesh = previous;
            ReleaseMesh();
        }
        void LateUpdate()
        {
            if (!generated || lastSize != size || lastResolution != EffectiveResolution || lastDensity != oceanDensity || lastMode != mode) Rebuild();
            if (mode == SurfaceMode.Ocean && Application.isPlaying)
            {
                Transform target = followTarget;
                if (!target && Camera.main) target = Camera.main.transform;
                if (target) transform.position = new Vector3(target.position.x,transform.position.y,target.position.z);
            }
            UpdateBounds();
        }
        [ContextMenu("Rebuild Water Mesh")]
        public void Rebuild()
        {
            size = Mathf.Max(1,size); resolution = Mathf.Clamp(resolution,16,256);
            oceanDensity = Mathf.Clamp(oceanDensity,1,4);
            ReleaseMesh();
            int gridResolution=EffectiveResolution;
            int stride = gridResolution+1;
            Vector3[] vertices = new Vector3[stride*stride];
            Vector3[] normals = new Vector3[vertices.Length];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[gridResolution*gridResolution*6];
            for (int z=0; z<stride; z++) for (int x=0; x<stride; x++)
            {
                int i=z*stride+x;
                float px=x/(float)gridResolution*2-1, pz=z/(float)gridResolution*2-1;
                if (mode == SurfaceMode.Ocean)
                {
                    // Monotonic mapping keeps a single connected mesh: no tile cracks or LOD seams.
                    px = Mathf.Sign(px)*(0.04f*Mathf.Abs(px)+0.96f*Mathf.Pow(Mathf.Abs(px),oceanDensity));
                    pz = Mathf.Sign(pz)*(0.04f*Mathf.Abs(pz)+0.96f*Mathf.Pow(Mathf.Abs(pz),oceanDensity));
                }
                vertices[i]=new Vector3(px*size*0.5f,0,pz*size*0.5f);
                normals[i]=Vector3.up; uv[i]=new Vector2(x/(float)gridResolution,z/(float)gridResolution);
            }
            int index=0;
            for (int z=0; z<gridResolution; z++) for (int x=0; x<gridResolution; x++)
            {
                int a=z*stride+x, b=a+stride;
                triangles[index++]=a; triangles[index++]=b; triangles[index++]=a+1;
                triangles[index++]=a+1; triangles[index++]=b; triangles[index++]=b+1;
            }
            generated=new Mesh {name="Water Grid (Generated)",hideFlags=HideFlags.DontSave,
                indexFormat=vertices.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            generated.vertices=vertices; generated.normals=normals; generated.uv=uv; generated.triangles=triangles;
            GetComponent<MeshFilter>().sharedMesh=generated;
            lastSize=size; lastResolution=gridResolution; lastDensity=oceanDensity; lastMode=mode;
            UpdateBounds();
        }
        void UpdateBounds()
        {
            if (!generated) return;
            if (!water) water=GetComponent<WaterSurface>();
            float padding=water?water.MaxDisplacement+1:5;
            generated.bounds=new Bounds(Vector3.zero,new Vector3(size+padding*2,padding*2,size+padding*2));
        }
        void ReleaseMesh()
        {
            if (!generated) return;
            if (Application.isPlaying) Destroy(generated); else DestroyImmediate(generated);
            generated=null;
        }
    }
}
