using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Packer.Packer;
using Spatial;

namespace Packer
{
    public enum SideRequirement
    {
        None,
        OneWall,
        TwoWalls,
        OneFree,
        TwoFree,
        OneWallOneFree
    }

    public class PlaceableObject
    {
        public Vector Dimensions;
        public List<int> RoomsIn;
        public SideRequirement XSideRequirement;
        public SideRequirement YSideRequirement;
        public int Id = ObjectId++;
        public static int ObjectId = 0;
        public SpaceMapper.AbstractPacker.PackingRequest Request;
        public bool IsSemiWall;
        public bool PlaceFarAway;

        public int TilesNeeded
        {
            get
            {
                return Dimensions.X * Dimensions.Y;
            }
        }
        
        public PlaceableObject(int x, int y, List<int> roomsIn, 
            SideRequirement xSideRequirement = SideRequirement.None,
            SideRequirement ySideRequirement = SideRequirement.None,
            bool isSemiWall = false,
            bool placeFarAway = false)
        {
            Dimensions = new Vector(x, y);
            RoomsIn = roomsIn;
            XSideRequirement = xSideRequirement;
            YSideRequirement = ySideRequirement;
            IsSemiWall = isSemiWall;
            PlaceFarAway = placeFarAway;
        }
    }
}