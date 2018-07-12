using ClipperLib;
using System.Collections.Generic;
using System.Linq;

namespace Spatial
{
    using Path = List<IntPoint>;
    using Paths = List<List<IntPoint>>;

    class ClipperUtility
    {
        public static List<Polygon> Execute(
            Polygon a, Polygon b, ClipType clipType)
        {
            Paths solution = new Paths();

            Clipper clipper = new Clipper();
            
            clipper.AddPath(a.GetPoints(), PolyType.ptSubject, true);
            clipper.AddPath(b.GetPoints(), PolyType.ptClip, true);
            clipper.Execute(clipType, solution);

            List<Polygon> solutionPolygons = new List<Polygon>();
            foreach (Path path in solution)
            {
                Polygon polygon = new Polygon(path);
                solutionPolygons.Add(polygon);
            }

            return solutionPolygons;
        }

        public static List<Polygon> Execute(
            List<Polygon> a, List<Polygon> b, ClipType clipType)
        {
            Paths solution = new Paths();

            Clipper clipper = new Clipper();

            var aPaths = a.Select(poly => poly.GetPoints()).ToList();
            clipper.AddPaths(aPaths, PolyType.ptSubject, true);
            var bPaths = b.Select(poly => poly.GetPoints()).ToList();
            clipper.AddPaths(bPaths, PolyType.ptClip, true);
            clipper.Execute(clipType, solution);

            List<Polygon> solutionPolygons = new List<Polygon>();
            foreach (Path path in solution)
            {
                Polygon polygon = new Polygon(path);
                solutionPolygons.Add(polygon);
            }

            return solutionPolygons;
        }
    }
}