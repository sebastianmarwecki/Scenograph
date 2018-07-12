using SpaceMapper;
using UnityEngine;

namespace HelperClasses
{
    public class Resizer : AbstractPostLinkage
    {
        public Vector3 GlobalScale = Vector3.one;
        public override void Call ()
        {
            var parent = transform.parent;
            transform.parent = null;
            transform.localScale = GlobalScale;
            transform.parent = parent;
        }
    }
}
