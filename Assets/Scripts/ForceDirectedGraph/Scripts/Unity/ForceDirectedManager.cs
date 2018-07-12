using EpForceDirectedGraph.cs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SerializeField]
public class ForceDirectedManager {
    [Range(0, 100)]
    public float Stiffness = 80;
    [Range(0, 100000)]
    public float Repulsion = 40000;
    [Range(0, 1)]
    public float Damping = .5f;

    Dictionary<Node, Transform> _nodeToDisplay;
    Dictionary<Transform, Node> _displayToNode;
    Graph _graph;
    ForceDirected2D _physics;

    public ForceDirectedManager() {
        _graph = new Graph();
        _physics = new ForceDirected2D(_graph, Stiffness, Repulsion, Damping);

        _nodeToDisplay = new Dictionary<Node, Transform>();
        _displayToNode = new Dictionary<Transform, Node>();
    }

    public void AddNode(Transform nodeDisplay)
    {
        var nodeData = new NodeData()
        {
            label = "" + nodeDisplay.GetInstanceID(),
            mass = 1,
            initialPostion = Vector3ToPosition(nodeDisplay.position)
        };

        var node = _graph.CreateNode(nodeData);

        _nodeToDisplay[node] = nodeDisplay;
        _displayToNode[nodeDisplay] = node;
    }

    public void AddEdge(Transform nodeDisplay1, Transform nodeDisplay2)
    {
        _graph.CreateEdge(_displayToNode[nodeDisplay1], _displayToNode[nodeDisplay2]);
    }

    public void Update(float deltaTime)
    {
        _physics.Calculate(deltaTime);

        _physics.EachNode(HandleNodeUpdate);
    }

    void HandleNodeUpdate(Node node, Point point)
    {
        _nodeToDisplay[node].transform.position = PositionToVector3(point.position);
    }

    Vector3 PositionToVector3(AbstractVector vector)
    {
        return new Vector3(vector.x, vector.y, vector.z);
    }

    AbstractVector Vector3ToPosition(Vector3 vector3)
    {
        return new FDGVector3(vector3.x, vector3.y, vector3.z);
    }
}
