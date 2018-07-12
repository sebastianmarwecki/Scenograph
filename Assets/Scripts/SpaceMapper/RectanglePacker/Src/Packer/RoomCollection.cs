using System.Collections.Generic;
using System.Linq;
using Spatial;

namespace Packer.Packer
{
    public class RoomCollection : IRoom
    {
        private readonly List<IRoom> _rooms;

        public RoomCollection(IEnumerable<Room> rooms)
        {
            _rooms = rooms.Cast<IRoom>().ToList();
            Width = _rooms.First().Width;
            Height = _rooms.First().Height;
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public bool TileAvailable(int w, int h)
        {
            return _rooms.All(room => room.TileAvailable(w, h));
        }

        public bool TileIsWall(int w, int h)
        {
            return _rooms.First().TileIsWall(w, h);
        }

        public bool TileIsWall(Vector v)
        {
            return TileIsWall(v.X, v.Y);
        }

        public bool TileIsBlockedForRequirements(Vector v)
        {
            return _rooms.Any(room => room.TileIsBlockedForRequirements(v));
        }

        public void Add(PlacedObject placedObj)
        {
            _rooms.ForEach(room => room.Add(placedObj));
        }

        public void Remove(PlacedObject placedObj)
        {
            _rooms.ForEach(room => room.Remove(placedObj));
        }

        public IEnumerable<PlacedObject> GetPlacedObjects()
        {
            return _rooms.SelectMany(room => room.GetPlacedObjects()).Distinct();
        }
    }
}