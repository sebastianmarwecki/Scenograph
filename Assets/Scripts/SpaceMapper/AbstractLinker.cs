using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Petrinet;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.SpaceMapper
{
    public abstract class AbstractLinker : Builder
    {
        public PetrinetCondition Condition;
        public List<GameObject> PossibleSwitchObjects;
        public Vector3 UnfocusPosition, FocusPosition, CopyUnfocusOffset;
        public GameObject SeeThroughCorner, SeeThroughWall, SeeThroughUnavailable;

        [HideInInspector]
        public UnityEvent Linked;


        public static bool LeaveOutWallsLeftLowerWallsAndCornersForView;

        private int _copies;

        #region private functions
        private void OnEnable()
        {
            PetrinetCondition.TokenUpdated.AddListener(TokenUpdate);
        }

        private void OnDisable()
        {
            PetrinetCondition.TokenUpdated.RemoveListener(TokenUpdate);
        }

        private void Awake()
        {
            _copies = 0;
        }
        
        #endregion

        #region protected functions

        protected void TokenUpdate(PetrinetCondition condition)
        {
            if (Condition != condition)
                return;

            //set visuals on/off
            var shouldViz = condition.Tokens > 0;
            var buildObject = GetBuildObject();
            var offset = shouldViz ? FocusPosition : UnfocusPosition;
            if (buildObject != null)
                buildObject.transform.position = offset;
            //GetBuildObject().SetActive(shouldViz);
            var compilers = GetComponentsInChildren<AbstractCompiler>(true);
            foreach (var compiler in compilers)
            {
                //compiler.gameObject.SetActive(shouldViz);
                compiler.ApplyOffset(offset);
            }
        }

        internal Vector3 GetCopyUnfocusIncrement()
        {
            return CopyUnfocusOffset * _copies++;
        }

        public GameObject GetSwitchObject()
        {
            return PossibleSwitchObjects[Random.Range(0, PossibleSwitchObjects.Count)];
        }

        protected void PlacePrimitives(TrackingSpaceRoot ram, 
            List<AbstractCompiler> compilers, 
            bool placeStraightWallCorners, 
            List<GameObject> wall, 
            List<GameObject> wallOccupied, 
            List<GameObject> corner, 
            List<GameObject> floorOccupied, 
            List<GameObject> floorUnoccupied, 
            List<GameObject> unavailable, 
            int unavailableTilesBorder)
        {
            var total = ram.GetTileAmountTotal();
            var tileAvailable = (bool[,]) ram.TileAvailable.Clone();
            var tileSize = ram.TileSize;

            var compilerLinkInfos = compilers.Select(c => c.GetLinkInformation()).ToList();
            var compilerPlacements = compilers.Select(c => c.HardwareRequirements.GetAssignmentMatrix()).ToList();
            var compilerPlacedLinkedInfo = new AbstractCompiler.LinkInformation[total.x, total.y];

            for (var x = 0; x < total.x; x++)
            {
                for (var z = 0; z < total.y; z++)
                {
                    var first = compilerPlacements.FindIndex(cp => cp[x, z]);
                    var info = first < 0
                        ? AbstractCompiler.LinkInformation.Default
                        : compilerLinkInfos[first];
                    compilerPlacedLinkedInfo[x, z] = info;
                }
            }

            //iterate through corners
            for (var x = 0 - unavailableTilesBorder; x <= total.x + unavailableTilesBorder; x++)
            {
                for (var z = 0 - unavailableTilesBorder; z <= total.y + unavailableTilesBorder; z++)
                {
                    if ((x < 0 || x >= total.x || z < 0 || z >= total.y) && x < total.x + unavailableTilesBorder && z < total.y + unavailableTilesBorder)
                    {
                        if (unavailable.Any())
                        {
                            CompileLibraryObject(unavailable[Random.Range(0, unavailable.Count)], ram.GetSpaceFromPosition(x, z), 0f, "floor_" + x + "_" + z);
                        }
                    }
                    if (x < 0 || x > total.x || z < 0 || z > total.y)
                        continue;

                    //get assigned tiles in quadrants
                    var upLeft = x - 1 >= 0 && z < total.y && tileAvailable[x - 1, z];
                    var lowerLeft = x - 1 >= 0 && z - 1 >= 0 && tileAvailable[x - 1, z - 1];
                    var upRight = x < total.x && z < total.y && tileAvailable[x, z];
                    var lowerRight = x < total.x && z - 1 >= 0 && tileAvailable[x, z - 1];
                    
                    var upLeftOccupied = upLeft && compilerPlacements.Any(cp => cp[x - 1, z]) && compilerPlacedLinkedInfo[x - 1, z] != AbstractCompiler.LinkInformation.ShowAsUnoccupied;
                    //var lowerLeftOccupied = lowerLeft && compilerPlacements.Any(cp => cp[x - 1, z-1]) && compilerPlacedLinkedInfo[x - 1, z - 1] != AbstractCompiler.LinkInformation.ShowAsUnoccupied;
                    var upRightOccupied = upRight && compilerPlacements.Any(cp => cp[x, z]) && compilerPlacedLinkedInfo[x, z] != AbstractCompiler.LinkInformation.ShowAsUnoccupied;
                    var lowerRightOccupied = lowerRight && compilerPlacements.Any(cp => cp[x, z-1]) && compilerPlacedLinkedInfo[x, z - 1] != AbstractCompiler.LinkInformation.ShowAsUnoccupied;

                    var upLeftIgnore = upLeft && compilerPlacedLinkedInfo[x - 1, z] == AbstractCompiler.LinkInformation.ShowNoWalls;
                    var lowerLeftIgnore = lowerLeft  && compilerPlacedLinkedInfo[x - 1, z-1] == AbstractCompiler.LinkInformation.ShowNoWalls;
                    var upRightIgnore = upRight && compilerPlacedLinkedInfo[x , z] == AbstractCompiler.LinkInformation.ShowNoWalls;
                    var lowerRightIgnore = lowerRight && compilerPlacedLinkedInfo[x , z-1] == AbstractCompiler.LinkInformation.ShowNoWalls;

                    //create floor objects
                    if (upRight && !upRightIgnore)
                    {
                        if (upRightOccupied && floorOccupied.Any())
                        {
                            CompileLibraryObject(floorOccupied[Random.Range(0, floorOccupied.Count)], ram.GetSpaceFromPosition(x, z), 0f, "floor_" + x + "_" + z);
                        }
                        else if(!upRightOccupied && floorUnoccupied.Any())
                        {
                            CompileLibraryObject(floorUnoccupied[Random.Range(0, floorUnoccupied.Count)], ram.GetSpaceFromPosition(x, z), 0f, "floor_" + x + "_" + z);
                        }
                    }
                    else if (!upRight && x < total.x && z < total.y)
                    {
                        if (unavailable.Any())
                        {
                            var unav = unavailable[Random.Range(0, unavailable.Count)];
                            if (LeaveOutWallsLeftLowerWallsAndCornersForView && SeeThroughUnavailable != null)
                            {
                                var av = false;
                                for (var xx = x+1; xx < total.x; xx++)
                                {
                                    for (var zz = z+1; zz < total.y; zz++)
                                    {
                                        av = tileAvailable[xx, zz];
                                        if (av)
                                            break;
                                    }
                                    if (av)
                                        break;
                                }
                                if(av)
                                    unav = SeeThroughUnavailable;
                            }
                            CompileLibraryObject(unav, ram.GetSpaceFromPosition(x, z), 0f, "floor_" + x + "_" + z);
                        }
                    }

                    //check for walls
                    var leftWall =  upLeft != lowerLeft      && !upLeftIgnore     && !lowerLeftIgnore;
                    var lowerWall = lowerRight != lowerLeft  && !lowerRightIgnore && !lowerLeftIgnore;
                    var rightWall = lowerRight != upRight    && !lowerRightIgnore && !upRightIgnore;
                    var upperWall = upLeft != upRight        && !upLeftIgnore     && !upRightIgnore;

                    //do not build if iso view is on and upper left or lower right are not available
                    var rightSeeThrough = false;
                    var upSeeThrough = false;
                    var cornerSeeThrough = false;
                    if (LeaveOutWallsLeftLowerWallsAndCornersForView)
                    {
                        rightSeeThrough = rightWall && !lowerRight;
                        upSeeThrough = upperWall && !upLeft;
                        //lowerWall = lowerWall && lowerLeft;
                        //leftWall = leftWall && lowerLeft;
                        //rightSeeThrough = !rightWall && oldRight;
                        //upSeeThrough = !upperWall && oldUpper;
                        cornerSeeThrough = rightSeeThrough || upSeeThrough;
                    }

                    //do not build, if no adjacent edge is a wall
                    if (!upperWall && !lowerWall && !rightWall && !leftWall)
                        continue;

                    //create ref point for wall and corner placement
                    Vector2 refPoint;
                    if (x == total.x && z == total.y)
                    {
                        refPoint = ram.GetSpaceFromPosition(x - 1, z - 1) + 0.5f * tileSize;
                    }
                    else if (x == total.x)
                    {
                        refPoint = ram.GetSpaceFromPosition(x - 1, z);
                        refPoint.x += 0.5f * tileSize.x;
                        refPoint.y -= 0.5f * tileSize.y;
                    }
                    else if (z == total.y)
                    {
                        refPoint = ram.GetSpaceFromPosition(x, z - 1);
                        refPoint.x -= 0.5f * tileSize.x;
                        refPoint.y += 0.5f * tileSize.y;
                    }
                    else
                    {
                        refPoint = ram.GetSpaceFromPosition(x, z) - 0.5f * tileSize;
                    }

                    //create corner if not straight wall
                    var count = 0;
                    if (leftWall) count++;
                    if (rightWall) count++;
                    if (lowerWall) count++;
                    if (upperWall) count++;
                    var obj = corner[Random.Range(0, corner.Count)];
                    var buildSeeThrough = cornerSeeThrough && SeeThroughCorner != null;
                    if (buildSeeThrough)
                        obj = SeeThroughCorner;
                    if (corner.Any() && !(!placeStraightWallCorners && count == 2 && leftWall == rightWall))
                        CompileLibraryObject(obj, refPoint, 0f, "corner_" + x + "_" + z);

                    //create walls
                    if (wall != null)
                    {
                        if (upperWall)
                        {
                            var occupied = upLeftOccupied || upRightOccupied;
                            var list = occupied ? wallOccupied : wall;
                            var objWall = list[Random.Range(0, list.Count)];
                            if (upSeeThrough && SeeThroughWall != null)
                                objWall = SeeThroughWall;
                            CompileLibraryObject(objWall,
                                new Vector2(0f, 0.5f * tileSize.y) + refPoint,
                                upLeft ? 90f : -90f, "upperWall_" + x + "_" + z);
                        }
                        if (rightWall)
                        {
                            var occupied = lowerRightOccupied || upRightOccupied;
                            occupied = occupied || lowerRight && !lowerRightOccupied;
                            var list = occupied ? wallOccupied : wall;
                            var objWall = list[Random.Range(0, list.Count)];
                            if (rightSeeThrough && SeeThroughWall != null)
                                objWall = SeeThroughWall;
                            CompileLibraryObject(objWall,
                                    new Vector2(0.5f * tileSize.x, 0f) + refPoint,
                                upRight ? 180f : 0f, "rightWall_" + x + "_" + z);
                        }
                    }
                }
            }
        }

        internal void ActivateLinker(TrackingSpaceRoot ram, List<AbstractCompiler> compilers)
        {
            Link(ram, compilers);
            if(Linked != null)
                Linked.Invoke();
        }
    
        #endregion

        internal abstract void Link(TrackingSpaceRoot ram, List<AbstractCompiler> compilers);
    }
}