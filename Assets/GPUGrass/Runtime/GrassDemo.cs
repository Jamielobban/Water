using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GPUGrass
{
    public sealed class GrassDemo : MonoBehaviour
    {
        public GpuGrass grass;
        public bool createGround;
        Material soil;
        Vector2 look;
        void Start()
        {
            if (createGround && grass && !grass.surface)
            {
                transform.position = new Vector3(8, 3, -30);
                transform.rotation = Quaternion.Euler(8, -12, 0);
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Meadow soil";
                ground.transform.SetParent(grass.transform, false);
                ground.transform.position = new Vector3(0, -.025f, 0);
                ground.transform.localScale = Vector3.one * 10.5f;
                soil = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                soil.SetColor("_BaseColor", new Color(.16f, .19f, .07f));
                soil.SetFloat("_Smoothness", 0);
                ground.GetComponent<Renderer>().sharedMaterial = soil;
            }
            if (grass && grass.surface)
            {
                if (grass.surface.Samples == null) grass.surface.Rebuild();
                transform.position = new Vector3(8, grass.surface.SurfaceBounds.max.y + 3, -30);
                transform.rotation = Quaternion.Euler(18, -12, 0);
            }
            look = new Vector2(transform.eulerAngles.y, transform.eulerAngles.x);
        }
        void OnDestroy() { if (soil) Destroy(soil); }
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var m = Mouse.current;
            if (k == null) return;
            if (k.pKey.wasPressedThisFrame && grass) grass.NextStyle();
            if (k.lKey.wasPressedThisFrame && grass) grass.debugLOD = !grass.debugLOD;
            if (m == null) return;
            if (m.rightButton.isPressed)
            {
                Vector2 delta = m.delta.ReadValue() * .12f;
                look.x += delta.x; look.y = Mathf.Clamp(look.y - delta.y, -85, 85);
                transform.rotation = Quaternion.Euler(look.y, look.x, 0);
                Vector3 move = new Vector3((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0),
                    (k.eKey.isPressed ? 1 : 0) - (k.qKey.isPressed ? 1 : 0),
                    (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
                transform.position += transform.TransformDirection(Vector3.ClampMagnitude(move, 1)) * Time.deltaTime * (k.leftShiftKey.isPressed ? 24 : 8);
            }
#endif
        }
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 440, 260), GUI.skin.box);
            GUILayout.Label("GPU MEADOW  /  THREE BOTANICAL VARIETIES");
            GUILayout.Label("Short turf  •  Broad meadow grass  •  Golden tall grass");
            GUILayout.Label("RMB + WASD to fly  |  Q / E height  |  Shift faster");
            if (grass)
            {
                GUILayout.Label("Style: " + grass.StyleName);
                if (GUILayout.Button("P  /  Next grass style")) grass.NextStyle();
                grass.debugLOD = GUILayout.Toggle(grass.debugLOD, "LOD colors [L]: green / amber / blue");
                GUILayout.Label("Wind strength");
                grass.wind = GUILayout.HorizontalSlider(grass.wind, 0, 3);
                GUILayout.Label($"{grass.bladeCount:N0} candidates  |  " + (grass.UsesSculptedShader ? "Sculpted geometry / 3 LODs" : (grass.style == GrassStyle.LowPoly ? "2 / 2 / 1" : "5 / 2 / 1") + " segment blades"));
                GUILayout.Label("GPU frustum culling + stable distance transitions");
            }
            if (!SystemInfo.supportsComputeShaders) GUILayout.Label("This device does not support compute shaders.");
            GUILayout.EndArea();
        }
    }
}
