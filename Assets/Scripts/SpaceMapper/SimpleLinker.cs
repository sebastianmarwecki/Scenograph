using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.SpaceMapper
{
    public class SimpleLinker : AbstractLinker
    {
        public List<GameObject> FloorLibraryUnoccupied;
        public List<GameObject> FloorLibraryOccupied;
        public List<GameObject> WallLibrary;
        public List<GameObject> WallOccupiedLibrary;
        public List<GameObject> CornerLibrary;
        public List<GameObject> UnavailableLibrary;
        public int UnavailableTilesBorder;
        public bool PlaceStraightWallObjects;

        internal override void Link(TrackingSpaceRoot ram, List<AbstractCompiler> compilers)
        {
            //delete old game objects first
            DeleteKids();

            //build environment from library objects
            PlacePrimitives(ram, compilers, PlaceStraightWallObjects, WallLibrary, WallOccupiedLibrary, CornerLibrary, FloorLibraryOccupied, FloorLibraryUnoccupied, UnavailableLibrary, UnavailableTilesBorder);

            TokenUpdate(Condition);
        }
    }
}
