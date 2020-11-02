using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph
{
    private List<Connection> WaypointConnections = new List<Connection>();


    public void AddConnection(Connection aConnection)
    {
        WaypointConnections.Add(aConnection);
    }

    public List<Connection> GetConnections(GameObject fromNode)
    {
        List<Connection> tempConnections = new List<Connection>();

        foreach(Connection aConnection in WaypointConnections)
        {
            if (aConnection.FromNode.Equals(fromNode))
            {
                tempConnections.Add(aConnection);
            }
        }

        return tempConnections;
    }
}
