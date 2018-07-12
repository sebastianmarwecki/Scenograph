using System;
using System.Collections.Generic;
using Assets.Scripts.SpaceMapper;
using UnityEngine;

namespace SpaceMapper
{
    public abstract class AbstractPacker : MonoBehaviour
    {
        public struct PackingResult
        {
            public Vector2Int Pointer;
            public Vector2Int Allocation;
            public bool Reverse;
        }

        public struct PackingRequest
        {
            public Vector2 Size;
            public List<int> Places;
            public WallPrefs WallX;
            public WallPrefs WallY;
            //public int ValueWallX;
            //public int ValueWallY;
            public bool IsSemiWall;
            public bool PlaceFarAway;
        }

        public enum WallPrefs
        {
            None,
            OneWall,
            TwoWalls,
            OneFree,
            TwoFree,
            OneWallOneFree
        }
        
        public abstract bool GetPacking(
            List<PackingRequest> request,
            TrackingSpaceRoot ram,
            out Dictionary<PackingRequest, PackingResult> result);
        
        #region protected static helper functions

        protected static int GetTileAmount(HardwareRequirements hardwareSpecification, Vector2 tileSize)
        {
            var tiles = GetTileAmountAsVector2Int(hardwareSpecification, tileSize);
            Debug.Log(hardwareSpecification.gameObject + "/" + hardwareSpecification.transform.parent.gameObject.name + "="+ tiles.x*tiles.y);
            return tiles.x * tiles.y;
        }

        protected static Vector2Int GetTileAmountAsVector2Int(HardwareRequirements hardwareSpecification, Vector2 tileSize)
        {
            return GetTileAmountAsVector2Int(hardwareSpecification.Requirement, tileSize);
        }

        protected static Vector2Int GetTileAmountAsVector2Int(Vector2 requirement, Vector2 tileSize)
        {
            var numberTilesForX = Math.Max(1, Mathf.CeilToInt(requirement.x / tileSize.x));
            var numberTilesForZ = Math.Max(1, Mathf.CeilToInt(requirement.y / tileSize.y));
            return new Vector2Int(numberTilesForX, numberTilesForZ);
        }

        protected static bool[,] MatrixCombine(List<bool[,]> matrices, bool andElseOr)
        {
            var copyFirst = (bool[,])matrices[0].Clone();
            matrices.RemoveAt(0);
            foreach (var t in matrices)
                copyFirst = MatrixCombine(copyFirst, t, andElseOr);
            return copyFirst;
        }

        protected static bool[,] MatrixCombine(bool[,] first, bool[,] second, bool andElseOr)
        {
            if (first.Length != second.Length)
                return null;
            var returnMatrix = (bool[,])first.Clone();
            for (var i = 0; i < first.GetLength(0); i++)
            {
                for (var j = 0; j < first.GetLength(1); j++)
                {
                    returnMatrix[i, j] = andElseOr ? first[i, j] && second[i, j] : first[i, j] || second[i, j];
                }
            }
            return returnMatrix;
        }

        protected static bool[,] InvertMatrix(bool[,] matrix)
        {
            var returnMatrix = (bool[,])matrix.Clone();
            for (var i = 0; i < matrix.GetLength(0); i++)
            {
                for (var j = 0; j < matrix.GetLength(1); j++)
                {
                    returnMatrix[i, j] = !matrix[i, j];
                }
            }
            return returnMatrix;
        }

        #endregion
    }
}
