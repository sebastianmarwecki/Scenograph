using System;
using SpaceMapper;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.SpaceMapper
{
    public class HardwareRequirements : MonoBehaviour
    {
        public Vector2 Requirement;
        public AbstractPacker.WallPrefs WallPrefsX;
        public AbstractPacker.WallPrefs WallPrefsY;
        //public int ValueX;
        //public int ValueY;
        public bool IsSemiWall = false;
        public bool PlaceFarAway = false;
        public Transform VisualReferenceInEditor;

        [HideInInspector]
        public UnityEvent ReAssigned = new UnityEvent();
        [HideInInspector]
        //public bool[,] AssignedMemorySpace;
        public Vector2Int? MemoryPointer;
        [HideInInspector]
        public Vector2Int AllocatedMemory;
        [HideInInspector]
        public bool Reversed;
        [HideInInspector]
        public TrackingSpaceRoot Ram;
        //[HideInInspector]
        //public Vector2[,] MemorySpacePointer;
        //[HideInInspector]
        //public Vector2 MemoryFragmentSpaceTileSize;

        public void Assign(TrackingSpaceRoot ram, AbstractPacker.PackingResult assignment)//, Vector2[,] memorySpacePointer, Vector2 memoryFragmentSpaceTileSize)
        {
            MemoryPointer = assignment.Pointer;
            Ram = ram;
            AllocatedMemory = assignment.Allocation;
            Reversed = assignment.Reverse;
            if (ReAssigned != null)
                ReAssigned.Invoke();
        }

        public bool[,] GetAssignmentMatrix()
        {
            if (Ram == null || MemoryPointer == null)
                return null;
            return Ram.GetAssignmentMatrix(MemoryPointer.Value, AllocatedMemory);
        }
    }

    [CustomEditor(typeof(HardwareRequirements))]
    public class HardwareRequirementsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var myScript = (HardwareRequirements)target;
            if (Application.isPlaying)
            {
                var assignment = myScript.GetAssignmentMatrix();
                GUILayout.Label("");
                GUILayout.Label("AssignedMemorySpace");
                GUILayout.Label(assignment == null ? "NOT ASSIGNED" : Matrix2String(assignment, myScript.Ram.TileAvailable));
            }
        }

        private static string Matrix2String(bool[,] matrix, bool[,] total)
        {
            var ret = "";
            for (var z = matrix.GetLength(1) - 1; z >= 0; z--)
            for (var x = 0; x < matrix.GetLength(0); x++)
            {
                ret += !total[x, z] ? "X" : matrix[x, z] ? "1" : "0";
                if (x == matrix.GetLength(0) - 1 && z > 0)
                    ret += Environment.NewLine;
            }
            return ret;
        }
    }
}