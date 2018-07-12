using System;
using System.Collections.Generic;
using Packer.Packer;
using Spatial;

namespace Packer
{
    public class PlacedObject
    {
        public PlaceableObject PlaceableObject;

        public Vector Dimensions;
        public bool Placed;
        public Vector Position;
        public bool Flipped;
        public IRoom Rooms;
        public int WallScore;
        public int DistScore;
        public int PlacementScore
        {
            get { return 100 * WallScore + DistScore; }
        }
        public SideRequirement XSideRequirement
        {
            get { return Flipped ? PlaceableObject.YSideRequirement : PlaceableObject.XSideRequirement; }
        }
        public SideRequirement YSideRequirement
        {
            get { return Flipped ? PlaceableObject.XSideRequirement : PlaceableObject.YSideRequirement; }
        }
        public bool ShouldCalculateDistScore;

        public PlacedObject(PlaceableObject placeableObject, Vector position, bool flipped, bool shouldCalculateDistScore=false)
        {
            PlaceableObject = placeableObject;
            Position = position;
            Flipped = flipped;
            ShouldCalculateDistScore = shouldCalculateDistScore;

            Dimensions = !Flipped ? PlaceableObject.Dimensions : PlaceableObject.Dimensions.Reversed();
        }
        
        public void AddToRooms()
        {
            Rooms.Add(this);
        }

        public void RemoveFromRooms()
        {
            Rooms.Remove(this);
        }

        public IEnumerable<Vector> PlacedIndices()
        {
            var posW = Position.X;
            var posH = Position.Y;
            
            for (var w = 0; w < Dimensions.X; w++)
            for (var h = 0; h < Dimensions.Y; h++)
            {
                yield return new Vector(w + posW, h + posH);
            }
        }

        public bool Fits(Vector size)
        {
            return Dimensions.X <= size.X && Dimensions.Y <= size.Y;
        }

        internal void CalculateWallScore()
        {
            SideState up;
            SideState down;
            SideState left;
            SideState right;

            // returning Partial is not always correct as it might cancel when seeing Blocked
            _checkForSideStates(Dimensions.X, Dimensions.Y, Position,
                out up, out down, out left, out right);

            var cornerScore = 
                PlaceableObject.IsSemiWall && _anyCornerBlocked(Dimensions.X, Dimensions.Y, Position)
                    ? 2 : 0;

            var xScore = _getSideScore(YSideRequirement, up, down);
            var yScore = _getSideScore(XSideRequirement, left, right);
            //var cornerBlocked = 
            
            WallScore = xScore + yScore + cornerScore;
        }

        private int _getSideScore(SideRequirement sideReq, SideState side1, SideState side2)
        {
            var sideScore = 0;
            
            var both = (side1 | side2);

            if (PlaceableObject.PlaceFarAway && (both & SideState.Blocked) > 0)
            {
                sideScore += 5;
            }
            
            switch (sideReq)
            {
                case SideRequirement.None:
                    sideScore = 0;
                    break;
                case SideRequirement.OneFree:
                    sideScore += (both & SideState.Free) > 0 ? 0 : 1;
                    break;
                case SideRequirement.OneWall:
                    sideScore += (both & SideState.Wall) > 0 ? 0 : 1;
                    break;
                case SideRequirement.OneWallOneFree:
                    sideScore += _checkTwoRequirements(side1, side2, both, SideState.Free, SideState.Wall);
                    break;
                case SideRequirement.TwoFree:
                    sideScore += _checkTwoRequirements(side1, side2, both, SideState.Free, SideState.Free);
                    break;
                case SideRequirement.TwoWalls:
                    sideScore += _checkTwoRequirements(side1, side2, both, SideState.Wall, SideState.Wall);
                    break;
            }

            return sideScore;
        }

        private static int _checkTwoRequirements(SideState side1, SideState side2, SideState both, SideState required1, SideState required2)
        {
            int sideScore;
            var bothAreFulfilled = (side1 & required1) > 0 && (side2 & required2) > 0 || (side1 & required2) > 0 && (side2 & required1) > 0;
            var atLeastOne = (both & required1) > 0 || (both & required2) > 0;
            // higher penalty for blocked?
            sideScore = bothAreFulfilled ? 0 : atLeastOne ? 1 : 2;
            return sideScore;
        }

        internal void CalculateDistanceScore()
        {
            DistScore = 0;

            if (ShouldCalculateDistScore)
            {
                var otherObjs = Rooms.GetPlacedObjects();

                // get dist, add to score
                // if smaller than x relative to the dimensions of the space
                // what are the dimensions of the space???

                var maxRoomDist = Math.Sqrt(Rooms.Width * Rooms.Width + Rooms.Height * Rooms.Height);
                
                foreach (var otherObj in otherObjs)
                {
                    if (Equals(otherObj)) continue;

                    var penalty = PlaceableObject.PlaceFarAway && otherObj.PlaceableObject.PlaceFarAway;
                    var distToOther = Position.Distance(otherObj.Position);

                    DistScore += (int)((1 - distToOther / maxRoomDist) * 10 * (penalty ? 5 : 1));
                }
            }
        }

        [Flags]
        enum SideState
        {
            None = 0,
            Blocked = 1,
            Partial = 2,
            Wall = 4,
            Free = 8
        }

        bool _anyCornerBlocked(
            int dimX,
            int dimY,
            Vector position)
        {
            var topLeft = position.AddXY(-1, -1);
            var topRight = position.AddXY(dimX + 1, -1);
            var bottomLeft = position.AddXY(-1, dimY + 1);
            var bottomRight = position.AddXY(dimY + 1, dimY + 1);
            
            return Rooms.TileIsBlockedForRequirements(topLeft) ||
                Rooms.TileIsBlockedForRequirements(topRight) ||
                Rooms.TileIsBlockedForRequirements(bottomLeft) ||
                Rooms.TileIsBlockedForRequirements(bottomRight);
        }

        void _checkForSideStates(
            int dimX,
            int dimY,
            Vector position,
            out SideState upState, out SideState downState, out SideState leftState, out SideState rightState)
        {
            upState = SideState.None; 
            downState = SideState.None;
            leftState = SideState.None;
            rightState = SideState.None;

            var noContinueStates = SideState.Blocked | SideState.Partial;

            for (int wOff = 0; 
                ((upState & noContinueStates) == 0 || (downState & noContinueStates) == 0) &&
                    wOff < dimX; 
                wOff++)
            {
                var up = position.AddXY(wOff, -1);
                var down = position.AddXY(wOff, dimY);

                upState = _changeStatusForSide(upState, up, wOff == 0 ? true : false);

                downState = _changeStatusForSide(downState, down, wOff == 0 ? true : false);
            }

            for (int hOff = 0;
                ((leftState & noContinueStates) == 0 || (rightState & noContinueStates) == 0) &&
                    hOff < dimY; 
                hOff++)
            {
                var left = position.AddXY(-1, hOff);
                var right = position.AddXY(dimX, hOff);

                leftState = _changeStatusForSide(leftState, left, hOff == 0 ? true : false);

                rightState = _changeStatusForSide(rightState, right, hOff == 0 ? true : false);
            }
        }

        private SideState _changeStatusForSide(SideState previousState, Vector pos, bool initial = false)
        {
            SideState state = previousState;

            if (Rooms.TileIsBlockedForRequirements(pos))
            {
                state = SideState.Blocked;
            }
            else if (Rooms.TileIsWall(pos))
            {
                if (initial) state = SideState.Wall;
                else if(previousState == SideState.Free) state = SideState.Partial;
            }
            else
            {
                if (initial) state = SideState.Free;
                else if(previousState == SideState.Wall) state = SideState.Partial;
            }

            return state;
        }
    }
}