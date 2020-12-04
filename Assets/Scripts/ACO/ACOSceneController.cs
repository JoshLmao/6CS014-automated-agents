using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ACOSceneController : AStarSceneController
{
    private ACOCON m_acoCon = new ACOCON();

    protected float DefaultPheromone
    {
        get { return m_acoCon.DefaultPheromone; }
    }

    protected void ConfigureACO(float alpha = 1.0f, float beta = 0.0001f, float evaporationFactor = 0.5f, float q = 0.0006f)
    {
        m_acoCon.SetAlpha(alpha);
        m_acoCon.SetBeta(beta);
        m_acoCon.SetEvaporationFactor(evaporationFactor);
        m_acoCon.SetQ(q);
    }

    protected List<ACOConnection> GenerateACOPath(int iterationThreshold, int totalNumAnts, GameObject[] waypointNodes, List<ACOConnection> connections, GameObject startNode, int maxPathLength)
    {
        return m_acoCon.ACO(iterationThreshold, totalNumAnts, waypointNodes, connections, startNode, maxPathLength);
    }
}
