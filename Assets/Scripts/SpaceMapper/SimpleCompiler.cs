using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Petrinet;
using UnityEngine;

namespace Assets.Scripts.SpaceMapper
{
    public class SimpleCompiler : AbstractCompiler
    {
        public LinkInformation Info = LinkInformation.ShowNoWalls;
        public List<GameObject> TransitionVisualizations, ChangeOnFired;
        public string ControllerName;
        public List<Collider> TriggerAreasPerTransition;
        public Collider GlobalTriggerAreaForAllTransitions;
        public Collider TriggerField;
        public float TriggerDistance = 0.5f;
        public Trigger TransitionTrigger;
        public List<Renderer> InteractiveRenderers;

        public SteamVR_TrackedController Controller;

        public enum Trigger
        {
            Point,
            Proximity
        }
        public bool TriggerAutomatically = false;
        public Material Ready, NotReady;

        public List<bool> _startStates;
        private Dictionary<Renderer, Material> _normalMats;

        private List<PetrinetTransition> _readyToFire;
        private bool _compiled;

        protected virtual void SetVars()
        {
            var controllerGo = GameObject.Find(ControllerName);
            Controller = controllerGo.GetComponentInChildren<SteamVR_TrackedController>(true);
            _readyToFire = new List<PetrinetTransition>();
            _compiled = true;
        }

        public override void Recompile(TrackingSpaceRoot ram)
        {
            var memoryPointer = new Vector2Int();
            var allocatedMemory = HardwareRequirements.AllocatedMemory;
            var reversed = HardwareRequirements.Reversed;
            if (HardwareRequirements.MemoryPointer != null)
                memoryPointer = HardwareRequirements.MemoryPointer.Value;
            else Debug.LogError("pointer should not be zero");
            var tilesAvailable = ram.TileAvailable;
            var dimensions = ram.GetTileAmountTotal();
            
            //if (PositionAtPointerElseMeanAllocated)
            //{
            //    var pos = ram.GetSpaceFromPosition(memoryPointer);
            //    position = new Vector3(pos.x, 0f, pos.y);
            //}
            //else
            //{
            var pos = Vector2.zero;
            for (var i = 0; i < allocatedMemory.x; i++)
            for (var j = 0; j < allocatedMemory.y; j++)
                pos += ram.GetSpaceFromPosition(memoryPointer.x + i, memoryPointer.y + j);
            pos /= allocatedMemory.x * allocatedMemory.y;
            var position = new Vector3(pos.x, 0, pos.y);
            //}
            transform.position = position;

            //check rotation
            //check all walls
            var upperWall = true;
            var rightWall = true;
            var lowerWall = true;
            var leftWall = true;
            for (var x = memoryPointer.x; x < memoryPointer.x + allocatedMemory.x; x++)
            for (var z = memoryPointer.y; z < memoryPointer.y + allocatedMemory.y; z++)
            {
                if (x == memoryPointer.x)
                    leftWall = leftWall && (x == 0 || !tilesAvailable[x - 1, z]);
                if (z == memoryPointer.y)
                    lowerWall = lowerWall && (z == 0 || !tilesAvailable[x, z - 1]);
                if (z == memoryPointer.y + allocatedMemory.y - 1)
                    upperWall = upperWall && (z + 1 == dimensions.y || !tilesAvailable[x, z + 1]);
                if (x == memoryPointer.x + allocatedMemory.x - 1)
                    rightWall = rightWall && (x + 1 == dimensions.x || !tilesAvailable[x + 1, z]);
            }
            if (upperWall && !reversed)
                transform.localRotation = Quaternion.Euler(0f, 0f, 0f); 
            else if (rightWall && reversed)
                transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            else if (lowerWall && !reversed)
                transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            else if(leftWall && reversed)
                transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            else if (upperWall)
                transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            else if (rightWall)
                transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            else if (lowerWall)
                transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            else if (leftWall)
                transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            SetVars();
        }

        public override void UpdateTransitions(Dictionary<PetrinetTransition, bool> visualize, Dictionary<PetrinetTransition, bool> readyToFire)
        {
            _readyToFire = readyToFire.Where(r => r.Value).Select(r => r.Key).ToList();

            for (var i = 0; i < Mathf.Min(TransitionVisualizations.Count, Transitions.Count); i++)
            {
                var viz = visualize[Transitions[i]];
                TransitionVisualizations[i].SetActive(viz);
                //foreach (var a in ChangeOnFired)
                //    a.SetActive(!a.activeSelf);
            }
        }

        private void Awake()
        {
            _normalMats = new Dictionary<Renderer, Material>();
            foreach (var rend in InteractiveRenderers)
                if(rend.gameObject.layer == gameObject.layer)
                    _normalMats.Add(rend, rend.material);
            _startStates = ChangeOnFired.Select(c => c.activeSelf).ToList();
        }

        protected virtual bool CanTrigger(out PetrinetTransition transition)
        {
            transition = null;
            return InsideTriggerField() && (CanTriggerFirstAble(out transition) || CanTriggerClosest(out transition));
        }
        
        private bool InsideTriggerField()
        {
            return TriggerField == null || TriggerField.bounds.Contains(Controller.transform.position);
        }

        protected void Update()
        {
            if (!_compiled)
                return;
            
            Material mat = null;
            PetrinetTransition transition;
            var canTrigger = CanTrigger(out transition);
            if (canTrigger)
            {
                var readyToFire = _readyToFire.Contains(transition);
                var trigger = TriggerAutomatically || Controller.triggerPressed;//Input.GetMouseButton(0);

                if (trigger)
                    transition.TryFire();

                mat = readyToFire ? Ready : NotReady;
            }
            ChangeMaterial(mat);
        }

        public override void Reset()
        {
            for (var i = 0; i < ChangeOnFired.Count; i++)
                ChangeOnFired[i].SetActive(_startStates[i]);
        }

        public override void TransitionFired(PetrinetTransition transition)
        {
            foreach (var a in ChangeOnFired)
                a.SetActive(!a.activeSelf);
        }

        protected bool CanTriggerFirstAble(out PetrinetTransition first)
        {
            var ret = false;
            first = null;
            if (GlobalTriggerAreaForAllTransitions == null)
            {
            } else if (GlobalTriggerAreaForAllTransitions.bounds.Contains(Controller.transform.position))
            {
                ret = true;
            }
            else
            {
                var forward = Controller.transform.forward;
                switch (TransitionTrigger)
                {
                    case Trigger.Point:
                        RaycastHit info;
                        if (GlobalTriggerAreaForAllTransitions.Raycast(new Ray(Controller.transform.position, forward), out info, 10f))
                        {
                            ret = true;
                        }
                        break;
                    case Trigger.Proximity:
                        var distance = GlobalTriggerAreaForAllTransitions.bounds.Contains(Controller.transform.position) ?
                            0f :
                            Vector3.Distance(Controller.transform.position, GlobalTriggerAreaForAllTransitions.ClosestPointOnBounds(Controller.transform.position));
                        if (distance < TriggerDistance)
                        {
                            ret = true;
                        }
                        break;
                }
            }
            
            if (!ret) return false;
            first = Transitions.FirstOrDefault(t => t.GetConditionsFulfilled().Values.Any(v => v));
            if (first == null)
                ret = false;
            return ret;
        }

        protected bool CanTriggerClosest(out PetrinetTransition closest)
        {
            var forward = Controller.transform.forward;
            var distanceClosest = float.MaxValue;
            closest = null;
            for (var i = 0; i < TriggerAreasPerTransition.Count; i++)
            {
                if (!TriggerAreasPerTransition[i].gameObject.activeSelf)
                    continue;
                switch (TransitionTrigger)
                {
                    case Trigger.Point:
                        RaycastHit info;
                        if (TriggerAreasPerTransition[i].bounds.Contains(Controller.transform.position))
                        {
                            distanceClosest = 0f;
                            closest = Transitions[i];
                        }
                        if (TriggerAreasPerTransition[i].Raycast(new Ray(Controller.transform.position, forward), out info, 10f))
                        {
                            if (info.distance < distanceClosest)
                            {
                                distanceClosest = info.distance;
                                closest = Transitions[i];
                            }
                        }
                        break;
                    case Trigger.Proximity:
                        var distance = TriggerAreasPerTransition[i].bounds.Contains(Controller.transform.position) ? 
                            0f :
                            Vector3.Distance(Controller.transform.position, TriggerAreasPerTransition[i].ClosestPointOnBounds(Controller.transform.position));
                        if (distance < distanceClosest)
                        {
                            distanceClosest = distance;
                            closest = Transitions[i];
                        }
                        break;
                }
            }
            return distanceClosest < TriggerDistance;
        }

        private void ChangeMaterial(Material material)
        {
            foreach (var rend in _normalMats.Keys)
            {
                if (rend == null || rend.material == material)
                    continue;
                rend.material = material ?? _normalMats[rend];
            }
        }

        public override LinkInformation GetLinkInformation()
        {
            return Info;
        }
    }
}
