using EpForceDirectedGraph.cs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeCreator : MonoBehaviour {

    public int NumNodesToCreate = 10;
    public int NumRandomEdges = 20;
    public GameObject NodeTemplate;

    [Range(0, 100)]
    public float Stiffness = 80;
    [Range(0, 100000)]
    public float Repulsion = 40000;
    [Range(0, 1)]
    public float Damping = .5f;

    Dictionary<Node, GameObject> _nodes;
    Graph _graph;
    ForceDirected2D _physics;
    
    void Start () {

        _graph = new Graph();
        _physics = new ForceDirected2D(_graph, Stiffness, Repulsion, Damping);

        _nodes = new Dictionary<Node, GameObject>();

		for (int i = 0; i < NumNodesToCreate; i++)
        {
            var visualNode = Instantiate(NodeTemplate, transform);

            var nodeData = new NodeData() { label = "Node" + i, mass = 1, initialPostion = new FDGVector3(0, 0, 0) };

            var node = _graph.CreateNode(nodeData);

            _nodes[node] = visualNode;
        }
        
        for (int i = 0; i < NumRandomEdges; i++)
        {
            var n1 = Random.Range(0, NumNodesToCreate - 1);
            var n2 = Random.Range(0, NumNodesToCreate - 1);

            var node1 = _graph.GetNode("Node" + n1);
            var node2 = _graph.GetNode("Node" + n2);

            var edgeData = new EdgeData()
            {
                length = Random.Range(.1f, 3)
            };

            _graph.CreateEdge(node1, node2, edgeData);
        }

        // add force simulation
	}
	
	void Update () {
        _physics.Calculate(Time.deltaTime);

        _physics.EachNode(HandleNodeUpdate);
        _physics.EachEdge(HandleEdgeUpdate);
    }

    void HandleEdgeUpdate(EpForceDirectedGraph.cs.Edge edge, Spring spring) {
        var from = PositionToVector3(spring.point1.position);
        var to = PositionToVector3(spring.point2.position);
        
        Debug.DrawLine(from, to, Color.white, Time.deltaTime);
    }

    void HandleNodeUpdate(Node node, Point point)
    {
        _nodes[node].transform.position = PositionToVector3(point.position);
    }

    Vector3 PositionToVector3(AbstractVector vector)
    {
        return new Vector3(vector.x, vector.y, vector.z);
    }
}
