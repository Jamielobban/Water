using UnityEditor;
using UnityEngine;

namespace AdvancedWater.Editor
{
    public sealed class StylizedWaterShaderGUI : ShaderGUI
    {
        static readonly string[] StyleNames={"Cel Lagoon","Wind Waker","Painterly","Graphic Ink","Arcane","Anime Ocean","Low-Poly","Cozy Pastel","Watercolor Lake","Manga Sea","Retro Pixel Water","Paper-cut Ocean","Bioluminescent Water"};
        static bool palette=true,surface=true,surfaceFoam=true,foam=true,lighting=true;

        public override void OnGUI(MaterialEditor editor,MaterialProperty[] properties)
        {
            MaterialProperty P(string name)=>FindProperty(name,properties,false);
            void Prop(string name,string label=null)
            {
                MaterialProperty property=P(name);
                if (property!=null) editor.ShaderProperty(property,label??property.displayName);
            }
            void Texture(string name,string label)
            {
                MaterialProperty property=P(name);
                if (property!=null) editor.TexturePropertySingleLine(new GUIContent(label),property);
            }
            bool Group(ref bool open,string label)
            {
                open=EditorGUILayout.BeginFoldoutHeaderGroup(open,label);
                return open;
            }

            MaterialProperty style=P("_StyleMode");
            if (style!=null)
            {
                EditorGUI.showMixedValue=style.hasMixedValue;
                EditorGUI.BeginChangeCheck();
                int selected=EditorGUILayout.Popup("Style Shading",Mathf.Clamp(Mathf.RoundToInt(style.floatValue),0,StyleNames.Length-1),StyleNames);
                if (EditorGUI.EndChangeCheck()) { editor.RegisterPropertyChangeUndo("Style Shading"); style.floatValue=selected; }
                EditorGUI.showMixedValue=false;
            }
            EditorGUILayout.HelpBox("Brush Noise breaks flat color bands into painted patches. Brush Scale sets patch size; Brush Strength controls their visibility.",MessageType.Info);

            if (Group(ref palette,"Palette and Color Bands"))
            {
                Prop("_ShallowColor"); Prop("_DeepColor"); Prop("_ShadowColor"); Prop("_AccentColor");
                Prop("_ColorDepth"); Prop("_ColorSteps");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (Group(ref surface,"Surface Motion and Brushwork"))
            {
                Texture("_NormalA","Broad Normal"); Texture("_NormalB","Detail Normal");
                Prop("_NormalScale"); Prop("_NormalStrength"); Prop("_Current");
                Texture("_NoiseMap","Brush / Anti-Tiling Noise"); Prop("_BrushScale"); Prop("_BrushStrength");
                Prop("_MarkScale"); Prop("_MarkStrength"); Prop("_MarkSpeed"); Prop("_SparkleDensity");
                if (style!=null && (style.hasMixedValue || style.floatValue>7.5f))
                {
                    Prop("_PaperScale"); Prop("_PaperStrength"); Prop("_PigmentStrength");
                    Prop("_HatchScale"); Prop("_HatchStrength");
                    Prop("_PixelSize"); Prop("_PixelFPS"); Prop("_DitherStrength");
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (style!=null && (style.hasMixedValue || style.floatValue>10.5f))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Layered Paper and Bioluminescence",EditorStyles.boldLabel);
                if (style.hasMixedValue || style.floatValue<11.5f)
                {
                    Prop("_LayerScale"); Prop("_LayerShadow"); Prop("_LayerShadowWidth");
                    Prop("_ScallopScale"); Prop("_ScallopSize"); Prop("_PaperEdgeWidth");
                }
                if (style.hasMixedValue || style.floatValue>11.5f)
                {
                    Prop("_BioGlowColor"); Prop("_BioGlowStrength"); Prop("_BioTrailStrength");
                    Prop("_BioDensity"); Prop("_BioScale"); Prop("_BioFleckSize"); Prop("_BioPulseSpeed");
                    EditorGUILayout.HelpBox("Glowing trails use Water Interactor ripples and boat wakes. Bloom on your camera's Volume adds a halo around bright emission.",MessageType.None);
                }
            }

            if (Group(ref surfaceFoam,"Surface Foam Pattern"))
            {
                Texture("_SurfaceFoamMap","Surface Pattern"); Prop("_SurfaceFoamColor"); Prop("_SurfaceFoamShadowColor");
                Prop("_SurfaceFoamScale"); Prop("_SurfaceFoamStrength"); Prop("_SurfaceFoamCutoff");
                Prop("_SurfaceFoamVariation"); Prop("_SurfaceFoamDistortion");
                EditorGUILayout.HelpBox("This layer covers the water independently of wave height. Use it for Wind Waker-style graphic networks.",MessageType.None);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (Group(ref foam,"Crests, Shore and Interaction Foam"))
            {
                Prop("_UseFoam"); Texture("_FoamMap","Breaking-Crest Mask"); Texture("_ShoreFoamMap","Contact-Foam Mask");
                Prop("_FoamColor"); Prop("_FoamScale"); Prop("_FoamCutoff"); Prop("_FoamCoverage"); Prop("_CrestFoamStrength");
                Prop("_ShoreFoamWidth"); Prop("_ShoreFoamStrength");
                Prop("_ShoreWaveStrength"); Prop("_ShoreWaveSpacing"); Prop("_ShoreWaveSpeed"); Prop("_InteractionFoamStrength");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (Group(ref lighting,"Toon Lighting"))
            {
                Prop("_ReflectionStrength"); Prop("_HighlightStrength"); Prop("_OutlineStrength"); Prop("_FresnelSteps"); Prop("_EdgeFade");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            foreach (Object target in editor.targets)
            {
                Material material=(Material)target;
                if (material.GetFloat("_UseFoam")>0.5f) material.EnableKeyword("_FOAM"); else material.DisableKeyword("_FOAM");
            }
        }
    }
}
