using Packer;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Spatial;

namespace PackerTests
{
    [TestFixture]
    public class PackerTest
    {
        List<PlaceableObject> _objectsToPlace;
        List<PlacedObject> _packedObjects;
        DeterministicPacker _packer;
        List<int> _roomIds;
        bool[,] _roomLayout;
        PlacementScoreRequirement _placementScoreReq = PlacementScoreRequirement.PreferBest;

        [SetUp]
        public void TestInitialize()
        {
            _objectsToPlace = new List<PlaceableObject>();
            _packer = new DeterministicPacker();
            _roomIds = new List<int>();
        }
        
        PlaceableObject GenerateObject(
            int width, int height, params int[] roomsIn)
        {
            var placeableObject = new PlaceableObject(width, height, roomsIn.ToList());

            return placeableObject;
        }

        PlaceableObject GenerateObject(
            int width, int height,
            SideRequirement xRequirement,
            SideRequirement yRequirement,
            params int[] roomsIn)
        {
            var placeableObject = new PlaceableObject(width, height, roomsIn.ToList(),
                xRequirement, yRequirement);

            return placeableObject;
        }

        public void GivenObject(int width, int height,
            SideRequirement xRequirement,
            SideRequirement yRequirement,
            params int[] roomsIn)
        {
            foreach (var roomId in roomsIn) if (!_roomIds.Contains(roomId)) _roomIds.Add(roomId);

            _objectsToPlace.Add(GenerateObject(width, height, xRequirement, yRequirement, roomsIn));
        }

        public void GivenObject(int width, int height, params int[] roomsIn)
        {
            foreach (var roomId in roomsIn) if (!_roomIds.Contains(roomId)) _roomIds.Add(roomId);

            _objectsToPlace.Add(GenerateObject(width, height, roomsIn));
        }

        public void GivenRoom(int width, int height, List<Vector> unavailable = null)
        {
            _roomLayout = Helper.GenerateRoomLayout(width, height, unavailable);
        }

        public void GivenRoom(int width, int height, params int[] unavaiableXY)
        {
            Assert.IsTrue(unavaiableXY.Length % 2 == 0);

            var unavailable = new List<Vector>();
            for (int off = 0; off < unavaiableXY.Length; off += 2)
            {
                unavailable.Add(new Vector(unavaiableXY[off], unavaiableXY[off + 1]));
            }

            _roomLayout = Helper.GenerateRoomLayout(width, height, unavailable);
        }

        public void GivenPlacementScoreRequirement(PlacementScoreRequirement scoreReq)
        {
            _placementScoreReq = scoreReq;
        }

        [Test]
        public void TestSimplePacking()
        {
            DeterministicPacker packer = new DeterministicPacker();

            bool[,] roomLayout = Helper.GenerateRoomLayout(5, 5);
            List<PlaceableObject> objectsToPlace = new List<PlaceableObject>();

            // different packing densities for testing
            objectsToPlace.Add(GenerateObject(1, 1, 0));
            objectsToPlace.Add(GenerateObject(1, 1, 0));
            objectsToPlace.Add(GenerateObject(1, 1, 0));

            // 3 / 25 density, should always succeed
            List<PlacedObject> placedObj;
            Assert.AreEqual(0, packer.Pack(out placedObj, objectsToPlace, roomLayout));
            Assert.IsNotNull(placedObj);
        }

        [Test]
        public void TestHalfDensityPacking()
        {
            DeterministicPacker packer = new DeterministicPacker();

            bool[,] roomLayout = Helper.GenerateRoomLayout(4, 4);
            List<PlaceableObject> objectsToPlace = new List<PlaceableObject>();

            // different packing densities for testing
            objectsToPlace.Add(GenerateObject(2, 1, 0));
            objectsToPlace.Add(GenerateObject(2, 2, 0));
            objectsToPlace.Add(GenerateObject(1, 2, 0));
            objectsToPlace.Add(GenerateObject(1, 1, 0));

            // 9 / 16 density

            List<PlacedObject> placedObj;
            Assert.AreEqual(0, packer.Pack(out placedObj, objectsToPlace, roomLayout));
            Assert.IsNotNull(placedObj);
        }

        [Test]
        public void TestJustFitsPacking()
        {
            DeterministicPacker packer = new DeterministicPacker();

            bool[,] roomLayout = Helper.GenerateRoomLayout(4, 4);
            List<PlaceableObject> objectsToPlace = new List<PlaceableObject>();

            // different packing densities for testing
            objectsToPlace.Add(GenerateObject(2, 2, 0));
            objectsToPlace.Add(GenerateObject(2, 2, 0));
            objectsToPlace.Add(GenerateObject(2, 2, 0));
            objectsToPlace.Add(GenerateObject(2, 2, 0));

            // 16 / 16 density

            List<PlacedObject> placedObj;
            Assert.AreEqual(0, packer.Pack(out placedObj, objectsToPlace, roomLayout));
            Assert.IsNotNull(placedObj);
        }
        
        [Test]
        public void TestJustFitsWithTwoRequirementsAndTakenTiles()
        {
            GivenRoom(5, 5, 3, 0, 3, 1, 1, 2, 1, 3, 1, 4);

            GivenObject(2, 3, SideRequirement.OneWall, SideRequirement.None, 0);
            GivenObject(3, 2, SideRequirement.OneWall, SideRequirement.None, 0);
            GivenObject(3, 1, 0);
            GivenObject(1, 5, 0);

            // todo
            // only works with the side requirements (preferbest or onlybest)!
            // perhaps we need a least damage determination when packing: fewest rectangles split in half
            GivenPlacementScoreRequirement(PlacementScoreRequirement.LeastDamageThenBestWall);

            ThenFirstFitSuccessful(false);
        }

        [Test]
        public void TestSiderequirementOnYFulfilled()
        {
            GivenRoom(5, 4);

            GivenObject(3, 1, SideRequirement.TwoFree, SideRequirement.OneWall, 0);

            GivenPlacementScoreRequirement(PlacementScoreRequirement.OnlyBestWall);

            ThenFirstFitSuccessful();

            var expected1 = new Vector(1, 0);
            var expected2 = new Vector(1, 3);

            var packedObj = _packedObjects.First();
            var actual = packedObj.Position;

            Assert.IsFalse(packedObj.Flipped);

            var isPositionedCorrectly = expected1.Equals(actual) ||
                                        expected2.Equals(actual);

            Assert.IsTrue(isPositionedCorrectly);
        }

        [Test]
        public void TestFirstFitThenBestFitPacking()
        {
            GivenRoom(5, 5);

            GivenObject(3, 1, SideRequirement.None, SideRequirement.OneWall, 0);
            GivenObject(1, 3, SideRequirement.OneWall, SideRequirement.None, 0);
            GivenObject(1, 3, SideRequirement.OneWall, SideRequirement.None, 0);
            GivenObject(3, 1, SideRequirement.None, SideRequirement.OneWall, 0);

            GivenPlacementScoreRequirement(PlacementScoreRequirement.LeastDamageThenBestWall);

            ThenFirstFitSuccessful(false);

            ThenBestFitSuccessful();
        }

        [Test]
        public void TestImpossiblePacking()
        {
            DeterministicPacker packer = new DeterministicPacker();

            bool[,] roomLayout = Helper.GenerateRoomLayout(5, 5);
            List<PlaceableObject> objectsToPlace = new List<PlaceableObject>();

            // different packing densities for testing
            objectsToPlace.Add(GenerateObject(6, 6, 0));

            // 36 / 25 density, should always fail

            List<PlacedObject> placedObj;
            Assert.AreNotEqual(0, packer.Pack(out placedObj, objectsToPlace, roomLayout));
            Assert.IsNull(placedObj);
        }

        [Test]
        public void TestMultiRoomPacking()
        {
            DeterministicPacker packer = new DeterministicPacker();

            bool[,] roomLayout = Helper.GenerateRoomLayout(5, 5);
            List<PlaceableObject> objectsToPlace = new List<PlaceableObject>();
            
            objectsToPlace.Add(GenerateObject(4, 4, 0));
            objectsToPlace.Add(GenerateObject(1, 1, 0, 1));
            objectsToPlace.Add(GenerateObject(3, 3, 1));
            objectsToPlace.Add(GenerateObject(1, 1, 1, 2));
            objectsToPlace.Add(GenerateObject(2, 2, 2));
            objectsToPlace.Add(GenerateObject(2, 1, 2));

            List<PlacedObject> placedObj;
            Assert.AreEqual(0, packer.Pack(out placedObj, objectsToPlace, roomLayout));
            Assert.IsNotNull(placedObj);
        }

        [Test]
        public void TestLargeDimManyRoomsHalfDensePacking()
        {
            GivenRoom(30, 30);

            // 0 : 205+25/900
            GivenObject(7, 7, 0);
            GivenObject(8, 8, 0);
            GivenObject(9, 7, 0);
            GivenObject(7, 3, 0);
            GivenObject(2, 2, 0);
            GivenObject(2, 2, 0);

            GivenObject(5, 5, 0, 1);

            // 1 : 372+25+25/900
            GivenObject(15, 15, 1);
            GivenObject(7, 7, 1);
            GivenObject(7, 7, 1);
            GivenObject(7, 7, 1);

            GivenObject(5, 5, 1, 2);

            // 2 : 344+25/900
            GivenObject(5, 5, 2);
            GivenObject(5, 5, 2);
            GivenObject(5, 5, 2);
            GivenObject(5, 5, 2);
            GivenObject(5, 5, 2);
            GivenObject(5, 5, 2);
            GivenObject(5, 5, 2);
            GivenObject(13, 13, 2);

            ThenFirstFitSuccessful();
        }

        [Test]
        public void TestBorderRequirementsWithFlip()
        {
            GivenRoom(5, 4);

            GivenObject(4, 1, SideRequirement.TwoWalls, SideRequirement.OneWall, 0);
            GivenObject(4, 1, SideRequirement.TwoWalls, SideRequirement.OneWall, 0);

            ThenFirstFitSuccessful(false);
        }

        private void ThenFirstFitSuccessful(bool wantPerfect = true)
        {
            var score = _packer.Pack(out _packedObjects, _objectsToPlace, _roomLayout, _placementScoreReq);

            if (wantPerfect)
                Assert.AreEqual(0, score);

            Assert.IsNotNull(_packedObjects);
        }

        private void ThenBestFitSuccessful(bool wantPerfect = true)
        {
            _packer.ImprovePackingAccordingToPlacementScores(out _packedObjects, _packedObjects);

            var wallScore = _packedObjects.Sum(packedObj => packedObj.WallScore);

            if (wantPerfect)
                Assert.AreEqual(0, wallScore);

            Assert.IsNotNull(_packedObjects);
        }
    }
}
