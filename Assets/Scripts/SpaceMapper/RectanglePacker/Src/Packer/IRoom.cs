using Spatial;
using System.Collections.Generic;

namespace Packer
{
    public interface IRoom
    {
        int Width { get; }
        int Height { get; }

        bool TileAvailable(int w, int h);
        bool TileIsWall(int w, int h);
        bool TileIsWall(Vector v);
        bool TileIsBlockedForRequirements(Vector v);

        void Add(PlacedObject placedObj);
        void Remove(PlacedObject placedObj);

        IEnumerable<PlacedObject> GetPlacedObjects();
    }
}