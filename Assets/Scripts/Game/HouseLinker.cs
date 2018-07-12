using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.SpaceMapper;
using UnityEngine;

namespace Assets.Scripts.Game
{
    public class HouseLinker : SimpleLinker
    {
        public string Type2Connect;

        public Shader Shade;
        public Material Replace;
        public List<Material> ReplaceLighter;
        public List<Color> Colors;

        private List<string> _lighterNames;
        private Material _mat, _matLight;
        private static int _colorIndex;

        internal override void Link(TrackingSpaceRoot ram, List<AbstractCompiler> compilers)
        {
            base.Link(ram, compilers);
            
            var typeAsType = Type2Connect == "" ? null : Find(Type2Connect);
            if (typeAsType != null)
            {
                var typeObjects = transform.GetComponentsInChildren(typeAsType);
                foreach (var to1 in typeObjects)
                {
                    foreach (var to2 in typeObjects)
                    {
                        if (to1 == to2)
                            continue;
                        var go = new GameObject();
                        go.transform.parent = transform;
                        var lineRenderer = go.AddComponent<LineRenderer>();
                        var positions = new Vector3[2];
                        positions[0] = to1.transform.position;
                        positions[1] = to2.transform.position;
                        lineRenderer.endWidth = 0.1f;
                        lineRenderer.startWidth = 0.1f;
                        lineRenderer.startColor = Color.blue;
                        lineRenderer.endColor = Color.blue;
                        lineRenderer.positionCount = 2;
                        lineRenderer.SetPositions(positions);
                    }
                }
            }

            foreach (var c in compilers)
            {
                var door2Door = c.gameObject.GetComponent<SmallDoor2DoorTransitionCompiler>();
                if (door2Door == null)
                    continue;
                door2Door.HideStuff();
            }

            //colorize rooms
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
                ProcessRenderer(rend);
        }

        private void Awake()
        {
            if (++_colorIndex >= Colors.Count)
                _colorIndex = 0;
            var index = _colorIndex;
            _lighterNames = ReplaceLighter.Select(rl => rl.name).ToList();
            var color = Colors[index];
            var lightColor = Color.Lerp(Color.white, color, 0.7f);
            _mat = new Material(Shade)
            {
                name = Replace.name + " copy",
                color = color
            };
            _matLight = new Material(Shade)
            {
                name = ReplaceLighter[0].name,
                color = lightColor
            };
        }

        public void ColorizeThis(List<Renderer> goColorizeRends)
        {
            foreach (var rend in goColorizeRends)
                ProcessRenderer(rend);
        }

        private void ProcessRenderer(Renderer rend)
        {
            var rendName = rend.material.name.Split(' ')[0];
            if (rendName == Replace.name)
            {
                rend.material = _mat;
            }
            else if (_lighterNames.Contains(rendName))
            {
                rend.material = _matLight;
            }
        }

        public static Type Find(string qualifiedTypeName)
        {
            var t = Type.GetType(qualifiedTypeName);

            if (t != null)
            {
                return t;
            }
            else
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    t = asm.GetType(qualifiedTypeName);
                    if (t != null)
                        return t;
                }
                return null;
            }
        }
    }

}
