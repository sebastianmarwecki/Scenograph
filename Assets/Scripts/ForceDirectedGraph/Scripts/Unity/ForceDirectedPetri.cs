using System;
using Assets.Scripts.Petrinet;
using EpForceDirectedGraph.cs;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.SpaceMapper;
using UnityEngine;


namespace SpaceMapper
{
    [Serializable]
    public class ForceLayoutSettings
    {
        [Range(0, 100)]
        public float Stiffness = 50;
        [Range(0, 100000)]
        public float Repulsion = 10000;
        [Range(0, 1)]
        public float Damping = .5f;
        public bool UseViewport = true;
        //[Range(0, 10)]
        //public float LiveSimulationSpeed = 1;
    }

    [Serializable]
    public class ForceDirectedPetri {
        Dictionary<PetrinetCondition, CondNode> _condToNodes;
        Dictionary<HardwareRequirements, HardwareNode> _hardwareNodes;
        Graph _graph;
        ForceDirected2D _physics;
        List<PetriNode> _petriNodes;
        ForceLayoutSettings _layoutSettings;
        Vector2 _bottomLeft;
        Vector2 _topRight;
        //public static Transform Reference;

        public ForceDirectedPetri(Dictionary<PetrinetCondition, Valuation> conds, Dictionary<HardwareRequirements, Valuation> hard, ForceLayoutSettings settings, Vector3 bottomLeft, Vector3 topRight, Transform reference)
        {
            bottomLeft = reference.InverseTransformPoint(bottomLeft);
            topRight = reference.InverseTransformPoint(topRight);
            _bottomLeft = new Vector2(Mathf.Min(bottomLeft.x, topRight.x), Mathf.Min(bottomLeft.z, topRight.z));
            _topRight = new Vector2(Mathf.Max(bottomLeft.x, topRight.x), Mathf.Max(bottomLeft.z, topRight.z));
            _bottomLeft *= .8f;
            _topRight *= .8f;

            _layoutSettings = settings;
            _graph = new Graph();
            _physics = new ForceDirected2D(_graph, _layoutSettings.Stiffness, _layoutSettings.Repulsion, _layoutSettings.Damping);

            _condToNodes = new Dictionary<PetrinetCondition, CondNode>();
            _hardwareNodes = new Dictionary<HardwareRequirements, HardwareNode>();

            _petriNodes = new List<PetriNode>();
            PetriNode.Reference = reference;
            //Reference = reference;

            Func<Vector3, bool> withinBoundaries = delegate (Vector3 pos)
            {
                return _bottomLeft.x <= pos.x && pos.x <= _topRight.x && _bottomLeft.y <= pos.z && pos.z <= _topRight.y;
            };

            foreach (var condRadius in conds)
            {
                var condNode = new CondNode(condRadius.Key, condRadius.Value.Radius, condRadius.Value.Weight);

                if (!withinBoundaries(condNode.Visual.position))
                    Debug.LogWarning(condNode.Visual.name + " not within boundaries");

                _petriNodes.Add(condNode);
                _graph.AddNode(condNode);
                _condToNodes[condRadius.Key] = condNode;
            }
            
            foreach (var hardwareRadius in hard)
            {
                var hardwareNode = new HardwareNode(hardwareRadius.Key, hardwareRadius.Value.Radius, hardwareRadius.Value.Weight);

                if (!withinBoundaries(hardwareNode.Visual.position))
                    Debug.LogWarning(hardwareNode.Visual.name + " not within boundaries");

                _petriNodes.Add(hardwareNode);
                _graph.AddNode(hardwareNode);
                _hardwareNodes[hardwareRadius.Key] = hardwareNode;

                foreach (var transition in hardwareRadius.Key.gameObject.GetComponents<PetrinetTransition>())
                {
                    transition.In.ForEach(cond => AddEdge(_condToNodes[cond], hardwareNode));
                    transition.Out.ForEach(cond => AddEdge(hardwareNode, _condToNodes[cond]));
                }
            }

            foreach (var condNode in _condToNodes)
            {
                if (condNode.Key.Type == PetrinetCondition.ConditionType.Place)
                {
                    condNode.Value.Kids = 
                        condNode.Key.GetComponentsInChildren<HardwareRequirements>()
                        .Select(kid => _hardwareNodes[kid])//kid => kid.HardwareRequirements.transform)
                        //.Distinct()/
                        .ToList();

                    condNode.Value.Kids.ForEach(kid => AddEdge(condNode.Value, kid));
                }
            }
        }

        private void AddEdge(PetriNode source, PetriNode target)
        {
            var length = source.Radius + target.Radius + .1f;
            //var length = .1f;
            _graph.CreateEdge(source, target, new EdgeData() { length = length });
        }
        
        public void SimulateAhead(float totalTime, float stepTime)
        {
            while (totalTime > 0)
            {
                Update(stepTime);

                totalTime -= stepTime;
            }
        }

        public void Update(float stepTime)
        {
            Step(stepTime);

            KeepWithinBounds();

            // todo debug connections
            //foreach (var edge in _graph.edges)
            //{
            //    var unityPos1 = PositionToVector3(_physics.GetPoint(edge.Source).position);
            //    var unityPos2 = PositionToVector3(_physics.GetPoint(edge.Target).position);
            //    Debug.Log("Draw line from " + unityPos1 + " to " + unityPos2);

            //    Debug.DrawLine(unityPos1, unityPos2, Color.blue, Time.unscaledDeltaTime);
            //}
        }

        private void KeepWithinBounds()
        {
            if (_layoutSettings.UseViewport)
                _petriNodes.ForEach(petriNode =>
                {
                    var position = _physics.GetPoint(petriNode).position;

                    position.x = Mathf.Clamp(position.x, _bottomLeft.x, _topRight.x) * .9f;
                    position.y = Mathf.Clamp(position.y, _bottomLeft.y, _topRight.y) * .9f;
                });
        }

        public void UpdateVisuals()
        {
            _petriNodes.ForEach(petriNode =>
                petriNode.UpdateVisual(_physics.GetPoint(petriNode).position));
        }

        private void Step(float deltaTime)
        {
            _physics.Repulsion = _layoutSettings.Repulsion;
            _physics.Stiffness = _layoutSettings.Stiffness;
            _physics.Damping = _layoutSettings.Damping;

            _physics.Calculate(deltaTime);
        }

        public static Vector3 PositionToVector3(AbstractVector vector)
        {
            return new Vector3(vector.x, 0f, vector.y);
        }

        public static AbstractVector Vector3ToPosition(Vector3 vector3)
        {
            return new FDGVector2(vector3.x, vector3.z);
        }

        private abstract class PetriNode : Node
        {
            public string Name;
            public float Radius;
            public abstract Transform Visual { get; }
            public static Transform Reference;

            protected PetriNode(string name, Vector3 initialPosition, float radius, float weight) :
                base(name,
                    new NodeData()
                    {
                        initialPostion = Vector3ToPosition(initialPosition),
                        mass = weight
                    })
            {
                Name = name;
                Radius = radius;
            }

            public void UpdateVisual(AbstractVector vec)
            {
                var newPos = Reference.TransformPoint(PositionToVector3(vec));
                newPos.y = Reference.position.y;
                Visual.position = newPos;
            }
        }

        private class CondNode : PetriNode
        {
            public PetrinetCondition Condition;
            public List<HardwareNode> Kids = new List<HardwareNode>();
            //readonly float originalY;

            public override Transform Visual { get { return Condition.VisualReferenceInEditor.transform; } }

            public CondNode(PetrinetCondition cond, float radius, float weight) :
                base(cond.GetInstanceID().ToString(), Reference.InverseTransformPoint(cond.VisualReferenceInEditor.transform.position), radius, weight)
            {
                Condition = cond;
                //originalY = Visual.transform.position.y;
            }

            //public override void UpdateVisual(AbstractVector vec, Transform reference)
            //{
            //    //Kids.ForEach(kid => kid.Visual.parent = null);

            //    var newPos = PositionToVector3(vec);
            //    newPos.y = originalY;
            //    Visual.position = newPos;

            //    //Kids.ForEach(kid => kid.Visual.parent = Visual);
            //}
        }

        private class HardwareNode : PetriNode
        {
            readonly HardwareRequirements Hardware;
            public override Transform Visual { get { return Hardware.VisualReferenceInEditor.transform; } }
            //readonly float originalY;

            public HardwareNode(HardwareRequirements hardware, float radius, float weight) :
                base(hardware.GetInstanceID().ToString(), Reference.InverseTransformPoint(hardware.VisualReferenceInEditor.transform.position), radius,
                    weight)
            {
                Hardware = hardware;
               // originalY = Visual.transform.position.y;
            }

            //public override void UpdateVisual(AbstractVector vec, Transform reference)
            //{
            //    var newPos = PositionToVector3(vec);
            //    newPos.y = originalY;
            //    Visual.position = newPos;
            //}
        }
    }
}