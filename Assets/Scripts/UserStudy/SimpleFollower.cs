using UnityEngine;

namespace Assets.Scripts.UserStudy
{
    public class SimpleFollower : MonoBehaviour
    {
        public Transform Follow;
        public bool FollowPosition;
        public bool FollowRotation;
        public bool Global;
    
        void Update ()
        {
            if (Follow == null)
                return;

            if (FollowPosition)
            {
                if (Global)
                    transform.position = Follow.position;
                else
                    transform.localPosition = Follow.localPosition;
            }
            if (FollowRotation)
            {
                if (Global)
                    transform.rotation = Follow.rotation;
                else
                    transform.localRotation = Follow.localRotation;
            }
        }
    }
}
