using System;
using System.Collections.Generic;
using System.Diagnostics;
using Spatial;

namespace Packer.Packer
{
    public class Room : IRoom
    {
        public bool[,] RoomLayout { get; private set; }
        public bool[,] SemiWallLayout { get; private set; }
        public bool[,] RawRoomLayout { get; private set; }
        public int Width
        {
            get
            {
                return RoomLayout.GetLength(0);
            }
        }
        public int Height
        {
            get
            {
                return RoomLayout.GetLength(1);
            }
        }
        public int TilesTotal
        {
            get
            {
                return Width * Height;
            }
        }
        public int TilesUsed
        {
            get
            {
                return TilesTotal - TilesAvailable;
            }
        }
        public int TilesAvailable { get; private set; }
        public int Id { get; internal set; }
        private List<PlacedObject> _objectsInRoom;

        private readonly bool _freeBoolValue;
        private bool _takenBoolValue
        {
            get
            {
                return !_freeBoolValue;
            }
        }

        public Room(bool[,] roomLayout, bool freeBoolValue)
        {
            // todo what if one complete side of the room is missing, -> smaller array!
            RawRoomLayout = roomLayout;
            RoomLayout = Helper.Copy(roomLayout);
            SemiWallLayout = new bool[roomLayout.GetLength(0), roomLayout.GetLength(1)];

            _setFreeTiles();
            _objectsInRoom = new List<PlacedObject>();
            _freeBoolValue = freeBoolValue;
        }

        private Room(bool[,] roomLayout, int tilesAvailable, bool freeBoolValue)
        {
            RawRoomLayout = roomLayout;
            RoomLayout = Helper.Copy(roomLayout);
            SemiWallLayout = new bool[roomLayout.GetLength(0), roomLayout.GetLength(1)];

            TilesAvailable = tilesAvailable;
            _objectsInRoom = new List<PlacedObject>();
            _freeBoolValue = freeBoolValue;
        }

        public bool InRange(int w, int h)
        {
            return 0 <= w && w < Width && 0 <= h && h < Height;
        }

        public bool TileAvailable(int w, int h)
        {
            return InRange(w, h) && RoomLayout[w, h] == _freeBoolValue;
        }

        public bool TileIsWall(int w, int h)
        {
            return !InRange(w, h) || RawRoomLayout[w, h] == _takenBoolValue;
        }

        public bool TileIsWall(Vector v)
        {
            return TileIsWall(v.X, v.Y);
        }

        public bool TileIsBlockedForRequirements(Vector v)
        {
            return InRange(v.X, v.Y) && SemiWallLayout[v.X, v.Y];
        }

        public void Add(PlacedObject obj)
        {
            _objectsInRoom.Add(obj);

            foreach (var placedIndex in obj.PlacedIndices())
            {
                if (!TileAvailable(placedIndex.X, placedIndex.Y))
                {
                    UnityEngine.Debug.Log("Adding to unavailable tile" + placedIndex.ToString());
                }

                RoomLayout[placedIndex.X, placedIndex.Y] = _takenBoolValue;

                if (obj.PlaceableObject.IsSemiWall)
                    SemiWallLayout[placedIndex.X, placedIndex.Y] = true;
            }

            TilesAvailable -= obj.PlaceableObject.TilesNeeded;
        }

        public void Remove(PlacedObject obj)
        {
            _objectsInRoom.Remove(obj);

            foreach (var placedIndex in obj.PlacedIndices())
            {
                if (TileAvailable(placedIndex.X, placedIndex.Y))
                {
                    UnityEngine.Debug.Log("Removing available tile" + placedIndex.ToString());
                }

                RoomLayout[placedIndex.X, placedIndex.Y] = _freeBoolValue;

                if (obj.PlaceableObject.IsSemiWall)
                    SemiWallLayout[placedIndex.X, placedIndex.Y] = false;
            }

            TilesAvailable += obj.PlaceableObject.TilesNeeded;
        }

        private void _setFreeTiles()
        {
            int numFree = 0;
            for (int w = 0; w < Width; w++)
            {
                for (int h = 0; h < Height; h++)
                {
                    if (TileAvailable(w, h))
                        numFree++;
                }
            }
            
            TilesAvailable = numFree;
        }

        public IEnumerable<Vector> AvailableTileIndices()
        {
            for (int w = 0; w < Width; w++) 
            for (int h = 0; h < Height; h++) 
                if (TileAvailable(w, h))
                    yield return new Vector(w, h);
        }
        
        public Room CloneBase()
        {
            return new Room(RawRoomLayout, TilesAvailable, _freeBoolValue);
        }

        public IEnumerable<PlacedObject> GetPlacedObjects()
        {
            return _objectsInRoom;
        }
    }
}