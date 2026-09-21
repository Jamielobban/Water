using UnityEngine;

namespace AdvancedWater
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class WaterDemoBoat : MonoBehaviour
    {
        public Vector3 center=new Vector3(4,0,8);
        public float radius=9;
        public float speed=2;
        Rigidbody body;
        void Awake() => body=GetComponent<Rigidbody>();
        void FixedUpdate()
        {
            float angle=Time.time*speed/Mathf.Max(radius,1);
            Vector3 target=center+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
            Vector3 delta=target-transform.position; delta.y=0;
            Vector3 desired=Vector3.ClampMagnitude(delta,1)*speed;
            Vector3 velocity=body.linearVelocity; velocity.y=0;
            body.AddForce((desired-velocity)*2,ForceMode.Acceleration);
            if (desired.sqrMagnitude>0.1f)
            {
                float error=Vector3.SignedAngle(transform.forward,desired,Vector3.up)*Mathf.Deg2Rad;
                body.AddTorque(Vector3.up*(error*2-body.angularVelocity.y),ForceMode.Acceleration);
            }
        }
    }
}
