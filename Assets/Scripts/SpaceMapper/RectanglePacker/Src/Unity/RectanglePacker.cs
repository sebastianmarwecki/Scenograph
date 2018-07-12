using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.SpaceMapper;
using SpaceMapper;
using Packer;
using UnityEngine;
using SMAbstractPacker = SpaceMapper.AbstractPacker;

namespace SpaceMapper
{
    public class RectanglePacker : SMAbstractPacker
    {
        public enum Modes
        {
            Performance,
            Quality
        }
        public int InitialRunsPacker = 10;

        [Serializable]
        public class Settings {

        }

        [Serializable]
        public class PerformanceSettings : Settings
        {

        }

        [Serializable]
        public class QualitySettings : Settings
        {

        }

        public Modes ExecutionMode = Modes.Performance;

        private readonly Dictionary<WallPrefs, SideRequirement> _prefsToReq = 
            new Dictionary<WallPrefs, SideRequirement>
            {
                {WallPrefs.None, SideRequirement.None},
                {WallPrefs.OneFree, SideRequirement.OneFree },
                {WallPrefs.OneWall, SideRequirement.OneWall },
                {WallPrefs.OneWallOneFree, SideRequirement.OneWallOneFree },
                {WallPrefs.TwoFree, SideRequirement.TwoFree },
                {WallPrefs.TwoWalls, SideRequirement.TwoWalls },
            };

        private SideRequirement _convertToSideRequirement(WallPrefs pref)
        {
            return _prefsToReq[pref];
        }

        // todo show which reqs are fulfilled and which not
        public override bool GetPacking(List<PackingRequest> request, TrackingSpaceRoot ram, out Dictionary<PackingRequest, PackingResult> result)
        {
            var placeableObjects = new List<PlaceableObject>();
            foreach (var req in request)
            {
                var obj = new PlaceableObject((int)Math.Ceiling(req.Size.x), (int)Math.Ceiling(req.Size.y),
                    req.Places,
                    _convertToSideRequirement(req.WallY), _convertToSideRequirement(req.WallX),
                    req.IsSemiWall,
                    req.PlaceFarAway);
                obj.Request = req;

                placeableObjects.Add(obj);
            }
            
            var tilesAvailable = (bool[,])ram.TileAvailable.Clone();

            DeterministicPacker packer = new DeterministicPacker(true);


            var bestWallScore = int.MaxValue;
            List<PlacedObject> bestPlacedObjs = null;
            for (int i = 0; i < InitialRunsPacker; i++)
            {
                float acceptProbability = (float)i / InitialRunsPacker;
                
                List<PlacedObject> placedObjs;

                var firstFitScore = packer.Pack(out placedObjs,
                    placeableObjects, tilesAvailable,
                    ExecutionMode == Modes.Performance ? PlacementScoreRequirement.OnlyBestWall : PlacementScoreRequirement.PreferBest,
                    false,
                    acceptProbability);

                if (placedObjs == null)
                    continue;

                var wallScore = DeterministicPacker.GetScore(placedObjs, true);

                if (wallScore <= bestWallScore)
                {
                    bestPlacedObjs = placedObjs;
                    bestWallScore = wallScore;
                }
            }
            
            if (bestPlacedObjs == null)
            {
                result = null;
                return false;
            }

            if (ExecutionMode == Modes.Quality)
            {
                List<PlacedObject> annealedPlacedObjs = null;
                var annealingScore = packer.ImprovePackingAccordingToPlacementScores(out annealedPlacedObjs, bestPlacedObjs);

                var wallScore = DeterministicPacker.GetScore(annealedPlacedObjs, true);

                if (wallScore > 0)
                {
                    result = null;
                    return false;
                }

                bestPlacedObjs = annealedPlacedObjs;
            }

            // convert PackedObject to PackingResult
            result = new Dictionary<PackingRequest, PackingResult>();
            foreach (var placedObj in bestPlacedObjs)
            {
                var placeableObj = placedObj.PlaceableObject;
                var xDim = placedObj.Flipped
                    ? placeableObj.Dimensions.Y
                    : placeableObj.Dimensions.X;
                var yDim = placedObj.Flipped
                    ? placeableObj.Dimensions.X
                    : placeableObj.Dimensions.Y;
                
                var res = new PackingResult()
                {
                    Allocation = new Vector2Int(xDim, yDim),
                    Pointer = new Vector2Int(placedObj.Position.X, placedObj.Position.Y),
                    Reverse = placedObj.Flipped
                };

                result[placeableObj.Request] = res;
            }

            return true;
        }
    }
}

