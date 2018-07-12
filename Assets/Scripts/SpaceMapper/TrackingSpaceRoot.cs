using System.Collections.Generic;
using System.Linq;
using SpaceMapper;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.SpaceMapper
{
    [RequireComponent(typeof(AbstractPacker))]
    public class TrackingSpaceRoot : MonoBehaviour
    {
        public bool[,] TileAvailable;
        public char SplitChar = '_';
        //public string LayerName;
        [HideInInspector]
        public UnityEvent RequestRebuild = new UnityEvent();
        public Vector2 TileSize = new Vector2(1f, 1f);
        public Vector2 TotalSize = new Vector2(4f, 4f);
        public Material AvailableMaterial;
        public Material UnavailableMaterial;
        public Material SelectedMaterial;
        private Vector2Int _tileAmountTotal;

        private AbstractPacker _packer;

        public bool GetPacking(
            List<AbstractPacker.PackingRequest> multiRoomHardware,
            out Dictionary<AbstractPacker.PackingRequest, AbstractPacker.PackingResult> result)
        {
            return _packer.GetPacking(
                multiRoomHardware,
                this, out result);
        }

        private void Awake()
        {
            _packer = GetComponent<AbstractPacker>();
        }

        public float GetSpaceAvailable()
        {
            return GetTilesAvailable() * TileSize.x * TileSize.y;
        }

        public Vector2 GetSpaceFromPosition(Vector2Int position)
        {
            return GetSpaceFromPosition(position.x, position.y);
        }

        //public Vector2 GetSpace()
        //{
        //    new Vector2(TileSize.x * x + TileSize.x * 0.5f - TotalSize.x / 2f, TileSize.y * y + TileSize.y * 0.5f - TotalSize.y / 2f);
        //}

        public Vector2 GetSpaceFromPosition(int x, int y)
        {
            return new Vector2(TileSize.x * x + TileSize.x * 0.5f - TotalSize.x / 2f, TileSize.y * y + TileSize.y * 0.5f - TotalSize.y / 2f);
        }

        public int GetTilesAvailable()
        {
            if (TileAvailable == null)
                CheckTile();
            return TileAvailable.Cast<bool>().Count(ta => ta);
        }

        public Vector2Int GetTileAmountTotal()
        {
            return _tileAmountTotal;
        }

        public bool[,] GetAssignmentMatrix(Vector2Int pointer, Vector2Int allocated)
        {
            var ret = new bool[_tileAmountTotal.x, _tileAmountTotal.y];
            for (var i = pointer.x; i < pointer.x + allocated.x; i++)
            for (var j = pointer.y; j < pointer.y + allocated.y; j++)
                ret[i, j] = true;
            return ret;
        }

        void OnEnable()
        {
        //    var invokeChildren = GetComponentsInChildren<InvokeDeActivateEvent>(true);
        //    foreach (var ic in invokeChildren)
        //        ic.DeActivate.AddListener(CheckTilesChanged);
        }

        void OnDisable()
        {
        //    var invokeChildren = GetComponentsInChildren<InvokeDeActivateEvent>(true);
        //    foreach (var ic in invokeChildren)
        //        ic.DeActivate.RemoveListener(CheckTilesChanged);
        }

        internal void CheckTilesChanged(bool rebuildIfUpdate, bool rebuildIfNoUpdate)
        {
            var updated = CheckTile();
            if ((rebuildIfUpdate && updated || rebuildIfNoUpdate && !updated) && RequestRebuild != null)
                RequestRebuild.Invoke();
        }

        private bool CheckTile()
        {
            var children = transform.GetComponentsInChildren<InvokeDeActivateEvent>(true);//.Where(t => t.parent == parent);
            var indices = children.Select(c => ParseNameToIndex(c.name)).ToList();
            var maxX = indices.Max(i => i.x);
            var maxY = indices.Max(i => i.y);
            var newAvailableSpace = new bool[maxX+1, maxY+1];
            var isDifferent = TileAvailable == null || TileAvailable.Length != newAvailableSpace.Length;
            //availablePositions = new Vector2[matrixLength, matrixLength];
            //size = 0f;
            foreach (var child in children)
            {
                var index = ParseNameToIndex(child.name);
                var available = child.Available;//gameObject.activeSelf;
                newAvailableSpace[index.x, index.y] = available;
                //if(available)
                //    size += (child.localScale.x * child.localScale.z) * 100f;
                //availablePositions[ints[0], ints[1]] = new Vector2(child.transform.position.x, child.transform.position.z);
                if (!isDifferent)
                    isDifferent = newAvailableSpace[index.x, index.y] != TileAvailable[index.x, index.y];
            }
            TileAvailable = newAvailableSpace;
            _tileAmountTotal = new Vector2Int(TileAvailable.GetLength(0), TileAvailable.GetLength(1));
            return isDifferent;
        }

        private Vector2Int ParseNameToIndex(string name)
        {
            var childSplit = name.Split(SplitChar);
            var ints = childSplit.Select(int.Parse).ToArray();
            return new Vector2Int(ints[0], ints[1]);
        }

        private string ParseIndexToName(Vector2Int index)
        {
            var ret = index.x.ToString() + SplitChar + "" + index.y;
            return ret;
        }

        public bool SetAvailableTiles(List<Vector2Int> availableTiles, bool forceRebuildIfSame)
        {
            if (availableTiles.Any(at => at.x >= _tileAmountTotal.x || at.y >= _tileAmountTotal.y))
                return false;
            var children = transform.GetComponentsInChildren<InvokeDeActivateEvent>(true);
            var childrenXz = children.ToDictionary(c => c.name, c => c);
            for (var x = 0; x < _tileAmountTotal.x; x++)
            for (var z = 0; z < _tileAmountTotal.y; z++)
            {
                var position = new Vector2Int(x, z);
                var invoke = childrenXz[ParseIndexToName(position)];
                invoke.MakeAvailable(availableTiles.Contains(position));
            }
            CheckTilesChanged(true, forceRebuildIfSame);
            return true;
        }

        internal void Set()
        {
            if (Application.isPlaying)
                OnDisable();
            foreach (var trans in gameObject.GetComponentsInChildren<Transform>().Where(t => t != transform))
            {
                trans.parent = null;
                DestroyImmediate(trans.gameObject);
            }
            var xTiles = Mathf.FloorToInt(TotalSize.x / TileSize.x);
            var yTiles = Mathf.FloorToInt(TotalSize.y / TileSize.y);
            TileAvailable = new bool[xTiles,yTiles];
            for (var x = 0; x < xTiles; x++)
            {
                for (var y = 0; y < yTiles; y++)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    go.transform.parent = transform;
                    go.transform.localScale = new Vector3(TileSize.x * 0.1f, 1f, TileSize.y * 0.1f);
                    var getSpace = GetSpaceFromPosition(x, y);
                    go.transform.localPosition = new Vector3(getSpace.x, 0f, getSpace.y);//TileSize.x * x + TileSize.x*0.5f - TotalSize.x/2f, 0f, TileSize.y * y + TileSize.y * 0.5f - TotalSize.y / 2f);
                    var invoke = go.AddComponent<InvokeDeActivateEvent>();
                    invoke.AvailableMaterial = AvailableMaterial;
                    invoke.UnavailableMaterial = UnavailableMaterial;
                    invoke.SelectedMaterial = SelectedMaterial;
                    invoke.MakeAvailable(true);
                    var coll = go.GetComponent<Collider>();
                    DestroyImmediate(coll);
                    go.name = x + SplitChar.ToString() + y;
                    TileAvailable[x, y] = true;
                    //set layer
                    go.layer = gameObject.layer;//LayerMask.NameToLayer(LayerName);
                }
            }

            if (Application.isPlaying)
            {
                OnEnable();
                CheckTilesChanged(true, false);
            }
        }
    }


    [CustomEditor(typeof(TrackingSpaceRoot))]
    public class TrackingSpaceRootEditor : Editor
    {
        public override void OnInspectorGUI()
        {

            DrawDefaultInspector();

            var myScript = (TrackingSpaceRoot)target;

            if (GUILayout.Button("Set"))
            {
                myScript.Set();
            }
        }

        void OnEnable()
        {
            Tools.hidden = true;
            EditorApplication.update += Refocus;
        }

        private void Refocus()
        {
            var myScript = (TrackingSpaceRoot)target;
            if (Selection.activeGameObject == null)
            {
                Selection.activeGameObject = myScript.gameObject;
            }
        }

        void OnDisable()
        {
            Tools.hidden = false;
            EditorApplication.update -= Refocus;
        }

        public void OnSceneGUI()
        {
            var myScript = (TrackingSpaceRoot)target;
            //var firstInvoke = Selection.activeGameObject.GetComponent<InvokeDeActivateEvent>();//(InvokeDeActivateEvent)target;

            //var available = firstInvoke.Available;
            //if (GUILayout.Button(available ? "Make unavailable" : "Make available"))
            //{

            //}

            
            //if (pressed)
            //{
            //    foreach (var invoke in invokes)
            //    {
            //        invoke.MakeAvailable(!available);
            //        // if (Selection.activeObject != target)
            //    }
            //    //}
            //    myScript.CheckTilesChanged(true);
            //    Selection.activeGameObject = null;
            //}
            //else
            //{
            //    foreach(var invoke in invokes)
            //    {
            //        invoke.TryChangeMaterial(myScript.SelectedMaterial);
            //    }
            //}
        }
    }
}
