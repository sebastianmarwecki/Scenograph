using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.SpaceMapper;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.Petrinet
{
    [System.Serializable]
    public class TryFireEvent : UnityEvent<PetrinetTransition, bool>
    {
    }

    [RequireComponent(typeof(HardwareRequirements))]
    public class PetrinetTransition : MonoBehaviour
    {
        public string Name;
        public List<PetrinetCondition> In, Out;
        [HideInInspector]
        public static TryFireEvent TriedToFire = new TryFireEvent();
        [HideInInspector]
        public UnityEvent InConditionsChanged = new UnityEvent();
        public List<int> InTokens, OutTokens;
        [HideInInspector]
        public HardwareRequirements HardwareRequirements;
        public GameObject Preview;
        public GameObject PreviewObject;
        public Transform PhotoFrom, PhotoTo;
       // private Vector3 _previewPosition;
        //private Quaternion _previewRotation;

        //public Vector3 GetPreviewPosition()
        //{
        //    return Application.isPlaying ? _previewPosition : PreviewObject.transform.localPosition;
        //}

        //public Quaternion GetPreviewRotation()
        //{
        //    return Application.isPlaying ? _previewRotation : PreviewObject.transform.rotation;
        //}

        void Awake()
        {
           // _previewPosition = transform.InverseTransformPoint(PreviewObject.transform.position);
          //  _previewRotation = PreviewObject.transform.rotation;
            CheckProperties();
            HardwareRequirements = GetComponent<HardwareRequirements>();
        }

        internal void CheckProperties()
        {
            if (Name == "")
                Name = gameObject.name;

            while (OutTokens.Count < Out.Count)
                OutTokens.Add(1);
            if (OutTokens.Count > Out.Count)
                OutTokens.RemoveRange(Out.Count, OutTokens.Count - Out.Count);
            for (var i = 0; i < OutTokens.Count; i++)
                if (OutTokens[i] <= 0)
                    OutTokens[i] = 1;

            while (InTokens.Count < In.Count)
                InTokens.Add(1);
            if (InTokens.Count > In.Count)
                InTokens.RemoveRange(In.Count, InTokens.Count - In.Count);
            for (var i = 0; i < InTokens.Count; i++)
                if (InTokens[i] <= 0)
                    InTokens[i] = 1;
        }

        private void OnEnable()
        {
            PetrinetCondition.TokenUpdated.AddListener(CheckReadyToFire);
        }

        private void OnDisable()
        {
            PetrinetCondition.TokenUpdated.RemoveListener(CheckReadyToFire);
        }

        private void CheckReadyToFire(PetrinetCondition condition)
        {
            if (!In.Contains(condition))
                return;
            if (InConditionsChanged != null)
                InConditionsChanged.Invoke();
        }
    
        internal Dictionary<PetrinetCondition, bool> GetConditionsFulfilled()
        {
            CheckProperties();

            var dict = In.ToDictionary(i => i, i => i.Tokens >= InTokens[In.IndexOf(i)]);
            return dict;
        }

        internal bool CanFire()
        {
            var allIn = GetConditionsFulfilled().All(k => k.Value);
            return allIn;
        }
    
        internal bool TryFire()
        {
            if (!CanFire())
            {
                if(TriedToFire != null)
                    TriedToFire.Invoke(this, false);
                return false;
            }
            foreach (var o in Out)
            {
                o.Tokens += OutTokens[Out.IndexOf(o)];
                o.TokenUpdate();
            }
            foreach (var i in In)
            {
                i.Tokens -= InTokens[In.IndexOf(i)];
                i.TokenUpdate();
            }
            if (TriedToFire != null)
                TriedToFire.Invoke(this, true);
            return true;
        }
    }

    [CustomEditor(typeof(PetrinetTransition))]
    public class PetrinetTransitionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var myScript = (PetrinetTransition) target;
            myScript.CheckProperties();
        }
    }
}