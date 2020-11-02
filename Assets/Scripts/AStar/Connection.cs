using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Connection
{
    private float m_cost = 0f;
    public float Cost
    {
        get
        {
            if (m_cost == 0)
            {
                m_cost = Vector3.Distance(FromNode.transform.position, ToNode.transform.position);
            }
            return m_cost;
        }
        set { m_cost = value; }
    }

    private GameObject m_fromNode = null;
    public GameObject FromNode
    {
        get { return m_fromNode; }
        set
        {
            m_fromNode = value;
            m_cost = 0;
        }
    }

    private GameObject m_toNode = null;
    public GameObject ToNode
    {
        get { return m_toNode; }
        set
        {
            m_toNode = value;
            m_cost = 0;
        }
    }
}
