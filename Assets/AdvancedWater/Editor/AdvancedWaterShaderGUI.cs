using UnityEditor;
using UnityEngine;

namespace AdvancedWater.Editor
{
    public sealed class AdvancedWaterShaderGUI : ShaderGUI
    {
        static bool color=true,normals=true,optics=true,foam=true,caustics=false,art=false;
        public override void OnGUI(MaterialEditor editor,MaterialProperty[] properties)
        {
            MaterialProperty P(string name)=>FindProperty(name,properties,false);
            void Prop(string name,string label=null) { MaterialProperty p=P(name); if (p!=null) editor.ShaderProperty(p,label??p.displayName); }
            void Tex(string name,string label) { MaterialProperty p=P(name); if (p!=null) editor.TexturePropertySingleLine(new GUIContent(label),p); }
            bool Group(ref bool open,string label) { open=EditorGUILayout.BeginFoldoutHeaderGroup(open,label); return open; }

            if (Group(ref color,"Color and Absorption"))
            { Prop("_ShallowColor"); Prop("_DeepColor"); Prop("_Absorption"); Prop("_DepthDistance"); Prop("_EdgeFade"); }
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (Group(ref normals,"Normals and Current"))
            {
                Tex("_NormalA","Primary Normal"); Tex("_NormalB","Cross / Micro Normal"); Prop("_NormalScale");
                Prop("_NormalStrength"); Prop("_MicroStrength"); Prop("_Current"); Prop("_UseFlow"); Tex("_FlowMap","Flow Map");
                Prop("_FlowBounds"); Prop("_DetailFade");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (Group(ref optics,"Refraction, Reflection and Light"))
            {
                Prop("_UseRefraction"); Prop("_RefractionStrength"); Prop("_Smoothness"); Prop("_ReflectionStrength");
                Prop("_PlanarStrength"); Prop("_SpecularColor"); Prop("_SunStrength"); Prop("_Sparkle");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (Group(ref foam,"Independent Foam Layers"))
            {
                Prop("_UseFoam"); Tex("_FoamMap","Breaking-Crest Foam"); Tex("_ShoreFoamMap","Shore / Contact Foam");
                Tex("_NoiseMap","Foam Breakup Noise"); Prop("_FoamColor"); Prop("_FoamUnlit"); Prop("_FoamScale"); Prop("_FoamDistortion");
                Prop("_ShoreFoamWidth"); Prop("_ShoreFoamStrength"); Prop("_CrestThreshold"); Prop("_CrestFoamStrength");
                Prop("_IntersectionFoamWidth"); Prop("_IntersectionFoamStrength");
                Prop("_ShoreWaveStrength"); Prop("_ShoreWaveSpeed"); Prop("_InteractionFoamStrength");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (Group(ref caustics,"Caustics"))
            { Prop("_UseCaustics"); Tex("_CausticsMap","Caustics Map"); Prop("_CausticsScale"); Prop("_CausticsStrength"); Prop("_CausticsDepth"); }
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (Group(ref art,"Art Direction")) { Prop("_ScatterStrength"); Prop("_StylizedSteps"); Prop("_StylizedFoamCoverage"); }
            EditorGUILayout.EndFoldoutHeaderGroup();

            foreach (Object target in editor.targets)
            {
                Material m=(Material)target;
                SetKeyword(m,"_FOAM","_UseFoam"); SetKeyword(m,"_REFRACTION","_UseRefraction");
                SetKeyword(m,"_FLOWMAP","_UseFlow"); SetKeyword(m,"_CAUSTICS","_UseCaustics");
            }
        }
        static void SetKeyword(Material material,string keyword,string property)
        {
            if (material.HasProperty(property) && material.GetFloat(property)>0.5f) material.EnableKeyword(keyword); else material.DisableKeyword(keyword);
        }
    }
}
