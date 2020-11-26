using System.Collections.Generic;
using UnityEngine;

public class ACOAnt
{
    private float m_antTourLength = 0;
    public float AntTourLength
    {
        set { m_antTourLength = value; }
        get { return m_antTourLength; }
    }

    private List<ACOConnection> m_antTravelledConnections = new List<ACOConnection>();
    public List<ACOConnection> AntTravelledConnections
    {
        get { return m_antTravelledConnections; }
    }

    private GameObject m_startNode;
    public GameObject StartNode
    {
        set { m_startNode = value; }
        get { return m_startNode; }
    }

    public ACOAnt()
    {

    }

    public void AddAntTourLength(float antTourLength)
    {
        AntTourLength += antTourLength;
    }

    public void AddTravelledConnection(ACOConnection connection)
    {
        AntTravelledConnections.Add(connection);
    }
}
