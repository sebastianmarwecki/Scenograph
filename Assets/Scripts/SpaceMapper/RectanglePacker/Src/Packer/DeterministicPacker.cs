using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Packer.Packer;

namespace Packer
{
    public class DeterministicPacker
    {
        private readonly bool _freeBoolValue;
        private readonly System.Random _rand;

        public DeterministicPacker(bool freeBoolValue = false)
        {
            _freeBoolValue = freeBoolValue;
            _rand = new Random(DateTime.Now.Millisecond);
        }

        public int Pack(out List<PlacedObject> placedObjs,
            List<PlaceableObject> objectsToPlace, bool[,] roomLayout, 
            PlacementScoreRequirement scoreReq = PlacementScoreRequirement.DontCare, bool includeDistance = false, float acceptProbability = 1f)
        {
            var templateRoom = new Room(roomLayout, _freeBoolValue);
            var roomIds = objectsToPlace.SelectMany(obj => obj.RoomsIn).Distinct();
            var rooms = new Dictionary<int, Room>();
            foreach (var r in roomIds)
            {
                var room = templateRoom.CloneBase();
                room.Id = r;
                rooms[r] = room;
            }
            
            // sort objectsByScore
            var objectsToPlaceSorted = objectsToPlace.OrderBy(
                obj => -obj.TilesNeeded * obj.RoomsIn.Count
            );

            // pack objects
            placedObjs = new List<PlacedObject>();
            foreach (var obj in objectsToPlaceSorted)
            {
                IRoom objRooms = new RoomCollection(obj.RoomsIn.Select(roomId => rooms[roomId]));
                
                RectanglePlacer placer = new RectanglePlacer(objRooms);

                var placedObj = placer.Place(obj, scoreReq, includeDistance, acceptProbability);
                
                if (placedObj == null)
                {
                    placedObjs = null;
                    return int.MaxValue;
                }

                placedObjs.Add(placedObj);
            }

            Debug.Assert(placedObjs.Count == objectsToPlace.Count);

            var score = placedObjs.Sum(placedObj => placedObj.WallScore);

            //UnityEngine.Debug.Log("[RectPlacer] Finished First Fit");
            //UnityEngine.Debug.Log("[RectPlacer] WallScore: " + score);
            //Helper.PrintToConsole(placedObjs);

            return score;
        }

        class Selection
        {
            public int Index;
            public PlacedObject PlacedObj;

            public Selection(int index, PlacedObject obj)
            {
                Index = index;
                PlacedObj = obj;
            }
        }

        List<PlacedObject> _annealFit;
        List<PlacedObject> _beforeStep;
        List<PlacedObject> _afterStep;
        
        public static int GetScore(List<PlacedObject> placedObjs, bool onlyWall)
        {
            var score = 0;

            foreach (var placedObj in placedObjs)
            {
                placedObj.ShouldCalculateDistScore = !onlyWall;
                placedObj.CalculateWallScore();
                placedObj.CalculateDistanceScore();
                score += placedObj.PlacementScore;
            }

            return score;
        }

        private int GetCurrentAnnealScore(bool onlyWall = false)
        {
            return GetScore(_annealFit, onlyWall);
        }

        public int ImprovePackingAccordingToPlacementScores(out List<PlacedObject> annealFit, List<PlacedObject> firstFit)
        {
            var alpha = 0.98f;
            var temperature = 100.0f;
            var epsilon = 0.001f;
            
            _annealFit = new List<PlacedObject>(firstFit);

            var scoreBeforeAnneal = GetCurrentAnnealScore();

            var numNotWorkingSinceLastSuccess = 0;
            var numNotWorking = 0;
            var numLoops = 0;
            while (_annealFit.Count > 2 && temperature > epsilon && numNotWorkingSinceLastSuccess < 30)
            {
                var beforeScore = GetCurrentAnnealScore();

                if (!AnnealStep())
                {
                    numNotWorkingSinceLastSuccess++;
                    numNotWorking++;
                    continue;
                }

                numNotWorkingSinceLastSuccess = 0;
                var afterScore = GetCurrentAnnealScore();

                var delta = afterScore - beforeScore;
                
                if (delta < 0 || _rand.NextDouble() < Math.Exp(-delta / temperature))
                {
                    AcceptStep();
                }
                else
                {
                    DiscardStep();
                }

                temperature *= alpha;
                numLoops++;
            }

            var wallAfterAnneal = GetCurrentAnnealScore(true);
            var scoreAfterAnneal = GetCurrentAnnealScore();
            var totalDelta = scoreAfterAnneal - scoreBeforeAnneal;

            //UnityEngine.Debug.Log("[RectPlacer] Finished Best Fit");
            //UnityEngine.Debug.Log("[RectPlacer] Number of Loops: " + numLoops);
            //UnityEngine.Debug.Log("[RectPlacer] Number of failed steps: " + numNotWorking);

            //UnityEngine.Debug.Log("[RectPlacer] Score: " + scoreAfterAnneal);
            //UnityEngine.Debug.Log("[RectPlacer] Wall Score: " + wallAfterAnneal);
            //UnityEngine.Debug.Log("[RectPlacer] Delta (smaller better): " + totalDelta);

            annealFit = _annealFit;

            return scoreAfterAnneal;
        }

        private void AcceptStep()
        {
            foreach (var placedObj in _beforeStep)
                _annealFit.Remove(placedObj);
            _annealFit.AddRange(_afterStep);

            _beforeStep = null;
            _afterStep = null;
        }

        private void DiscardStep() {
            foreach (var placedObj in _afterStep)
            {
                placedObj.RemoveFromRooms();
            }

            foreach (var placedObj in _beforeStep)
            {
                placedObj.AddToRooms();
            }

            _beforeStep = null;
            _afterStep = null;
        }

        int _percentReplaced = 40;
        private bool AnnealStep()
        {
            _annealFit.OrderByDescending(placedObj => placedObj.PlacementScore);
            
            var numReplace = (_annealFit.Count * _percentReplaced) / 100;
            numReplace = Math.Max(2, numReplace);
            var stepLength = _annealFit.Count / numReplace;

            //UnityEngine.Debug.Log("Replacing " + numReplace + " of " + annealFit.Count);

            var placedObjsToBeReplacedWithIndex = new List<Selection>();
            for (int i = 0; i < numReplace; i++)
            {
                // - i so we only select those which were not previously selected
                var objIndex = _rand.Next(0, i * stepLength - i);

                // skip one position if it was previously chosen (because we looked for total -i)
                // needs to be done in the correct order
                placedObjsToBeReplacedWithIndex = 
                    placedObjsToBeReplacedWithIndex.OrderBy(selected => selected.Index).ToList();
                for (int j = 0; j < i; j++)
                    if (objIndex >= placedObjsToBeReplacedWithIndex[j].Index)
                        objIndex++;

                //UnityEngine.Debug.Log("Selecting object " + objIndex);

                var placedObj = _annealFit[objIndex];
                placedObjsToBeReplacedWithIndex.Add(new Selection(objIndex, placedObj));

                placedObj.RemoveFromRooms();
            }

            var replacedPlacedObjs = new List<PlacedObject>();
            foreach (var idObj in placedObjsToBeReplacedWithIndex)
            {
                var newPlaced = new RectanglePlacer(idObj.PlacedObj.Rooms)
                    .Place(idObj.PlacedObj.PlaceableObject, PlacementScoreRequirement.PreferBest, true);

                if (newPlaced == null)
                {
                    // first remove so that there is the original space
                    for (int i = 0; i < replacedPlacedObjs.Count; i++)
                    {
                        replacedPlacedObjs[i].RemoveFromRooms();
                    }

                    foreach (var beforePlaced in placedObjsToBeReplacedWithIndex)
                    {
                        beforePlaced.PlacedObj.AddToRooms();
                    }

                    break;
                }

                replacedPlacedObjs.Add(newPlaced);
            }

            if (replacedPlacedObjs.Count != placedObjsToBeReplacedWithIndex.Count)
                return false;

            _beforeStep = placedObjsToBeReplacedWithIndex.Select(sel => sel.PlacedObj).ToList();
            _afterStep = replacedPlacedObjs;

            return true;
        }
    }
}
