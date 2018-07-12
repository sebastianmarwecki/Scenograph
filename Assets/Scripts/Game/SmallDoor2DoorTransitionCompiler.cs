using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Petrinet;
using Assets.Scripts.SpaceMapper;
using UnityEngine;

namespace Assets.Scripts.Game
{
    public class SmallDoor2DoorTransitionCompiler : SimpleCompiler
    {
        public DoorPlacement PlacementOfDoors;
        public List<Renderer> OutsideRenderer, Always1, Always2;
        public List<bool> HideOccupiedWallsAndCorners;

        public enum DoorPlacement
        {
            StraightSmall,
            CornerSmall,
            StraightBig
        }
        
        public override void Recompile(TrackingSpaceRoot ram)
        {
            var memoryPointer = HardwareRequirements.MemoryPointer.Value;
            var allocatedMemory = HardwareRequirements.AllocatedMemory;
            var tilesAvailable = ram.TileAvailable;
            var dimensions = ram.GetTileAmountTotal();
            var reversed = HardwareRequirements.Reversed;

            //check where there is first 2m wall, place there
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
            //set position and rotation
            var position2 = ram.GetSpaceFromPosition(memoryPointer);
            var position3 = new Vector3(position2.x, 0f, position2.y);
            transform.position = position3;
            switch (PlacementOfDoors)
            {
                case DoorPlacement.StraightBig:
                    if (upperWall && !reversed)
                    {
                        transform.position = position3 + (allocatedMemory.y - 1) * Vector3.forward;
                        transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    }
                    else if (rightWall && reversed)
                    {
                        transform.position = position3 + Vector3.forward + (allocatedMemory.x - 1) * Vector3.right;
                        transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    }
                    else if (lowerWall && !reversed)
                    {
                        transform.position = position3 + Vector3.right;
                        transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    }
                    else
                    {
                        if (!(leftWall && reversed))
                            Debug.LogError(gameObject.name + "/" + transform.parent.gameObject.name + " no fitting wall");
                        transform.position = position3;
                        transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                    }
                    break;
                case DoorPlacement.CornerSmall:
                    if (upperWall && rightWall)
                    {
                        transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    }
                    else if (rightWall && lowerWall)
                    {
                        transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    }
                    else if (lowerWall && leftWall)
                    {
                        transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    }
                    else
                    {
                        if (!leftWall || !upperWall)
                            Debug.LogError(gameObject.name + "/" + transform.parent.gameObject.name + " no wall");
                        transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                    }
                    break;
                case DoorPlacement.StraightSmall:
                    if (leftWall || rightWall)
                    {
                        transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    }
                    else
                    {
                        if (!upperWall && !lowerWall)
                            Debug.LogError(gameObject.name + "/" + transform.parent.gameObject.name + " no wall");
                        transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                    }
                    break;
            }
            
            for (var i = 0; i < Transitions.Count; i++)
            {
                Colorize(Transitions[i], i == 0 ? Always1 : Always2);

                
            }

            SetVars();
        }

        //public bool sadfsadf;
        //new void Update()
        //{
        //    base.Update();
        //    Debug.Log("1 " + Transitions[0].Name);
        //    if (sadfsadf)
        //    {
        //        Debug.Log("2 " + Transitions[0].Name);
        //        sadfsadf = false;
        //        var transitionsReady = Transitions.Where(r => r.GetConditionsFulfilled().All(v => v.Value));
        //        var firstReady = transitionsReady.FirstOrDefault();
        //        if (firstReady == null)
        //            return;

        //        Colorize(firstReady, OutsideRenderer);
        //    }
        //}

        public override void TransitionFired(PetrinetTransition transition)
        {
            base.TransitionFired(transition);
            HideStuff();
        }

        private void Colorize(PetrinetTransition transition, List<Renderer> rends)
        {
            var firstPlaceIn = transition.In.FirstOrDefault(i => i.Type == PetrinetCondition.ConditionType.Place);
            if (firstPlaceIn != null)
            {
                var houseLinker = firstPlaceIn.GetComponent<HouseLinker>();
                if (houseLinker != null)
                {
                    houseLinker.ColorizeThis(rends);
                }
            }
        }
        
        public override void UpdateTransitions(Dictionary<PetrinetTransition, bool> visualize, Dictionary<PetrinetTransition, bool> readyToFire)
        {
            base.UpdateTransitions(visualize, readyToFire);
            
            var transitionsReady = Transitions.Where(r => r.GetConditionsFulfilled().All(v => v.Value));
            var firstReady = transitionsReady.FirstOrDefault();
            if (firstReady == null)
                return;

            Colorize(firstReady, OutsideRenderer);

            //var firstPlaceOut = firstReady.Out.FirstOrDefault(i => i.Type == PetrinetCondition.ConditionType.Place);
            //if (firstPlaceOut != null)
            //{
            //    var houseLinker = firstPlaceOut.GetComponent<HouseLinker>();
            //    if (houseLinker != null)
            //    {
            //        houseLinker.ColorizeThis(MarkerRenderer);
            //    }
            //}
        }

        public void HideStuff()
        {
            var memoryPointer = HardwareRequirements.MemoryPointer.Value;
            var allocatedMemory = HardwareRequirements.AllocatedMemory;
            var reversed = HardwareRequirements.Reversed;
            for (var i = 0; i < Mathf.Min(HideOccupiedWallsAndCorners.Count, Transitions.Count); i++)
            {
                if (!HideOccupiedWallsAndCorners[i])
                    continue;
                var firstPlaceIn = Transitions[i].In.FirstOrDefault(t => t.Type == PetrinetCondition.ConditionType.Place);
                if (firstPlaceIn == null) continue;
                var linker = firstPlaceIn.GetComponent<AbstractLinker>();
                var buildObject = linker.GetBuildObject();
                if (buildObject == null) continue;
                if (firstPlaceIn.Tokens == 0)
                {
                   // Debug.Log(firstPlaceIn.name + " should show");
                    foreach (var t in buildObject.GetComponentsInChildren<Transform>())
                        foreach (var rend in t.gameObject.GetComponentsInChildren<Renderer>())
                            rend.enabled = true;
                }
                else
                {
                   // Debug.Log(firstPlaceIn.name + " should hide");
                    for (var x = memoryPointer.x; x < memoryPointer.x + allocatedMemory.x; x++)
                    for (var z = memoryPointer.y; z < memoryPointer.y + allocatedMemory.y; z++)
                    {
                        var objects = new List<GameObject>();
                        var wall = buildObject.transform.Find((reversed ? "upperWall_" : "rightWall_") + x + "_" + z);
                        if (wall != null)
                            objects.Add(wall.gameObject);
                        if (x != memoryPointer.x || z != memoryPointer.y)
                        {
                            var corner = buildObject.transform.Find("corner_" + x + "_" + z);
                            if (corner != null)
                                objects.Add(corner.gameObject);
                        }
                        foreach (var obj in objects)
                            foreach (var rend in obj.GetComponentsInChildren<Renderer>())
                                rend.enabled = false;
                    }
                }
            }
        }
    }
}
