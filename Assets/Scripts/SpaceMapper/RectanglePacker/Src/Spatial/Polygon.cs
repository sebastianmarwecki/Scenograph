using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Object = System.Object;

namespace Spatial
{
    using Path = List<IntPoint>;

    public class Tuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public Tuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        } 
    }

    // todo equality operators
    // todo holes
    public class Polygon
    {
        public List<Vector> Points;

        #region constructor
        public Polygon()
        {
            Points = new List<Vector>();
        }
        
        public Polygon(List<Vector> vectorList)
        {
            Points = vectorList;
        }

        public Polygon(Path path)
        {
            Points = path.Select(intPoint => new Vector(intPoint)).ToList();
        }

        public Path GetPoints()
        {
            return Points.Select(point => point.ToIntPoint()).ToList();
        }

        public Polygon Clone()
        {
            List<Vector> newList = new List<Vector>(Points.Count);
            Points.ForEach(vector => newList.Add(vector));
            return new Polygon(newList);
        }

        public Polygon DeepClone()
        {
            Polygon clone = new Polygon();
            foreach (Vector point in Points)
            {
                clone.Points.Add(point.Clone());
            }
            return clone;
        }

        public static Polygon AsRectangle(Vector dimensions, Vector offset = null)
        {
            Polygon polygon = new Polygon(new List<Vector> {
                new Vector(0, 0),
                new Vector(dimensions.X, 0),
                new Vector(dimensions.X, dimensions.Y),
                new Vector(0, dimensions.Y)
            });

            if (offset != null)
                polygon += offset;

            return polygon;
        }
        #endregion

        #region arithmetic operators
        public static Polygon operator +(Polygon polygon, Vector offset)
        {
            Polygon newPolygon = new Polygon();
            polygon.Points.ForEach(point => newPolygon.Points.Add(point + offset));
            return newPolygon;
        }
        #endregion

        #region geometric operations
        public Vector Center
        {
            get
            {
                Vector center = new Vector();

                if (Points.Count > 0)
                {
                    Points.ForEach(point => center += point);
                    center /= Points.Count;
                }

                return center;
            }
            set
            {
                Vector oldCenter = Center;
                List<Vector> newPoints = new List<Vector>();
                Points.ForEach(point => newPoints.Add(point - oldCenter + value));
                Points = newPoints;
            }
        }

        public double Circumference
        {
            get
            {
                float circumference = 0;
                for (int i = 0; i < Points.Count; i++)
                {
                    circumference += Vector.Distance(Points[(i + 1) % Points.Count], Points[i]);
                }
                return circumference;
            }
        }
        
        #region Clipping
        internal List<Polygon> Intersection(Polygon other)
        {
            return ClipperUtility.Execute(this, other, ClipType.ctIntersection);
        }

        internal List<Polygon> Difference(Polygon other)
        {
            return ClipperUtility.Execute(this, other, ClipType.ctDifference);
        }

        internal float Area()
        {
            return (float)Clipper.Area(GetPoints());
        }
        #endregion

        public Tuple<Vector, Vector> EnclosingVectors
        {
            get
            {
                int minX = int.MaxValue;
                int minY = int.MaxValue;
                int maxX = int.MinValue;
                int maxY = int.MinValue;

                foreach (Vector point in Points)
                {
                    if (point.X < minX) minX = point.X;
                    if (point.Y < minY) minY = point.Y;
                    if (maxX < point.X) maxX = point.X;
                    if (maxY < point.Y) maxY = point.Y;
                }

                return new Tuple<Vector, Vector>(new Vector(minX, minY), new Vector(maxX, maxY));
            }
        }

        public Vector Dimension
        {
            get
            {
                Tuple<Vector, Vector> enclosingVectors = EnclosingVectors;
                return enclosingVectors.Item2 - enclosingVectors.Item1;
            }
        }
        #endregion

        #region Enumeration
        public IEnumerator<Vector> GetEnumerator()
        {
            return Points.GetEnumerator();
        }
        #endregion
    }
}
