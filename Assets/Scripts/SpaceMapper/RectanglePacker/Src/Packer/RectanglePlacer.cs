using System;
using System.Collections.Generic;
using System.Linq;
using Packer.Packer;
using Spatial;

namespace Packer
{
    public class RectanglePlacer
    {
        readonly IRoom _room;
        PlacementScoreRequirement _scoreReq;
        PlacedObject _bestPlacementSoFar;
        int _numCutsOfBestSoFar;
        bool _includeDistance;
        float _acceptProbability;
        Random _rand;

        public RectanglePlacer(IRoom room)
        {
            _room = room;
            _rand = new Random(DateTime.Now.Millisecond);
        }
        
        public PlacedObject Place(PlaceableObject obj, 
            PlacementScoreRequirement scoreReq, bool includeDistance,
            float acceptProbability=1f)
        {
            _includeDistance = includeDistance;
            _scoreReq = scoreReq;
            _bestPlacementSoFar = null;
            _acceptProbability = acceptProbability;

            var maxObjectSize = new List<Vector>[_room.Width, _room.Height];

            // dynamic algorithm for determining fit
            // first row
            for (int w = 0, maxWidth = 0; w < _room.Width; w++)
            {
                if (_room.TileAvailable(w, 0))
                {
                    maxWidth++;
                    _addSingle(maxObjectSize, w, 0, maxWidth, 1);
                } else
                    maxWidth = 0;
                
                var placed = _tryPlaceObject(maxObjectSize, w, 0, obj);
                if (placed != null)
                {
                    placed.AddToRooms();
                    return placed;
                }
            }

            // first column
            for (int h = 0, maxHeight = 0; h < _room.Height; h++)
            {
                if (_room.TileAvailable(0, h))
                {
                    maxHeight++;
                    _addSingle(maxObjectSize, 0, h, 1, maxHeight);
                } else
                    maxHeight = 0;
                
                var placed = _tryPlaceObject(maxObjectSize, 0, h, obj);
                if (placed != null)
                {
                    placed.AddToRooms();
                    return placed;
                }
            }

            // rest of the 2d array
            for (int h = 1; h < _room.Height; h++)
            {
                for (int w = 1, lastSmallestWidth = _room.TileAvailable(0, h) ? 1 : 0; w < _room.Width; w++)
                {
                    if (_room.TileAvailable(w, h))
                    {
                        lastSmallestWidth++;

                        _addBasedOnNeighborsForAllDirections(maxObjectSize, w, h, lastSmallestWidth);

                        var placed = _tryPlaceObject(maxObjectSize, w, h, obj);
                        if (placed != null)
                        {
                            placed.AddToRooms();
                            return placed;
                        }
                    }
                    else
                    {
                        lastSmallestWidth = 0;
                    }
                }
            }

            if (_bestPlacementSoFar != null)
            {
                _bestPlacementSoFar.AddToRooms();
                return _bestPlacementSoFar;
            }

            return null;
        }

        private static void _addSingle(List<Vector>[,] vec, int w, int h, int wDim, int hDim)
        {
            vec[w, h] = new List<Vector> { new Vector(wDim, hDim) };
        }

        // hack: public so it can be unit tested
        public static void _addBasedOnNeighborsForAllDirections(
            List<Vector>[,] vec, int w, int h, int lastSmallestWidth)
        {
            var leftRects = vec[w - 1, h];
            var upRects = vec[w, h - 1];

            if (leftRects == null && upRects == null)
            {
                _addSingle(vec, w, h, 1, 1);
            }
            else
            {
                var thisRects = new List<Vector>();

                // if they are null it means there was a boundary
                var largestWidth = leftRects == null ? 1 : leftRects.Max(rect => rect.X) + 1;
                var largestHeight = upRects == null ? 1 : upRects.Max(rect => rect.Y) + 1 ;

                if (leftRects != null)
                {
                    thisRects.AddRange(leftRects.Select(rectDim => rectDim.AddX(1)));
                }

                if (upRects != null)
                {
                    thisRects.AddRange(upRects.Select(rectDim => rectDim.AddY(1)));
                }

                // bound by the largest values in the left and upper rectangles
                // to find out about new lefter or upper bounds that the other sides don't know about
                thisRects = thisRects
                    .Select(rect => rect.Y > largestHeight ? rect.SetY(largestHeight) : rect)
                    .Select(rect => rect.X > largestWidth ? rect.SetX(largestWidth) : rect)
                    .ToList();

                // delete those which are contained in another, only keep the largest dimensions
                // keep the smallest indexed element if there are equal elements
                var deleteIndices = new List<int>();
                for (int i = thisRects.Count - 1; i >= 0; i--)
                {
                    for (int j = 0; j < thisRects.Count; j++)
                    {
                        if (i == j) continue;
                        
                        if (thisRects[i].X <= thisRects[j].X && thisRects[i].Y <= thisRects[j].Y &&
                            !(thisRects[i].Equals(thisRects[j]) && i < j))
                        {
                            deleteIndices.Add(i);
                            break;
                        }
                    }
                }

                foreach (var i in deleteIndices)
                {
                    thisRects.RemoveAt(i);
                }

                vec[w, h] = thisRects;
            }
        }

        private PlacedObject _tryPlaceObject(List<Vector>[,] vec, int w, int h, PlaceableObject obj)
        {
            if (vec[w, h] == null) return null;

            var numRectanglesCut = vec[w, h].Count;

            foreach (var maxDimensionInDirection in vec[w, h])
            {
                PlacedObject placedObject = null;

                if (_placeIfFullfilsConstrainsRememberIfBetterScore(
                                obj, false, 
                                new Vector(w - obj.Dimensions.X + 1, h - obj.Dimensions.Y + 1),
                                maxDimensionInDirection,
                                numRectanglesCut, out placedObject))
                    return placedObject;
                
                if (_placeIfFullfilsConstrainsRememberIfBetterScore(
                                obj, true, 
                                new Vector(w - obj.Dimensions.Y + 1, h - obj.Dimensions.X + 1),
                                maxDimensionInDirection,
                                numRectanglesCut, out placedObject))
                    return placedObject;
            }

            return null;
        }

        private bool _placeIfFullfilsConstrainsRememberIfBetterScore(
            PlaceableObject obj, bool flipped, 
            Vector position, Vector maxDimensionsInDirection,
            int numRectanglesCut,
            out PlacedObject o)
        {
            var placedObject = new PlacedObject(obj, position, flipped, _includeDistance);
            placedObject.Rooms = _room;

            if (!placedObject.Fits(maxDimensionsInDirection))
            {
                o = null;
                return false;
            }

            if (_scoreReq == PlacementScoreRequirement.DontCare)
            {
                o = placedObject;
                return true;
            }

            placedObject.CalculateWallScore();
            placedObject.CalculateDistanceScore();

            switch(_scoreReq)
            {
                case PlacementScoreRequirement.LeastDamageThenBestWall:
                    if (_bestPlacementSoFar == null ||
                        placedObject.WallScore + 100 * numRectanglesCut < _bestPlacementSoFar.WallScore + 100 * _numCutsOfBestSoFar)
                    {
                        _bestPlacementSoFar = placedObject;
                        _numCutsOfBestSoFar = numRectanglesCut;
                    }
                    break;
                case PlacementScoreRequirement.OnlyBestWall:
                    if (placedObject.WallScore == 0)
                    {
                        if (_rand.NextDouble() <= _acceptProbability)
                        {
                            o = placedObject;
                            return true;
                        } else
                        {
                            _bestPlacementSoFar = placedObject;
                        }
                    }
                    break;
                case PlacementScoreRequirement.PreferBest:
                    if (placedObject.PlacementScore == 0)
                    {
                        if (_rand.NextDouble() <= _acceptProbability)
                        {
                            o = placedObject;
                            return true;
                        }
                        else
                        {
                            _bestPlacementSoFar = placedObject;
                        }
                    } else if (_bestPlacementSoFar == null || placedObject.PlacementScore < _bestPlacementSoFar.PlacementScore)
                    {
                        _bestPlacementSoFar = placedObject;
                    }
                    break;
            }

            o = null;
            return false;
        }
    }
}