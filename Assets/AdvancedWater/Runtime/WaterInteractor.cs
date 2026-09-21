using UnityEngine;

namespace AdvancedWater
{
    [DisallowMultipleComponent]
    public sealed class WaterInteractor : MonoBehaviour
    {
        public WaterSurface water;
        public Vector3 localOffset;
        [Header("Boat Wake")]
        public bool boatWake;
        [Min(0.1f)] public float wakeHalfWidth=0.55f;
        [Range(0,1)] public float wakeStrength=0.8f;
        [Min(0)] public float wakeSpread=0.45f;
        [Min(0.1f)] public float wakeLifetime=8;
        [Min(0.1f)] public float wakeFullSpeed=2.5f;
        Vector3 wakePrevious;
        float wakePreviousTime;
        WaterSurface wakeSurface;
        bool wakeTracking;
        [Min(0.02f)] public float interval=0.45f;
        [Min(0)] public float minimumSpeed=0.15f;
        [Min(0.01f)] public float contactDistance=0.6f;
        [Min(0)] public float amplitude=0.12f;
        [Min(0.01f)] public float propagationSpeed=1;
        [Min(0.05f)] public float width=0.6f;
        [Tooltip("Optional particle system for actual splash spray. Ripples and wake foam work without it.")]
        public ParticleSystem splashParticles;
        Vector3 previous;
        float nextEmission;
        bool wasTouching;
        void OnEnable() { previous=transform.TransformPoint(localOffset); wasTouching=false; nextEmission=Time.time; wakeTracking=false; }
        void FixedUpdate()
        {
            Vector3 position=transform.TransformPoint(localOffset);
            float speed=Vector3.Distance(previous,position)/Mathf.Max(Time.fixedDeltaTime,0.001f);
            Vector3 horizontalMotion=position-previous; horizontalMotion.y=0;
            float horizontalSpeed=horizontalMotion.magnitude/Mathf.Max(Time.fixedDeltaTime,0.001f);
            previous=position;
            WaterSurface surface=water?water:WaterSurface.Find(position);
            bool touching=surface && surface.isActiveAndEnabled && surface.Sample(position,out float height,out _)
                && Mathf.Abs(position.y-height)<contactDistance;
            if (boatWake)
            {
                UpdateWake(surface,position,horizontalSpeed,touching);
                wasTouching=touching;
                return;
            }
            // Bobbing alone must not emit a continuous wake, and re-entry must
            // respect the same cooldown as sustained contact.
            if (touching && Time.time>=nextEmission && (!wasTouching || horizontalSpeed>=minimumSpeed))
            {
                surface.AddRipple(position,amplitude*Mathf.Clamp(speed*0.3f,0.5f,3),propagationSpeed,width);
                if (!wasTouching && splashParticles)
                {
                    splashParticles.transform.position=position;
                    splashParticles.Emit(Mathf.Clamp(Mathf.RoundToInt(speed*8),6,60));
                }
                nextEmission=Time.time+Mathf.Max(0.02f,interval);
            }
            wasTouching=touching;
        }
        void UpdateWake(WaterSurface surface,Vector3 position,float speed,bool touching)
        {
            float now=WaterSurface.Clock;
            if (!touching || speed<minimumSpeed)
            { wakeTracking=false; return; }
            if (!wakeTracking || wakeSurface!=surface)
            {
                wakePrevious=position; wakePreviousTime=now; wakeSurface=surface;
                wakeTracking=true;
                return;
            }
            float elapsed=now-wakePreviousTime;
            if (elapsed<0.3f) return;
            Vector3 travel=position-wakePrevious; travel.y=0;
            // Do not connect a teleport or a long pause with a stripe of foam.
            if (elapsed<1 && travel.magnitude<Mathf.Max(3,wakeFullSpeed*elapsed*4))
                surface.AddWakeSegment(wakePrevious,position,wakePreviousTime,now,wakeHalfWidth,
                    wakeStrength*Mathf.SmoothStep(0,1,speed/Mathf.Max(wakeFullSpeed,0.1f)),wakeSpread,wakeLifetime);
            wakePrevious=position; wakePreviousTime=now;
        }

        public void Splash(float strength=1)
        {
            Vector3 p=transform.TransformPoint(localOffset);
            WaterSurface surface=water?water:WaterSurface.Find(p);
            if (surface) surface.AddRipple(p,amplitude*Mathf.Max(0,strength),propagationSpeed,width);
            if (splashParticles) { splashParticles.transform.position=p; splashParticles.Emit(20); }
        }
    }
}
