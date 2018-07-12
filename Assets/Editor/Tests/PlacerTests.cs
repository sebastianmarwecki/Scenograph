using System.Collections.Generic;
using Packer;
using NUnit.Framework;
using Spatial;

namespace PackerTests
{
    [TestFixture]
    public class PlacerTests
    {
        [Test]
        public void TestAddBasedOnNeighbors()
        {
            var tempLayout = new List<Vector>[2, 2];

            RectanglePlacer._addBasedOnNeighborsForAllDirections(tempLayout, 1, 1, 1);

            var expected = new List<Vector> {new Vector(1, 1)};

            CollectionAssert.AreEquivalent(expected, tempLayout[1, 1]);
        }

        [Test]
        public void TestAddOnlyFromUp()
        {
            var tempLayout = new List<Vector>[2, 2];

            tempLayout[1, 0] = new List<Vector> { new Vector(6, 1), new Vector(2, 3), new Vector(3, 2) };

            RectanglePlacer._addBasedOnNeighborsForAllDirections(tempLayout, 1, 1, 1);

            var expected = new List<Vector> { new Vector(1, 4) };

            CollectionAssert.AreEquivalent(expected, tempLayout[1, 1]);
        }

        [Test]
        public void TestAddOnlyFromLeft()
        {
            var tempLayout = new List<Vector>[2, 2];

            tempLayout[1, 0] = new List<Vector> { new Vector(6, 1), new Vector(2, 3), new Vector(3, 2) };

            RectanglePlacer._addBasedOnNeighborsForAllDirections(tempLayout, 1, 1, 7);

            var expected = new List<Vector> { new Vector(1, 4) };

            CollectionAssert.AreEquivalent(expected, tempLayout[1, 1]);
        }
    }
}
