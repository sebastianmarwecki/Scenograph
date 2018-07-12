using System;
using Packer.Packer;
using Spatial;

namespace Packer
{
    class ArrayPlacer
    {
        private IRoom _room;
        private Room _templateRoom;

        public ArrayPlacer(IRoom room, Room templateRoom)
        {
            _room = room;
            _templateRoom = templateRoom; // todo remove this
        }

        public PlacedObject Place(PlaceableObject obj, PlacementScoreRequirement scoreReq)
        {
            if (scoreReq != PlacementScoreRequirement.DontCare) throw new NotImplementedException();

            var tilesNotAvailable = Helper.Copy(_templateRoom.RoomLayout);
            foreach (var tileIndex in _templateRoom.AvailableTileIndices())
            {
                if (!_room.TileAvailable(tileIndex.X, tileIndex.Y))
                {
                    tilesNotAvailable[tileIndex.X, tileIndex.Y] = true;
                }
            }

            PlacedObject placedObj = null;
            for (var w = 0; placedObj == null && w < _room.Width; w++)
            {
                for (var h = 0; placedObj == null && h < _room.Height; h++)
                {
                    if (Fits(tilesNotAvailable, w, h, obj.Dimensions.X, obj.Dimensions.Y))
                    {
                        placedObj = new PlacedObject(obj, new Vector(w, h), false);
                    }

                    if (placedObj == null &&
                        obj.Dimensions.X != obj.Dimensions.Y &&
                        Fits(tilesNotAvailable, w, h, obj.Dimensions.Y, obj.Dimensions.X))
                    {
                        placedObj = new PlacedObject(obj, new Vector(w, h), true);
                    }
                }
            }

            return placedObj;
        }

        private static bool Fits(bool[,] tilesNotAvailable, int w, int h, int wDim, int hDim)
        {
            bool fits = true;

            var totalWidth = tilesNotAvailable.GetLength(0);
            var totalHeight = tilesNotAvailable.GetLength(1);

            for (var wOff = 0; fits && wOff < wDim; wOff++)
            {
                for (var hOff = 0; fits && hOff < hDim; hOff++)
                {
                    if (w + wOff >= totalWidth ||
                        h + hOff >= totalHeight ||
                        tilesNotAvailable[w + wOff, h + hOff]
                    )
                        fits = false;
                }
            }

            return fits;
        }
    }
}