using Spatial;

namespace Packer
{
    internal class Placing
    {
        public Vector Position;
        public bool Flipped;

        public Placing(int X, int Y, bool flipped)
        {
            Position = new Vector(X, Y);
            Flipped = flipped;
        }
    }
}