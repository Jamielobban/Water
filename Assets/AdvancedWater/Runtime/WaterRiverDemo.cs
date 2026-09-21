using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AdvancedWater
{
    [ExecuteAlways]
    public sealed class WaterRiverDemo : MonoBehaviour
    {
        public Material riverMaterial;
        public Material[] riverStyles;
        [Min(0)] public int styleIndex;
        public Material rockMaterial;
        public Material groundMaterial;
        GameObject generated;
        Camera demoCamera;
        Vector2 look;
        bool showHelp=true;
        WaterRiver demoRiver;

        public void NextStyle()
        {
            if (riverStyles==null || riverStyles.Length==0) return;
            styleIndex=(styleIndex+1)%riverStyles.Length;
            ApplyStyle();
        }
        void ApplyStyle()
        {
            if (!demoRiver) return;
            Material selected=riverMaterial;
            if (riverStyles!=null && riverStyles.Length>0)
            {
                styleIndex=Mathf.Clamp(styleIndex,0,riverStyles.Length-1);
                if (riverStyles[styleIndex]) selected=riverStyles[styleIndex];
            }
            demoRiver.material=selected;
            demoRiver.GetComponent<MeshRenderer>().sharedMaterial=selected;
        }

        void OnEnable() { Build(); }
        void OnDisable()
        {
            if (!generated) return;
            if (Application.isPlaying) Destroy(generated); else DestroyImmediate(generated);
        }
        void Build()
        {
            if (generated) return;
            generated=new GameObject("River Demo Environment") { hideFlags=HideFlags.DontSave };
            generated.transform.SetParent(transform,false);
            var water=new GameObject("River and Waterfall") { layer=4 };
            water.transform.SetParent(generated.transform,false);
            var river=water.AddComponent<WaterRiver>();
            demoRiver=river;
            river.material=riverMaterial;
            river.Rebuild();
            ApplyStyle();

            // Follow both banks so the upper reach, cliff and lower pool form one valley.
            for (int i=0;i<river.points.Length-1;i++)
            {
                var a=river.points[i]; var b=river.points[i+1];
                Vector3 direction=b.position-a.position;
                Vector3 side=Vector3.Cross(Vector3.up,direction).normalized;
                for (int bank=-1;bank<=1;bank+=2)
                {
                    Vector3 center=(a.position+b.position)*0.5f;
                    float width=(a.width+b.width)*0.5f;
                    center+=side*bank*(width*0.5f+3.4f);
                    float top=Mathf.Max(a.position.y,b.position.y)+0.45f;
                    float height=top+4;
                    center.y=top-height*0.5f;
                    var block=Primitive("Riverbank",PrimitiveType.Cube,center,
                        new Vector3(7,height,new Vector2(direction.x,direction.z).magnitude+2),groundMaterial);
                    if (side.sqrMagnitude>0.01f) block.transform.localRotation=Quaternion.LookRotation(new Vector3(direction.x,0,direction.z));
                    Vector3 rock=(a.position+b.position)*0.5f+side*bank*(width*0.5f+0.3f);
                    rock.y+=0.2f;
                    var stone=Primitive("Bank Boulder",PrimitiveType.Sphere,rock,
                        new Vector3(2.2f,1.4f+(i%3)*0.4f,2.8f),rockMaterial);
                    stone.transform.localRotation=Quaternion.Euler(i*17,bank*i*31,i*11);
                }
            }
            Primitive("Upper Riverbed",PrimitiveType.Cube,new Vector3(-2,6.8f,-15),new Vector3(18,1.5f,25),rockMaterial);
            Primitive("Waterfall Cliff",PrimitiveType.Cube,new Vector3(0,3,-2),new Vector3(8,7,2),rockMaterial);
            Primitive("Lower Riverbed",PrimitiveType.Cube,new Vector3(3,-1.4f,18),new Vector3(28,2,33),groundMaterial);

            var sun=new GameObject("River Sun");
            sun.transform.SetParent(generated.transform,false);
            sun.transform.localRotation=Quaternion.Euler(40,-35,0);
            var light=sun.AddComponent<Light>(); light.type=LightType.Directional;
            light.color=new Color(1,0.93f,0.8f); light.intensity=1.5f;
            var cameraObject=new GameObject("River Demo Camera") { tag="MainCamera" };
            cameraObject.transform.SetParent(generated.transform,false);
            demoCamera=cameraObject.AddComponent<Camera>();
            demoCamera.clearFlags=CameraClearFlags.SolidColor;
            demoCamera.backgroundColor=new Color(0.36f,0.55f,0.64f);
            demoCamera.nearClipPlane=0.1f; demoCamera.farClipPlane=500;
            cameraObject.AddComponent<AudioListener>();
            ResetView();
        }
        GameObject Primitive(string label,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(type); go.name=label;
            go.transform.SetParent(generated.transform,false);
            go.transform.localPosition=position; go.transform.localScale=scale;
            if (material) go.GetComponent<Renderer>().sharedMaterial=material;
            return go;
        }
        void ResetView()
        {
            demoCamera.transform.localPosition=new Vector3(24,17,30);
            demoCamera.transform.LookAt(transform.TransformPoint(new Vector3(0,4,0)));
            look=new Vector2(demoCamera.transform.eulerAngles.y,demoCamera.transform.eulerAngles.x);
        }
        void Update()
        {
            ApplyStyle();
            if (!Application.isPlaying || !demoCamera) return;
#if ENABLE_INPUT_SYSTEM
            var keyboard=Keyboard.current; var mouse=Mouse.current;
            if (keyboard==null || mouse==null) return;
            if (keyboard.hKey.wasPressedThisFrame) showHelp=!showHelp;
            if (keyboard.rKey.wasPressedThisFrame) ResetView();
            if (keyboard.pKey.wasPressedThisFrame) NextStyle();
            if (!mouse.rightButton.isPressed) return;
            Vector2 delta=mouse.delta.ReadValue()*0.12f;
            look.x+=delta.x; look.y=Mathf.Clamp(look.y-delta.y,-89,89);
            demoCamera.transform.rotation=Quaternion.Euler(look.y,look.x,0);
            Vector3 move=new Vector3((keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0),
                (keyboard.eKey.isPressed?1:0)-(keyboard.qKey.isPressed?1:0),
                (keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0));
            demoCamera.transform.position+=demoCamera.transform.TransformDirection(Vector3.ClampMagnitude(move,1))
                *Time.deltaTime*(keyboard.leftShiftKey.isPressed?30:10);
#endif
        }
        void OnGUI()
        {
            if (!Application.isPlaying || !showHelp) return;
            GUI.Box(new Rect(18,18,480,125),"RIVER & WATERFALL DEMO");
            GUI.Label(new Rect(32,45,450,24),"Right mouse + WASD: fly | Q/E: down/up | Shift: fast");
            GUI.Label(new Rect(32,70,450,24),"R: reset view | P: next style | H: hide help");
            if (GUI.Button(new Rect(32,102,120,25),"Next river style")) NextStyle();
            GUI.Label(new Rect(164,102,320,25),demoRiver && demoRiver.material?demoRiver.material.name:"");
        }
    }
}
