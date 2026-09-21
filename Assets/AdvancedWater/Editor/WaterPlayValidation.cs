using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace AdvancedWater.Editor
{
    // Runs the actual MonoBehaviour lifecycle and physics, not a reimplementation of buoyancy.
    [InitializeOnLoad]
    public static class WaterPlayValidation
    {
        const string Key="AdvancedWater.PlayValidation";
        const string Report="Logs/AdvancedWaterValidation.txt";
        static WaterPlayValidation() { if (SessionState.GetBool(Key,false)) Hook(); }
        public static void Run()
        {
            EditorSceneManager.OpenScene(WaterSetup.Root+"/Demo/Water Demo.unity");
            SessionState.SetBool(Key,true);
            SessionState.SetInt(Key+".Exit",0);
            SessionState.SetString(Key+".Errors","");
            SessionState.SetFloat(Key+".Start",(float)EditorApplication.timeSinceStartup);
            Hook();
            EditorApplication.isPlaying=true;
        }
        static void Hook()
        {
            EditorApplication.update-=Tick; EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged-=State;EditorApplication.playModeStateChanged+=State;
            Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;
        }
        static void Log(string message,string stack,LogType type)
        {
            if (type==LogType.Error || type==LogType.Exception || type==LogType.Assert)
                SessionState.SetString(Key+".Errors",SessionState.GetString(Key+".Errors","")+message+"\n");
        }
        static void State(PlayModeStateChange state)
        {
            if (state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key,false))
            {
                int code=SessionState.GetInt(Key+".Exit",1);
                SessionState.EraseBool(Key);
                EditorApplication.Exit(code);
            }
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Key,false)) return;
            if (EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".Start",0)>90)
            { Fail(new Exception("Play-mode validation timed out."));return; }
            if (!EditorApplication.isPlaying || Time.timeSinceLevelLoad<4) return;
            EditorApplication.update-=Tick;
            try
            {
                string errors=SessionState.GetString(Key+".Errors","");
                if (!string.IsNullOrEmpty(errors)) throw new Exception("Play-mode errors:\n"+errors);
                WaterSurface water=Object.FindFirstObjectByType<WaterSurface>();
                int floats=0;
                foreach (WaterBuoyancy buoyancy in Object.FindObjectsByType<WaterBuoyancy>(FindObjectsSortMode.None))
                {
                    Rigidbody body=buoyancy.GetComponent<Rigidbody>();
                    Vector3 p=body.position;
                    if (float.IsNaN(p.y) || float.IsInfinity(p.y) || p.y<water.Level-2 || p.y>water.Level+2.5f
                        || body.linearVelocity.sqrMagnitude>100) throw new Exception("Unstable buoyancy on "+body.name);
                    floats++;
                }
                if (floats!=4) throw new Exception("Expected four buoyancy bodies in demo.");
                WaterDemoBoat boat=Object.FindFirstObjectByType<WaterDemoBoat>();
                if (Vector3.Distance(boat.transform.position,new Vector3(13,0.4f,8))<0.5f) throw new Exception("Demo boat did not move.");
                var properties=new MaterialPropertyBlock();water.SurfaceRenderer.GetPropertyBlock(properties);
                int rippleCount=properties.GetInt("_AW_RippleCount");
                if (rippleCount<=0) throw new Exception("Moving bodies did not emit ripples.");
                Camera camera=Camera.main;camera.aspect=1280f/720;
                WaterSetup.Capture(camera,"Logs/WaterPreviews/Play Mode.png");
                camera.transform.position=new Vector3(6,-1.5f,-5);camera.transform.LookAt(new Vector3(-8,-2,15));
                WaterSetup.Capture(camera,"Logs/WaterPreviews/Play Mode Underwater.png");
                errors=SessionState.GetString(Key+".Errors","");
                if (!string.IsNullOrEmpty(errors)) throw new Exception("Runtime render errors:\n"+errors);
                File.AppendAllText(Report,$"Play mode passed after {Time.timeSinceLevelLoad:F2}s: {floats} stable float bodies, moving boat, {rippleCount} ripple slots, runtime reflection and underwater captures.\n");
                EditorApplication.isPlaying=false;
            }
            catch (Exception e) { Fail(e); }
        }
        static void Fail(Exception exception)
        {
            EditorApplication.update-=Tick;
            File.AppendAllText(Report,exception+"\n");SessionState.SetInt(Key+".Exit",1);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying=false;
            else { SessionState.EraseBool(Key);EditorApplication.Exit(1); }
        }
    }
}
