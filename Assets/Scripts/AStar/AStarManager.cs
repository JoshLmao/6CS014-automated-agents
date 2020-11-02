using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarManager
{
    private AStar m_aStar = new AStar();

    private Graph m_aGraph = new Graph();

    private Heuristic m_aHeuristic = new Heuristic();

    public AStarManager()
    {

    }

    public void AddConnection(Connection connection)
    {
        m_aGraph.AddConnection(connection);
    }

    public List<Connection> PathfindAStar( GameObject start, GameObject end )
    {
        return m_aStar.PathfindAStar(m_aGraph, start, end, m_aHeuristic);
    }
}
