using System;
using Packer;
using System.Collections.Generic;
using NUnit.Framework;
using Spatial;

namespace PackerTests
{
    [TestFixture]
    public class PolygonGeneratorTest
    {
        [Test]
        public void TestHShape()
        {
            bool[,] roomLayout = Helper.GenerateRoomLayout(5, 5, 
                new List<Vector> {
                    new Vector(2, 0), new Vector(2, 1),
                    new Vector(1, 3), new Vector(2, 3), new Vector(3, 3),
                    new Vector(1, 4), new Vector(2, 4), new Vector(3, 4)
                });

            var layoutString = Helper.GetStringRepresentation(roomLayout, available => available ? "1" : "0");
            Console.WriteLine(layoutString);

            Polygon roomPolygon = Helper.GetPolygonFromBoolArray(roomLayout);
            
            var expectedPoints = GetExpectedPoints(0, 0, 2, 0, 2, 2, 3, 2, 3, 0, 5, 0, 5, 5, 4, 5, 4, 3, 1, 3, 1, 5, 0, 5);
            
            CollectionAssert.AreEqual(expectedPoints, roomPolygon.Points);
        }

        [Test]
        public void TestPlusShape()
        {
            bool[,] roomLayout = Helper.GenerateRoomLayout(3, 3,
                new List<Vector> {
                    new Vector(0, 0), new Vector(2, 0),
                    new Vector(0, 2), new Vector(2, 2)
                });

            var layoutString = Helper.GetStringRepresentation(roomLayout, available => available ? "1" : "0");
            Console.WriteLine(layoutString);

            Polygon roomPolygon = Helper.GetPolygonFromBoolArray(roomLayout);

            var expectedPoints = GetExpectedPoints(1, 0, 2, 0, 2, 1, 3, 1, 3, 2, 2, 2, 2, 3, 1, 3, 1, 2, 0, 2, 0, 1, 1, 1);

            CollectionAssert.AreEqual(expectedPoints, roomPolygon.Points);
        }

        public List<Vector> GetExpectedPoints(params int[] coords)
        {
            if (coords.Length % 2 == 1)
            {
                Assert.IsTrue(false);
            }

            var numPoints = coords.Length / 2;
            var points = new List<Vector>();
            for (int i = 0; i < numPoints; i++)
            {
                points.Add(new Vector(coords[2 * i], coords[2 * i + 1]));
            }

            return points;
        }
    }
}
