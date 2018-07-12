using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.SpaceMapper
{
    [System.Serializable]
    public class DeActivateEvent : UnityEvent<bool>
    {
    }

    public class InvokeDeActivateEvent : MonoBehaviour {

        [HideInInspector]
        public DeActivateEvent DeActivate = new DeActivateEvent();
        [HideInInspector]
        public bool Available = true;
        [HideInInspector]
        public Material AvailableMaterial;
        [HideInInspector]
        public Material UnavailableMaterial;
        [HideInInspector]
        public Material SelectedMaterial;

        internal Renderer Renderer;
        //void OnEnable()
        //{
        //    if (DeActivate != null)
        //        DeActivate.Invoke(true);
        //}

        //private void OnDisable()
        //{
        //   if (DeActivate != null)
        //        DeActivate.Invoke(false);
        //}

        internal void TryChangeMaterial(Material material)
        {
            if (Renderer == null)
            {
                Renderer = GetComponent<Renderer>();
            }
            if (Renderer == null || Renderer.sharedMaterial == material)
                return;
            Renderer.sharedMaterial = material;
            var rends = GetComponentsInChildren<Renderer>();
            foreach (var rend in rends)
                rend.sharedMaterial = material;
        }

        internal void MakeAvailable(bool available)
        {
            Available = available;
            GetComponent<Renderer>().material = Available ? AvailableMaterial : UnavailableMaterial;
        }

        internal void FireChanged()
        {
            if (DeActivate != null)
                DeActivate.Invoke(Available);
        }

        [CanEditMultipleObjects]
        [CustomEditor(typeof(InvokeDeActivateEvent))]
        public class InvokeDeActivateEventEditor : Editor
        {
            //public override void OnInspectorGUI()
            //{
            //    DrawDefaultInspector();

            //    var myScript = Selection.activeGameObject.GetComponent<InvokeDeActivateEvent>();//(InvokeDeActivateEvent)target;
                
            //}

            //void OnEnable()
            //{
            //    Tools.hidden = true;
            //}

            //void OnDisable()
            //{
            //    Tools.hidden = false;
            //}
        }
    }
}