using SpaceMapper;
using UnityEngine;

namespace HelperClasses
{
    public class RotateToLongSide : AbstractPostLinkage
    {
        public override void Call()
        {
            var scale = transform.localScale;
            if (scale.x > scale.z) return;
            transform.localScale = new Vector3(scale.z, scale.y, scale.x);
            transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        }
    }
}
