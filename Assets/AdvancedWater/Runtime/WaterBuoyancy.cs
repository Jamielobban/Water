using UnityEngine;

namespace AdvancedWater
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
    public sealed class WaterBuoyancy : MonoBehaviour
    {
        [Tooltip("Leave empty to find the nearest water surface at each sample point.")]
        public WaterSurface water;
        [Tooltip("Local-space float points. Spread them around a boat hull for roll and pitch.")]
        public Vector3[] floatPoints = {new Vector3(-0.5f,-0.25f,-0.7f),new Vector3(0.5f,-0.25f,-0.7f),
            new Vector3(-0.5f,-0.25f,0.7f),new Vector3(0.5f,-0.25f,0.7f)};
        [Min(0.01f)] public float floatDepth=0.5f;
        [Range(0,10)] public float waterDrag=2;
        [Range(0,10)] public float angularWaterDrag=1;
        Rigidbody body;
        void Awake() => body=GetComponent<Rigidbody>();
        void FixedUpdate()
        {
            if (floatPoints==null || floatPoints.Length==0) return;
            float submerged=0;
            foreach (Vector3 local in floatPoints)
            {
                Vector3 point=transform.TransformPoint(local);
                WaterSurface surface=water?water:WaterSurface.Find(point);
                if (!surface || !surface.isActiveAndEnabled || !surface.Sample(point,out float height,out _)) continue;
                float depth=height-point.y;
                if (depth<=0) continue;
                float amount=Mathf.Clamp(depth/Mathf.Max(0.01f,floatDepth),0,2);
                Vector3 acceleration=-Physics.gravity*amount-body.GetPointVelocity(point)*waterDrag*Mathf.Min(amount,1);
                body.AddForceAtPosition(acceleration/floatPoints.Length,point,ForceMode.Acceleration);
                submerged+=Mathf.Min(amount,1)/floatPoints.Length;
            }
            body.AddTorque(-body.angularVelocity*angularWaterDrag*submerged,ForceMode.Acceleration);
        }
        void OnDrawGizmosSelected()
        {
            if (floatPoints==null) return;
            Gizmos.color=Color.cyan;
            foreach (Vector3 point in floatPoints) Gizmos.DrawWireSphere(transform.TransformPoint(point),0.08f);
        }
    }
}
