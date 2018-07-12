using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.SpaceMapper;
using UnityEngine;

namespace SpaceMapper
{
    public class SimplePacker : AbstractPacker {

        //public override bool SpaceRequirement(
        //    List<HardwareRequirements> hardwareCluster,
        //    List<HardwareRequirements> hardwareExtra,
        //    int extraAmount,
        //    TrackingSpaceRoot ram)
        //{
        //    var totalTiles = ram.GetTilesAvailable();
        //    var tiles = hardwareCluster.Sum(rt => GetTileAmount(rt, ram.TileSize));
        //    var extra = hardwareExtra.Sum(rt => GetTileAmount(rt, ram.TileSize));
        //    Debug.Log("space needed " + tiles + "+" + extra + "*" + extraAmount + " ? " + totalTiles);
        //    return totalTiles >= tiles + extra * extraAmount;
        //}

        public override bool GetPacking(
            List<PackingRequest> requests,
            TrackingSpaceRoot ram,
            out Dictionary<PackingRequest, PackingResult> result)
        {
            //clone/copy hardware list
            var req = new List<PackingRequest>(requests);
            
            //  Debug.Log("availableTiles " + HardwareRequirementsEditor.Matrix2String(availableTiles));

            //create result list
            var res = new Dictionary<PackingRequest, PackingResult>();

            //for each room have assignment matrix
            var places = new List<int>();
            foreach (var r in requests)
                places.AddRange(r.Places);
            places = places.Distinct().ToList();
            var placeToAssignment = places.ToDictionary(p => p, p => (bool[,])ram.TileAvailable.Clone());

            //iterate through, sort by biggest
            while (req.Any())
            {
                //get largest tile demand
                var nextIndex = GetNext2Place(req);
                var nextHw = req[nextIndex];
                req.RemoveAt(nextIndex);

                //Debug.Log("try place ");// + nextReq.Key.gameObject.name);

                //get demand
                var tilesNeeded = GetTileAmountAsVector2Int(nextHw.Size, ram.TileSize);

                //check availability
                var availableTiles = MatrixCombine(nextHw.Places.Select(p => placeToAssignment[p]).ToList(), true);
                //    if (concurrentRequests.Any())
                //{
                //var assignedSpace = concurrentRequests.Select(cr => assignedMatrix[cr]).ToList();
                //var assignedSpace = reqsAlreadyPlacedToConsider.Select(req => req.GetAssignmentMatrix()).ToList();
                //var singleListOccupiedTiles = MatrixCombine(assignedSpace, false);
                //var inverseOccupied = InvertMatrix(singleListOccupiedTiles);
                // availableTiles = MatrixCombine(inverseOccupied, availableTiles, true);
                //}
                //Debug.Log(HardwareRequirementsEditor.Matrix2String(availableTiles) + " availableTiles ");

                //assign tiles
                bool[,] assignedTiles;
                Vector2Int pointer, allocation;
                bool reverse;
                if (AssignTiles(availableTiles, ram.TileAvailable, tilesNeeded, nextHw.WallX, nextHw.WallY, out assignedTiles, out pointer, out allocation, out reverse))
                {
                    //Debug.Log(HardwareRequirementsEditor.Matrix2String(assignedTiles) + " assignedTiles ");
                    //TODO PAPER: talk about fragmentation! (optimization for better usage)
                    //place / allocate
                    res.Add(nextHw, new PackingResult {Pointer = pointer, Allocation = allocation, Reverse = reverse});
                    //add to assignment matrix
                    var inverseAssigned = InvertMatrix(assignedTiles);
                    foreach (var place in nextHw.Places)
                        placeToAssignment[place] = MatrixCombine(inverseAssigned, placeToAssignment[place], true);

                    //assignedMatrix.Add(nextHw, ram.GetAssignmentMatrix(pointer, allocation));
                    //nextReq.Key.Assign(ram, pointer, assignment);
                }
                else
                {
                    Debug.Log("Packing not possible: Not enough memory/space");
                    result = new Dictionary<PackingRequest, PackingResult>();
                    return false;
                }
            }

            result = res;
            return true;
        }

        private static int GetNext2Place(IList<PackingRequest> list)
        {
            var lastBigIndex = 0;
            var lastSize = list[0].Size.magnitude;
            for (var i = 1; i < list.Count; i++)
            {
                if (list[i].Size.magnitude > lastSize)
                    lastBigIndex = i;
            }
            return lastBigIndex;
        }

        private static bool FitsIn(bool[,] availableTiles, Vector2Int dimensions, Vector2Int tryFit, Vector2Int count)
        {
            var fitsin = tryFit.x + count.x <= dimensions.x && tryFit.y + count.y <= dimensions.y;
            if (!fitsin)
                return false;
            for (var xNext = tryFit.x; xNext < tryFit.x + count.x; xNext++)
            for (var zNext = tryFit.y; zNext < tryFit.y + count.y; zNext++)
            {
                var available = availableTiles[xNext, zNext];
                if (!available)
                    return false;
            }
            return true;
        }

        private bool SatisfiesWallConstraints(bool[,] availableTiles, Vector2Int dimensions, Vector2Int pointer, Vector2Int count, 
            WallPrefs wallX, WallPrefs wallY)
        {
            //check all walls
            var rightWall = true;
            var leftWall = true;
            for (var x = pointer.x; x < pointer.x + count.x; x++)
            for (var z = pointer.y; z < pointer.y + count.y; z++)
            {
                if (x == pointer.x)
                    leftWall = leftWall && (x == 0 || !availableTiles[x - 1, z]);
                if (x == pointer.x + count.x - 1)
                    rightWall = rightWall && (x + 1 == dimensions.x || !availableTiles[x + 1, z]);
            }
            var numberX = 0;
            if (leftWall)
                numberX++;
            if (rightWall)
                numberX++;
            switch (wallY)
            {
                case WallPrefs.OneWall:
                    if (numberX < 1)
                        return false;
                    break;
                case WallPrefs.TwoWalls:
                    if (numberX < 2)
                        return false;
                    break;
                case WallPrefs.OneFree:
                    if (numberX > 1)
                        return false;
                    break;
                case WallPrefs.TwoFree:
                    if (numberX > 0)
                        return false;
                    break;
                case WallPrefs.OneWallOneFree:
                    if (numberX != 1)
                        return false;
                    break;
            }
            var upperWall = true;
            var lowerWall = true;
            for (var x = pointer.x; x < pointer.x + count.x; x++)
            for (var z = pointer.y; z < pointer.y + count.y; z++)
            {
                if (z == pointer.y)
                    lowerWall = lowerWall && (z == 0 || !availableTiles[x, z - 1]);
                if (z == pointer.y + count.y - 1)
                    upperWall = upperWall && (z + 1 == dimensions.y || !availableTiles[x, z + 1]);
            }
            var numberY = 0;
            if (lowerWall)
                numberY++;
            if (upperWall)
                numberY++;
            switch (wallX)
            {
                case WallPrefs.OneWall:
                    if (numberY < 1)
                        return false;
                    break;
                case WallPrefs.TwoWalls:
                    if (numberY < 2)
                        return false;
                    break;
                case WallPrefs.OneFree:
                    if (numberY > 1)
                        return false;
                    break;
                case WallPrefs.TwoFree:
                    if (numberY > 0)
                        return false;
                    break;
                case WallPrefs.OneWallOneFree:
                    if (numberY != 1)
                        return false;
                    break;
            }
            
            return true;
        }

        private bool AssignTiles(bool[,] availableTiles, bool[,] totalTiles, Vector2Int count, 
            WallPrefs wallX, WallPrefs wallY,  
            out bool[,] assignedTiles, out Vector2Int pointer, out Vector2Int assignment, out bool reverse)
        {
            //place trigger in grid
            var dimensions = new Vector2Int(availableTiles.GetLength(0), availableTiles.GetLength(1));
            //pick random indices
            var xStart = Random.Range(0, dimensions.x);
            var zStart = Random.Range(0, dimensions.y);
            var found = false;
            reverse = false;
            assignedTiles = new bool[dimensions.x, dimensions.y];
            pointer = Vector2Int.zero;
            assignment = new Vector2Int(count.x, count.y);
            //iterate through all tiles, brute forcing all rotations and placements
            for (var x = 0; x < dimensions.x; x++)
            {
                for (var z = 0; z < dimensions.y; z++)
                {
                    //get random iteration offset
                    pointer = new Vector2Int((x + xStart) % dimensions.x, (z + zStart) % dimensions.y);

                    //check if all following tiles are unoccupied
                    var fitsin = FitsIn(availableTiles, dimensions, pointer, count);
                    var reversedCount = new Vector2Int(count.y, count.x);
                    var fitsInReversed = FitsIn(availableTiles, dimensions, pointer, reversedCount);
                    if (!fitsin && !fitsInReversed)
                        continue;

                    //check if wall preferences and satisfied (simple check)
                    var satisfiesWalls = fitsin && SatisfiesWallConstraints(totalTiles, dimensions, pointer, count, wallX,
                        wallY);
                    var reverseSatisfiesWalls = fitsInReversed && SatisfiesWallConstraints(totalTiles, dimensions, pointer, reversedCount, wallY,
                        wallX);

                    //if available, reserve tiles and return
                    var available = satisfiesWalls || reverseSatisfiesWalls;
                    if (available)
                    {
                        found = true;
                        //randomly rotate, if both rotations fit
                        if (satisfiesWalls && reverseSatisfiesWalls)
                            reverse = Random.Range(0, 2) == 0; 
                        else if(reverseSatisfiesWalls)
                            reverse = true;
                        if(reverse)
                            assignment = new Vector2Int(count.y, count.x);
                        for (var xNext = pointer.x; xNext < pointer.x + assignment.x; xNext++)
                        for (var zNext = pointer.y; zNext < pointer.y + assignment.y; zNext++)
                        {
                            assignedTiles[xNext, zNext] = true;
                        }
                        break;
                    }
                }
                if (found)
                    break;
            }
            return found;
        }
    }
}
