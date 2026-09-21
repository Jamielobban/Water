using UnityEditor;
using UnityEngine;

namespace GPUGrass.Editor
{
    public static class GrassWindMap
    {
        public static void Create(GpuGrass field)
        {
            string path=EditorUtility.SaveFilePanelInProject("Save wind flow map", "Meadow Swirling Wind", "asset", "Save a linear direction and strength texture.");
            if (string.IsNullOrEmpty(path)) return;
            const int n=128;
            var texture=new Texture2D(n,n,TextureFormat.RGBA32,false,true)
            { name="Meadow Swirling Wind", wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Bilinear };
            var colors=new Color[n*n];
            for (int y=0;y<n;y++) for (int x=0;x<n;x++)
            {
                Vector2 p=new Vector2(x/(float)(n-1)-.5f,y/(float)(n-1)-.5f);
                Vector2 a=p-new Vector2(-.22f,.08f), b=p-new Vector2(.24f,-.13f);
                Vector2 flow=new Vector2(.65f,.15f)
                    +new Vector2(-a.y,a.x)*(.14f/(a.sqrMagnitude+.018f))
                    -new Vector2(-b.y,b.x)*(.12f/(b.sqrMagnitude+.025f));
                float strength=Mathf.Clamp(flow.magnitude*.6f,.15f,.85f);
                flow.Normalize();
                colors[y*n+x]=new Color(flow.x*.5f+.5f,flow.y*.5f+.5f,strength,1);
            }
            texture.SetPixels(colors); texture.Apply(); AssetDatabase.CreateAsset(texture,path);
            Undo.RecordObject(field,"Assign wind flow map");
            field.windFlowMap=texture; field.flowInfluence=1;
            EditorUtility.SetDirty(field); PrefabUtility.RecordPrefabInstancePropertyModifications(field);
            AssetDatabase.SaveAssetIfDirty(texture); SceneView.RepaintAll();
        }
        public static void Share(GpuGrass field)
        {
            foreach(var other in Object.FindObjectsByType<GpuGrass>(FindObjectsSortMode.None))
            {
                if(other==field || other.gameObject.scene!=field.gameObject.scene) continue;
                Undo.RecordObject(other,"Share meadow wind");
                other.windFlowMap=field.windFlowMap; other.windDirection=field.windDirection;
                other.flowInfluence=field.flowInfluence; other.directionVariation=field.directionVariation;
                other.windPatternSize=field.windPatternSize; other.wind=field.wind;
                EditorUtility.SetDirty(other); PrefabUtility.RecordPrefabInstancePropertyModifications(other);
            }
            SceneView.RepaintAll();
        }
    }
}
