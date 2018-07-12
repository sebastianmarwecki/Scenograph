using System.Collections.Generic;
using System.Linq;
using AutoTiling;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.UI;

namespace Assets.Scripts.UserStudy
{
    public class Chaperones : MonoBehaviour
    {
        public float WallYEnd = 3f;
        public float WallYStart = -1f;
        public float Safety, Tiling;
        public bool ShowGround, ShowWall, ChangeAudioVolume, ChangeTimeScale, ShowGrey;
        public Color OverallColor, SimpleWallColor;
        public Camera PlayerCamera;
        public Texture SimpleWallTexture;
        public Texture[] WallMeshTextures;
        public Color[] WallColors;
        public Transform Reference;
        public bool AlwaysShowArea = false;
        public MeshFilter GroundMeshFilter;
        public Material WallMaterial;
        public Material SimpleWallMaterial;
        public List<Vector3> WallPoints;
        public Renderer GroundRenderer;

        private List<Vector3> _withoutOffset;
        private PostProcessingBehaviour _ppbehavior;
        private Renderer _wallRenderer;
        private DynamicTextureTiling _dtt;
        private float _factor;
        private float _temparature;
        private float _tint;
        private float _hueShift;
        private float _saturation;
        private float _contrast;
        private float _timeWrongZone;
        private Vector3 _meshCenter;
        private MeshFilter _wallMeshFilter;
        private Triangulator _triangulator;

        private float Alpha
        {
            set
            {
                var alpha = value < 0.001f ? 0f : value;
                if (ShowGround)
                {
                    var col = OverallColor;
                    col.a = alpha;
                    GroundRenderer.material.color = col;
                }
            }
        }
        private float WallAlphaDist
        {
            set
            {
                if (ShowWall)
                {
                    var alpha = value < 0.001f ? 0f : value;
                    alpha *= 0.6f;
                    _wallRenderer.materials[1].SetFloat("_Transparency", 1f - alpha);
                }
            }
        }
        private float Factor
        {
            set
            {
                if (Mathf.Abs(_factor - value) < 0.001f)
                    return;
                _factor = Mathf.Clamp01(value);
                if (_factor < 0.001f)
                {
                    if (_ppbehavior != null)
                        _ppbehavior.enabled = false;
                }
                else
                {

                    if (_ppbehavior != null)
                    {
                        _ppbehavior.enabled = true;
                        var settings = _ppbehavior.profile.colorGrading.settings;
                        settings.basic.temperature = _factor * _temparature;
                        settings.basic.tint = _factor * _tint;
                        settings.basic.hueShift = _factor * _hueShift;
                        settings.basic.saturation = _factor * _saturation + (1f - _factor);
                        settings.basic.contrast = _factor * _contrast + (1f - _factor);
                        _ppbehavior.profile.colorGrading.settings = settings;
                    }
                }
            }
        }
        
        
        void Start()
        {
            _triangulator = new Triangulator();
            if (ShowGrey)
            {
                _ppbehavior = PlayerCamera.gameObject.GetComponent<PostProcessingBehaviour>();
            }

            GroundRenderer.material.color = OverallColor;

            if (ShowWall)
            {
                var wall = new GameObject();
                wall.transform.parent = transform;
                wall.transform.localPosition = Vector3.zero;
                wall.transform.localRotation = Quaternion.identity;
                wall.name = "wall"; 
                _wallRenderer = wall.AddComponent<MeshRenderer>();
                _wallRenderer.materials = (new List<Material> { WallMaterial, SimpleWallMaterial }).ToArray();
                _wallMeshFilter = wall.AddComponent<MeshFilter>();
                for (var i = 0; i < WallMeshTextures.Length; i++)
                {
                    var factor = 1f / (WallMeshTextures.Length + 1);
                    _wallRenderer.materials[0].SetFloat("_MaxDistance" + (i + 1), Safety * (factor + (1f - factor) * (i + 1) / WallMeshTextures.Length));
                    _wallRenderer.materials[0].SetFloat("_MinDistance" + (i + 1), Safety * (factor + (1f - factor) * i / WallMeshTextures.Length));
                    var color = WallColors[i];
                    var colorNoAlpha = color;
                    colorNoAlpha.a = 0f;
                    _wallRenderer.materials[0].SetColor("_MinColor" + (i + 1), color);
                    _wallRenderer.materials[0].SetColor("_MaxColor" + (i + 1), colorNoAlpha);
                    _wallRenderer.materials[0].SetTexture("_Tex" + (i + 1), WallMeshTextures[i]);
                }
                _dtt = _wallMeshFilter.gameObject.AddComponent<DynamicTextureTiling>();

                _wallRenderer.materials[1].color = SimpleWallColor;
                _wallRenderer.materials[1].SetColor("_Diffusecolor", SimpleWallColor);
                _wallRenderer.materials[1].SetColor("_Speccolor", SimpleWallColor);
                _wallRenderer.materials[1].SetTexture("_MainTex", SimpleWallTexture);
            }

            if (ShowWall)// && !UseSimpleWall)
            {
                _dtt.unwrapMethod = UnwrapType.CubeProjection;
                _dtt.useUnifiedOffset = true;
                _dtt.useUnifiedScaling = true;
                _dtt.topScale = Vector2.one * Tiling;
                _dtt.CreateMeshAndUVs();
                _dtt.enabled = false;
            }
            if (_ppbehavior != null)
            {
                _temparature = _ppbehavior.profile.colorGrading.settings.basic.temperature;
                _tint = _ppbehavior.profile.colorGrading.settings.basic.tint;
                _hueShift = _ppbehavior.profile.colorGrading.settings.basic.hueShift;
                _saturation = _ppbehavior.profile.colorGrading.settings.basic.saturation;
                _contrast = _ppbehavior.profile.colorGrading.settings.basic.contrast;
                Factor = 1f;
            }

            Time.timeScale = 1f;
            AudioListener.volume = 1f;

            SetNewPositionsWithOffset(WallPoints, true);
        }

        public void SetNewPositionsWithOffset(List<Vector3> poly, bool reverse)
        {
            if(reverse)
                poly.Reverse();
            _withoutOffset = poly;

            WallPoints = new List<Vector3>();
            WallPoints.Clear();
            for(var i = 0; i < poly.Count; i++)
            {
                var v = poly[i];
                var ve = poly[i];
                v.y = WallYStart;
                ve.y = WallYEnd;
                WallPoints.Add(v);
                WallPoints.Add(ve);
                //WallPoints.Add(poly[i + 1 == poly.Count ? 0 : i + 1] + Vector3.up * WallYStart);

                //WallPoints.Add(poly[i+1 == poly.Count ? 0 : i+1] + Vector3.up * WallYEnd);
                //WallPoints.Add(poly[i] + Vector3.up * WallYEnd);
                //WallPoints.Add(poly[i + 1 == poly.Count ? 0 : i + 1] + Vector3.up * WallYStart);
            }

            if (ShowWall)
            {
                _meshCenter = Vector3.zero;
                foreach (var wp in WallPoints)
                {
                    _meshCenter += wp;
                }
                _meshCenter /= WallPoints.Count;
                ProcessMesh(WallPoints, _wallMeshFilter, false);
                _dtt.CreateMeshAndUVs();
            }
            if (ShowGround)
            {
                ProcessMesh(poly, GroundMeshFilter, true);
            }
        }

        private void ProcessMesh(List<Vector3> positions, MeshFilter filter, bool ground)
        {
            if (positions.Count >= 3)
            {
                //UVs
                var uvs = new Vector2[positions.Count];

                for (var x = 0; x < positions.Count; x++)
                {
                    if ((x % 2) == 0)
                    {
                        uvs[x] = new Vector2(0, 0);
                    }
                    else
                    {
                        uvs[x] = new Vector2(1, 1);
                    }
                }

                int[] tris;
                if (ground)
                {
                    tris = _triangulator
                        .TriangulatePolygon(
                            positions.Select(position => new Vector2(position.x, position.z)).ToArray());
                }
                else
                {
                    tris = new int[3 * positions.Count];

                    var baseIndex = 0;
                    for (var x = 0; x < tris.Length; x += 3)
                    {
                        if (x % 2 == 0)
                        {
                            tris[x] = baseIndex % positions.Count;
                            tris[x + 1] = (baseIndex + 1) % positions.Count;
                            tris[x + 2] = (baseIndex + 2) % positions.Count;
                        }
                        else
                        {
                            tris[x + 2] = baseIndex % positions.Count;
                            tris[x + 1] = (baseIndex + 1) % positions.Count;
                            tris[x] = (baseIndex + 2) % positions.Count;
                        }
                        baseIndex++;
                    }
                }

                if (filter.mesh == null)
                    filter.mesh = new Mesh();
                filter.mesh.Clear();
                filter.mesh.SetVertices(positions);
                filter.mesh.SetUVs(0, uvs.ToList());
                filter.mesh.SetTriangles(tris, 0);
                filter.mesh.name = "MyMesh";
                filter.mesh.RecalculateNormals();
                filter.mesh.RecalculateBounds();
            }
            else
            {
                filter.mesh.Clear();
            }
        }

        void Update()
        {
            if (_withoutOffset == null)
                return;
            var camRelative = Reference.InverseTransformPoint(PlayerCamera.transform.position);
            var polyDist = DistanceFromPoly(camRelative, false, _withoutOffset);
            //var dist = polyDist< 0f ? 0f : polyDist;
            var distFactor = Mathf.Clamp01((polyDist + Safety) / Safety);
            var factor = Mathf.Pow(distFactor, 0.33f);
            var inWrongZone = polyDist > 0f;
            Factor = inWrongZone ? 1f : 0f;
            Alpha = inWrongZone || AlwaysShowArea ? 1f : factor;
            WallAlphaDist = distFactor;
            if (inWrongZone)
                _timeWrongZone += Time.unscaledDeltaTime;
            else
                _timeWrongZone = 0f;
            var timeOut = _timeWrongZone > 0.5f;
            if (ChangeTimeScale)
                Time.timeScale = timeOut ? 0f : 1f;
            if (ChangeAudioVolume)
                AudioListener.volume = timeOut ? 0f : 1f;
        }

        void OnApplicationQuit()
        {
            if (_ppbehavior != null)
            {
                var settings = _ppbehavior.profile.colorGrading.settings;
                settings.basic.temperature = _temparature;
                settings.basic.tint = _tint;
                settings.basic.hueShift = _hueShift;
                settings.basic.saturation = _saturation;
                settings.basic.contrast = _contrast;
                _ppbehavior.profile.colorGrading.settings = settings;
            }
        }

        #region Helper

        private static float DistanceFromLine(Vector2 p, Vector2 l1, Vector2 l2)
        {
            float xDelta = l2.x - l1.x;
            float yDelta = l2.y - l1.y;

            //	final double u = ((p3.getX() - p1.getX()) * xDelta + (p3.getY() - p1.getY()) * yDelta) / (xDelta * xDelta + yDelta * yDelta);
            float u = ((p.x - l1.x) * xDelta + (p.y - l1.y) * yDelta) / (xDelta * xDelta + yDelta * yDelta);

            Vector2 closestPointOnLine;
            if (u < 0)
            {
                closestPointOnLine = l1;
            }
            else if (u > 1)
            {
                closestPointOnLine = l2;
            }
            else
            {
                closestPointOnLine = new Vector2(l1.x + u * xDelta, l1.y + u * yDelta);
            }


            var d = p - closestPointOnLine;
            return Mathf.Sqrt(d.x * d.x + d.y * d.y); // distance
        }

        private static float DistanceFromPoly(Vector3 pp, bool insideIsZero, List<Vector3> points)
        {
            if (points.Count < 3)
                return 0f;
            var inside = PointInPolygon(pp, points);
            if (insideIsZero && inside)
                return 0f;
            var p = new Vector2(pp.x, pp.z);
            var poly = points.Select(mp => new Vector2(mp.x, mp.z)).ToList();
            float result = 10000;

            // check each line
            for (int i = 0; i < poly.Count; i++)
            {
                int previousIndex = i - 1;
                if (previousIndex < 0)
                {
                    previousIndex = poly.Count - 1;
                }

                Vector2 currentPoint = poly[i];
                Vector2 previousPoint = poly[previousIndex];

                float segmentDistance = DistanceFromLine(new Vector2(p.x, p.y), previousPoint, currentPoint);

                if (segmentDistance < result)
                {
                    result = segmentDistance;
                }
            }
            if (inside)
                result *= -1;

            return result;
        }

        private static bool PointInPolygon(Vector3 point, List<Vector3> polygon)
        {
            var rev = new List<Vector3>(polygon);
            point.y = 0f;
            // Get the angle between the point and the
            // first and last vertices.
            var maxPoint = rev.Count - 1;
            var totalAngle = Vector3.Angle(rev[maxPoint] - point, rev[0] - point);

            // Add the angles from the point
            // to each other pair of vertices.
            for (var i = 0; i < maxPoint; i++)
            {
                totalAngle += Vector3.Angle(rev[i] - point, rev[i + 1] - point);
            }
            // The total angle should be 2 * PI or -2 * PI if
            // the point is in the polygon and close to zero
            // if the point is outside the polygon.
            totalAngle %= 360f;
            if (totalAngle > 359)
                totalAngle -= 360f;
            return (Mathf.Abs(totalAngle) < 0.001f);
        }
        
        #endregion

    }
}

