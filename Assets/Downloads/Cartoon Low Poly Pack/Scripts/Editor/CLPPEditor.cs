using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(DoorsAndDrawers)), CanEditMultipleObjects]
public class CLPPEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DoorsAndDrawers myScript = (DoorsAndDrawers)target;
		DrawDefaultInspector();
		EditorGUILayout.LabelField("Location", EditorStyles.boldLabel);
		GUILayout.BeginHorizontal();
		if(GUILayout.Button("Save Start Location", GUILayout.Width(150)))
		{
			myScript.SaveStartLocation();
		}
		if(GUILayout.Button("Save End Location", GUILayout.Width(150)))
		{
			myScript.SaveEndLocation();
		}
		GUILayout.EndHorizontal();
		EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
		GUILayout.BeginHorizontal();
		if(GUILayout.Button("Save Start Rotation", GUILayout.Width(150)))
		{
			myScript.SaveStartRotation();
		}
		if(GUILayout.Button("Save End Rotation", GUILayout.Width(150)))
		{
			myScript.SaveEndRotation();
		}
		GUILayout.EndHorizontal();
		EditorGUILayout.LabelField("", EditorStyles.boldLabel);
		GUILayout.BeginHorizontal();
		if(GUILayout.Button("Open/Close", GUILayout.Width(150)))
		{
			myScript.Activate();
		}
		if(GUILayout.Button ("Clear All", GUILayout.Width(150)))
		{
			myScript.startLoc = Vector3.zero;
			myScript.endLoc = Vector3.zero;
			myScript.startRot = Quaternion.identity;
			myScript.startRot.w = 0;
			myScript.endRot = Quaternion.identity;
			myScript.endRot.w = 0;
		}
		GUILayout.EndHorizontal();
	}
}
