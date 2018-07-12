using ClipperLib;
using System;

namespace Spatial
{
    public class Vector
    {
        public int X;
        public int Y;

        public float Length { get { return Magnitude; } }
        public float Magnitude { get { return (float)Math.Sqrt(SqrMagnitude); } }
        public float SqrMagnitude { get { return X * X + Y * Y; } }
        public static Vector Zero { get { return new Vector(0, 0); } }

        internal Vector Reversed()
        {
            return new Vector(Y, X);
        }

        /* constructors */
        public Vector() : this(0, 0) { }

        public Vector(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector(Vector v) : this(v.X, v.Y) { }

        public Vector(IntPoint intPoint) : this((int)intPoint.X, (int)intPoint.Y) { }

        internal IntPoint ToIntPoint()
        {
            return new IntPoint(X, Y);
        }

        /* arithmetic operators */

        public static Vector operator +(Vector first, Vector second)
        {
            return new Vector(first.X + second.X, first.Y + second.Y);
        }

        public static Vector operator -(Vector first, Vector second)
        {
            return new Vector(first.X - second.X, first.Y - second.Y);
        }

        public static double operator *(Vector first, Vector second)
        {
            return first.X * second.X + first.Y * second.Y;
        }

        public static Vector operator *(Vector vector, int factor)
        {
            return new Vector(vector.X * factor, vector.Y * factor);
        }

        public static Vector operator *(int factor, Vector vector)
        {
            return new Vector(vector.X * factor, vector.Y * factor);
        }

        public static Vector operator /(Vector vector, int factor)
        {
            return new Vector(vector.X / factor, vector.Y / factor);
        }

        public Vector RotateRight()
        {
            return new Vector(Y, -X);
        }

        /* comparer */
        public bool AxisValuesEqual(int v1, int v2)
        {
            return v1 == v2;
        }

        public override string ToString()
        {
            return "Vector(" + X + ", " + Y + ")";
        }

        public bool Equals(Vector other)
        {
            return other != null && AxisValuesEqual(X, other.X) && AxisValuesEqual(Y, other.Y);
        }

        internal Vector AddXY(int x, int y)
        {
            return new Vector(X + x, Y + y);
        }

        internal Vector AddX(int v)
        {
            return new Vector(X + v, Y);
        }

        internal Vector AddY(int v)
        {
            return new Vector(X, Y + v);
        }

        internal Vector SetX(int v)
        {
            return new Vector(v, Y);
        }

        internal Vector SetY(int v)
        {
            return new Vector(X, v);
        }

        public bool Equals(int x, int y)
        {
            return AxisValuesEqual(X, x) && AxisValuesEqual(Y, y);
        }

        public override bool Equals(Object other)
        {
            return other != null && other is Vector && Equals((Vector)other);
        }

        public Vector GetFlipped()
        {
            return new Vector(Y, X);
        }

        // todo is this correct?
        public override int GetHashCode()
        {
            int hash = 17;

            hash = hash * 23 + X.GetHashCode();
            hash = hash * 23 + Y.GetHashCode();

            return hash;
        }

        /* vector functions */
        public static float Distance(Vector first, Vector second)
        {
            return (float)(first - second).Magnitude;
        }

        public float Distance(Vector other)
        {
            return Distance(this, other);
        }

        public double Cross(Vector other)
        {
            return X * other.Y - other.X * Y;
        }

        public double Angle(Vector other)
        {
            return Math.Acos((this * other) / (Magnitude * other.Magnitude));
        }
        
        public Vector Clone()
        {
            return new Vector(X, Y);
        }
    }
}
