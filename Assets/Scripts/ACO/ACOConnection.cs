using UnityEngine;
using System.Collections.Generic;

public class ACOConnection
{
    private float m_distance = 0;
    public float Distance
    {
        get { return m_distance; }
    }

    private float m_pheromoneLevel;
    public float PheromoneLevel
    {
        set { m_pheromoneLevel = value; }
        get { return m_pheromoneLevel; }
    }

    private float m_pathProbability;
    public float PathProbability
    {
        set { m_pathProbability = value; }
        get { return m_pathProbability; }
    }

    private GameObject m_fromNode;
    public GameObject FromNode
    {
        get { return m_fromNode; }
    }

    private GameObject m_toNode;
    public GameObject ToNode
    {
        get { return m_toNode; }
    }

    private List<Connection> m_route;
    //The A* route between from node and to node
    public List<Connection> Route
    {
        get { return m_route; }
    }

    public ACOConnection()
    {

    }

    public void SetConnection(GameObject fromNode, GameObject toNode, float defaultPheromoneLevel)
    {
        m_fromNode = fromNode;
        m_toNode = toNode;

        m_distance = Vector3.Distance(m_fromNode.transform.position, m_toNode.transform.position);

        PheromoneLevel = defaultPheromoneLevel;
        PathProbability = 0;
    }

    public void SetAStarRoute(List<Connection> route)
    {
        m_route = route;
    }
}
