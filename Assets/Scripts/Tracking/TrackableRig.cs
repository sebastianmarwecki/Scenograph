using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace Assets.Scripts.Tracking
{
    public class TrackableRig : MonoBehaviour
    {
        public Transform ToFollow;
        public bool IsCamera;

        void Start()
        {
            InputTracking.disablePositionalTracking = true;
            var kids = GetComponentsInChildren<Transform>().Where(t => t.parent == transform).ToList();
            foreach (var kid in kids)
                kid.localPosition = Vector3.zero;
        }
        void Update()
        {
            if (ToFollow != null)
            {
                transform.position = ToFollow.position;//-InputTracking.GetLocalPosition(XRNode.CenterEye);
                transform.rotation = ToFollow.rotation * (IsCamera ? Quaternion.Inverse(InputTracking.GetLocalRotation(XRNode.CenterEye)) : Quaternion.identity);
            }
            else if(IsCamera)
            {
                transform.rotation = Quaternion.Inverse(InputTracking.GetLocalRotation(XRNode.CenterEye));
            }
        }
    }
}
