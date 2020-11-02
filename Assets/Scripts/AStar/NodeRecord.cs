using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeRecord
{
    private GameObject m_node;
    public GameObject Node
    {
        get { return m_node; }
        set { m_node = value; }
    }

    private Connection m_connection;
    public Connection Connection
    {
        get { return m_connection; }
        set { m_connection = value; }
    }

    private float m_costSoFar;
    public float CostSoFar
    {
        get { return m_costSoFar; }
        set { m_costSoFar = value; }
    }

    private float m_estimatedTotalCost;
    public float EstimatedTotalCost
    {
        get { return m_estimatedTotalCost; }
        set { m_estimatedTotalCost = value; }
    }
}
