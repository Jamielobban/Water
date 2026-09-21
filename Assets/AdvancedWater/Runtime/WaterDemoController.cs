using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AdvancedWater
{
    public sealed class WaterDemoController : MonoBehaviour
    {
        public WaterSurface water;
        public WaterProfile[] presets;
        public float moveSpeed=12;
        public bool showHelp=true;
        Vector2 look;
        int presetIndex;
        void Start() { look=new Vector2(transform.eulerAngles.y,transform.eulerAngles.x); }
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard=Keyboard.current; Mouse mouse=Mouse.current;
            if (keyboard==null || mouse==null) return;
            if (mouse.rightButton.isPressed)
            {
                Vector2 delta=mouse.delta.ReadValue()*0.12f;
                look.x+=delta.x; look.y=Mathf.Clamp(look.y-delta.y,-89,89);
                transform.rotation=Quaternion.Euler(look.y,look.x,0);
                Vector3 move=new Vector3((keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0),
                    (keyboard.eKey.isPressed?1:0)-(keyboard.qKey.isPressed?1:0),
                    (keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0));
                transform.position+=transform.TransformDirection(Vector3.ClampMagnitude(move,1))*moveSpeed
                    *(keyboard.leftShiftKey.isPressed?3:1)*Time.deltaTime;
            }
            if (keyboard.pKey.wasPressedThisFrame) NextPreset();
            if (keyboard.hKey.wasPressedThisFrame) showHelp=!showHelp;
            if (mouse.leftButton.wasPressedThisFrame && water)
            {
                Ray ray=GetComponent<Camera>().ScreenPointToRay(mouse.position.ReadValue());
                if (new Plane(Vector3.up,new Vector3(0,water.Level,0)).Raycast(ray,out float distance))
                {
                    Vector3 p=ray.GetPoint(distance);
                    if (water.Contains(p)) water.AddRipple(p,0.8f,1.5f,0.9f);
                }
            }
#endif
        }
        void NextPreset()
        {
            if (!water || presets==null || presets.Length==0) return;
            presetIndex=(presetIndex+1)%presets.Length;
            water.profile=presets[presetIndex]; water.Refresh();
        }
        void OnGUI()
        {
            if (!showHelp) return;
            GUI.Box(new Rect(18,18,430,105),"ADVANCED WATER  /  URP");
            GUI.Label(new Rect(32,44,405,24),"Right mouse + WASD: fly  |  Q / E: down / up  |  Shift: fast");
            GUI.Label(new Rect(32,66,405,24),"Left click: ripple  |  P: next preset  |  H: hide help");
            GUI.Label(new Rect(32,88,405,24),"Preset: "+(water && water.profile?water.profile.name:"None"));
        }
    }
}
