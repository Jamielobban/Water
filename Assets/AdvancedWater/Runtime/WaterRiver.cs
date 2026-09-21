using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedWater
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class WaterRiver : MonoBehaviour
    {
        [Serializable]
        public struct Point
        {
            public Vector3 position;
            [Min(0.2f)] public float width;
            [Range(0, 1)] public float foam;
            public Point(Vector3 position, float width, float foam = 0)
            { this.position = position; this.width = width; this.foam = foam; }
        }

        [Tooltip("Local-space points in downstream order. Closely spaced points round the waterfall lip and landing.")]
        public Point[] points = {
            new Point(new Vector3(-5,8,-24),7), new Point(new Vector3(-2,8,-14),8),
            new Point(new Vector3(0,8,-5),7,0.2f), new Point(new Vector3(0,7.8f,-2),7,0.6f),
            new Point(new Vector3(0,7,-0.5f),7,0.85f), new Point(new Vector3(0,4,0.5f),7,0.7f),
            new Point(new Vector3(0,0.5f,1.5f),8,1), new Point(new Vector3(0,0,4),11,1),
            new Point(new Vector3(2,0,9),13,0.55f), new Point(new Vector3(7,0,19),10,0.1f),
            new Point(new Vector3(5,0,32),9)
        };
        [Range(2,32)] public int subdivisions = 12;
        [Range(2,32)] public int across = 12;
        public Material material;
        Mesh generated, previous;
        bool dirty = true;

        void OnEnable() { previous = GetComponent<MeshFilter>().sharedMesh; dirty = true; }
        void OnValidate() { dirty = true; }
        void Update() { if (dirty) Rebuild(); }
        void OnDisable()
        {
            if (GetComponent<MeshFilter>().sharedMesh == generated) GetComponent<MeshFilter>().sharedMesh = previous;
            Release();
        }
        void Release()
        {
            if (!generated) return;
            if (Application.isPlaying) Destroy(generated); else DestroyImmediate(generated);
            generated = null;
        }

        [ContextMenu("Rebuild River")]
        public void Rebuild()
        {
            dirty = false;
            Release();
            if (points == null || points.Length < 2) return;
            int steps = Mathf.Clamp(subdivisions,2,32), columns = Mathf.Clamp(across,2,32);
            var centers = new List<Vector3>();
            var widths = new List<float>();
            var foams = new List<float>();
            for (int segment=0; segment<points.Length-1; segment++)
                for (int j=0; j<steps; j++)
                {
                    float t=j/(float)steps;
                    // Linear elevation avoids spline overshoot at the lip and pool.
                    centers.Add(Vector3.Lerp(points[segment].position,points[segment+1].position,t));
                    widths.Add(Mathf.Max(0.2f,Mathf.Lerp(points[segment].width,points[segment+1].width,t)));
                    foams.Add(Mathf.Lerp(points[segment].foam,points[segment+1].foam,t));
                }
            centers.Add(points[points.Length-1].position);
            widths.Add(Mathf.Max(0.2f,points[points.Length-1].width));
            foams.Add(points[points.Length-1].foam);
            int stride=columns+1;
            var vertices=new Vector3[centers.Count*stride];
            var uv=new Vector2[vertices.Length];
            var colors=new Color[vertices.Length];
            var triangles=new int[(centers.Count-1)*columns*6];
            float distance=0;
            Vector3 side=Vector3.right;
            for (int row=0;row<centers.Count;row++)
            {
                if (row>0) distance+=Vector3.Distance(centers[row-1],centers[row]);
                Vector3 direction=centers[Mathf.Min(row+1,centers.Count-1)]-centers[Mathf.Max(0,row-1)];
                Vector3 lateral=Vector3.Cross(Vector3.up,direction);
                if (lateral.sqrMagnitude>0.0001f) side=lateral.normalized;
                float slope=direction.sqrMagnitude>0.0001f?Mathf.Clamp01(-direction.normalized.y):0;
                for (int column=0;column<stride;column++)
                {
                    int index=row*stride+column;
                    float u=column/(float)columns;
                    vertices[index]=centers[row]+side*((u-0.5f)*widths[row]);
                    uv[index]=new Vector2(u,distance);
                    colors[index]=new Color(foams[row],slope,0,1);
                    if (row==centers.Count-1 || column==columns) continue;
                    int ti=(row*columns+column)*6;
                    triangles[ti]=index; triangles[ti+1]=index+stride; triangles[ti+2]=index+1;
                    triangles[ti+3]=index+1; triangles[ti+4]=index+stride; triangles[ti+5]=index+stride+1;
                }
            }
            generated=new Mesh { name="River and Waterfall (Generated)", hideFlags=HideFlags.DontSave,
                indexFormat=vertices.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16 };
            generated.vertices=vertices; generated.uv=uv; generated.colors=colors; generated.triangles=triangles;
            generated.RecalculateNormals(); generated.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh=generated;
            if (material) GetComponent<MeshRenderer>().sharedMaterial=material;
        }
    }
}
