using System;
using System.Collections.Generic;
using Packer;
using Spatial;
using System.Linq;

namespace Packer
{
    public class Helper
    {
        public static bool[,] Copy(bool[,] a)
        {
            bool[,] r = new bool[a.GetLength(0), a.GetLength(1)];
            Buffer.BlockCopy(a, 0, r, 0, a.Length * sizeof(bool));
            return r;
        }

        public static bool[,] GenerateRoomLayout(int width, int height, List<Vector> unavailable = null)
        {
            bool[,] room = new bool[width, height];

            //for (int h = 0; h < height; h++)
            //{
            //    for (int w = 0; w < width; w++)
            //    {
            //        room[w, h] = true;
            //    }
            //}

            if (unavailable != null)
                foreach (var unavailablePosition in unavailable)
                {
                    room[unavailablePosition.X, unavailablePosition.Y] = true;
                }

            return room;
        }

        public static void PrintToConsole(List<PlacedObject> placedObjs)
        {
            var anyRoom = placedObjs.First().Rooms;
            var numRooms = placedObjs
                .SelectMany(placedObj => placedObj.PlaceableObject.RoomsIn)
                .Distinct()
                .Count();
            
            var finalLayout = new int[numRooms, anyRoom.Width, anyRoom.Height];

            foreach (var placedObj in placedObjs)
            {
                foreach (var r in placedObj.PlaceableObject.RoomsIn)
                foreach (var placeIndex in placedObj.PlacedIndices())
                {
                    finalLayout[r, placeIndex.X, placeIndex.Y]++;
                }
            }

            PrintToConsole(finalLayout);
        }

        public static void PrintToConsole<T>(T[,,] placedNums)
        {
            Console.WriteLine(GetStringRepresentation(placedNums));
        }

        // todo jagged so that this can be nested
        public static string GetStringRepresentation<T>(T[,,] placedNums)
        {
            string ret = "";
            for (int r = 0; r < placedNums.GetLength(0); r++)
            {
                ret += "Room " + r + "\n";
                for (int h = 0; h < placedNums.GetLength(2); h++)
                {
                    for (int w = 0; w < placedNums.GetLength(1); w++)
                    {
                        ret += placedNums[r, w, h];
                    }
                    ret += "\n";
                }
            }

            return ret;
        }

        public static string GetStringRepresentation<T>(T[,] placedNums, Func<T, string> typeToName = null)
        {
            if (typeToName == null)
            {
                typeToName = t => t.ToString();
            }

            string ret = "";
            for (int h = 0; h < placedNums.GetLength(1); h++)
            {
                for (int w = 0; w < placedNums.GetLength(0); w++)
                {
                    ret += typeToName(placedNums[w, h]);
                }
                ret += "\n";
            }

            return ret;
        }

        enum Direction { Up, Right, Down, Left }
        class DirectionInstructions
        {
            public Vector ForwardOffset;
            public Direction ForwardUnavailableDirection; // do not change pos, because it might not be available
            public Vector ForwardPolyOffset;
            public Vector SidewardsOffset;
            public Direction SidewardsAvailableDirection;
            public Vector SidewardsPolyOffset;

            public DirectionInstructions(
                int forwardX, int forwardY, Direction forwardUnvailableDirection, int forwardPolyX, int forwardPolyY,
                int sidewardsX, int sidewardsY, Direction sidewardsAvailableDirection, int sidewardsPolyX, int sidewardsPolyY)
            {
                SidewardsOffset = new Vector(sidewardsX, sidewardsY);
                SidewardsAvailableDirection = sidewardsAvailableDirection;
                SidewardsPolyOffset = new Vector(sidewardsPolyX, sidewardsPolyY);

                ForwardOffset = new Vector(forwardX, forwardY);
                ForwardUnavailableDirection = forwardUnvailableDirection;
                ForwardPolyOffset = new Vector(forwardPolyX, forwardPolyY);
            }
        }

        static Dictionary<Direction, DirectionInstructions> _walkInstructions = new Dictionary<Direction, DirectionInstructions>
        {
            { Direction.Right, new DirectionInstructions(1, 0, Direction.Down, 1, 0, 0, -1, Direction.Up, 0, 0) },
            { Direction.Down, new DirectionInstructions(0, 1, Direction.Left, 1, 1, 1, 0, Direction.Right, 1, 0) },
            { Direction.Left, new DirectionInstructions(-1, 0, Direction.Up, 0, 1, 0, 1, Direction.Down, 1, 1) },
            { Direction.Up, new DirectionInstructions(0, -1, Direction.Right, 0, 0, -1, 0, Direction.Left, 0, 1) }
        };

        public static Polygon GetPolygonFromBoolArray(bool[,] roomLayout)
        {
            var roomWidth = roomLayout.GetLength(0);
            var roomHeight = roomLayout.GetLength(1);

            Vector firstAvailable = _getFirstAvailable(roomLayout, roomWidth, roomHeight);

            Func<Vector, bool> isInBounds = vec => 0 <= vec.X && vec.X < roomWidth && 0 <= vec.Y && vec.Y < roomHeight;
            Func<Vector, bool> isAvailable = vec => isInBounds(vec) && !roomLayout[vec.X, vec.Y];

            List<Vector> polygonPositions = new List<Vector>();
            polygonPositions.Add(firstAvailable.Clone());

            Vector pos = firstAvailable;
            Direction currentDirection = Direction.Right;
            var fullCircle = false;
            do
            {
                if (currentDirection == Direction.Up) fullCircle = true;

                DirectionInstructions instr = _walkInstructions[currentDirection];

                Vector nextPos = null;
                nextPos = pos + instr.SidewardsOffset;
                if (isAvailable(nextPos))
                {
                    var polyPos = pos + instr.SidewardsPolyOffset;
                    polygonPositions.Add(polyPos);
                    currentDirection = instr.SidewardsAvailableDirection;
                    pos = nextPos; // otherwise the algorithm would immediately walk sidewards afterwards (in the direction it was coming from)
                    continue;
                }

                nextPos = pos + instr.ForwardOffset;
                if (!isAvailable(nextPos))
                {
                    var polyPos = pos + instr.ForwardPolyOffset;
                    polygonPositions.Add(polyPos);
                    currentDirection = instr.ForwardUnavailableDirection;
                    continue;
                }

                pos = nextPos;
            } while (!fullCircle || !firstAvailable.Equals(pos));

            return new Polygon(polygonPositions);
        }

        private static Vector _getFirstAvailable(bool[,] roomLayout, int roomWidth, int roomHeight)
        {
            for (int h = 0; h < roomHeight; h++)
            {
                for (int w = 0; w < roomWidth; w++)
                {
                    if (!roomLayout[w, h])
                    {
                        return new Vector(w, h);
                    }
                }
            }

            return null;
        }
    }
}
