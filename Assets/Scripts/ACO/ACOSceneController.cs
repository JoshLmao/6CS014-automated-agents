using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ACOSceneController : MonoBehaviour
{
    private ACOCON m_acoCon = new ACOCON();

    protected float DefaultPheromone
    {
        get { return m_acoCon.DefaultPheromone; }
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    protected List<ACOConnection> GenerateACOPath(int iterationThreshold, int totalNumAnts, GameObject[] waypointNodes, List<ACOConnection> connections, GameObject startNode, int maxPathLength)
    {
        return m_acoCon.ACO(iterationThreshold, totalNumAnts, waypointNodes, connections, startNode, maxPathLength);
    }
}
