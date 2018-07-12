using OVRTouchSample;
using UnityEngine;

namespace Assets.Scripts.UserStudy
{
    public class HandCloseScript : MonoBehaviour
    {

        public SteamVR_TrackedController Controller;
        public Hand Hand;
        
        void Update () {
            Hand.Grab = Controller.triggerPressed;
        }
    }
}
