using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Assets.Scripts.Petrinet
{
    [System.Serializable]
    public class TokenUpdated : UnityEvent<PetrinetCondition>
    {
    }
    
    public class PetrinetCondition : MonoBehaviour
    {
        public int Tokens;
        public ConditionType Type;
        public Transform VisualReferenceInEditor;
        public Transform PictureObject;
        public Text FloatingText;
        public RectTransform SizeRef;

        public static UnityEvent<PetrinetCondition> TokenUpdated = new TokenUpdated();

        public enum ConditionType
        {
            LogicalCondition,
            Place
        }
    
        private void Awake()
        {
            if (VisualReferenceInEditor == null)
                Debug.LogWarning(gameObject.name + " missing visual reference");
            if(TokenUpdated == null)
                TokenUpdated = new TokenUpdated();
        }
    
        public void TokenUpdate()
        {
            if(TokenUpdated != null)
                TokenUpdated.Invoke(this);
        }
        
    }

    [CustomEditor(typeof(PetrinetCondition))]
    public class PetrinetConditionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            //var myScript = (PetrinetCondition)target;
            //if (myScript.transform.childCount > 0 && myScript.Type == PetrinetCondition.ConditionType.LogicalCondition)
            //    GUILayout.Label("--- CANNOT HAVE OBJECTS UNDERNEATH ---");
            //if (myScript.transform.childCount > 0)
            //            GUILayout.Label("--- CANNOT HAVE OBJECTS UNDERNEATH ---");
            //switch (myScript.Type)
            //{

            //    case PetrinetCondition.ConditionType.LogicalCondition:
            //        if(myScript.transform.childCount > 0)
            //            GUILayout.Label("--- CANNOT HAVE OBJECTS UNDERNEATH ---");
            //        //myScript.GeneratedTransitionObject = null;
            //        break;
            //    case PetrinetCondition.ConditionType.Place:
            //        myScript.GeneratedTransitionObject = (GameObject) EditorGUILayout.("TransitionPrefab:", myScript.GeneratedTransitionObject, typeof(GameObject), false);
            //        break;
            //}
        }
    }
}