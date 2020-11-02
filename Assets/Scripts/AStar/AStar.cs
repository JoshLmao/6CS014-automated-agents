using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStar
{
    public AStar()
    {

    }

    public List<Connection> PathfindAStar(Graph graph, GameObject start, GameObject end, Heuristic heuristic)
    {
        NodeRecord startRecord = new NodeRecord();
        startRecord.Node = start;
        startRecord.Connection = null;
        startRecord.CostSoFar = 0;
        startRecord.EstimatedTotalCost = heuristic.Estimate(start, end);

        PathfindingList openList = new PathfindingList();
        PathfindingList closedList = new PathfindingList();

        openList.AddRecord(startRecord);

        NodeRecord currentRecord = null;
        List<Connection> connections;

        while (openList.GetSize() > 0)
        {
            currentRecord = openList.GetSmallestElement();

            if (currentRecord.Node.Equals(end))
                break;

            connections = graph.GetConnections(currentRecord.Node);

            GameObject endNode;
            float endNodeCost;
            NodeRecord endNodeRecord;
            float endNodeHeuristic;

            foreach( Connection aConnection in connections)
            {
                endNode = aConnection.ToNode;
                endNodeCost = currentRecord.CostSoFar + aConnection.Cost;

                if (closedList.Contains(endNode))
                {
                    endNodeRecord = closedList.Find(endNode);

                    if (endNodeRecord.CostSoFar <= endNodeCost)
                        continue;

                    closedList.RemoveRecord(endNodeRecord);

                    endNodeHeuristic = endNodeRecord.EstimatedTotalCost - endNodeRecord.CostSoFar;
                }
                else if (openList.Contains(endNode))
                {
                    endNodeRecord = openList.Find(endNode);

                    if (endNodeRecord.CostSoFar <= endNodeCost)
                        continue;

                    endNodeHeuristic = endNodeRecord.EstimatedTotalCost - endNodeRecord.CostSoFar;
                }
                else
                {
                    endNodeRecord = new NodeRecord();
                    endNodeRecord.Node = endNode;

                    endNodeHeuristic = heuristic.Estimate(endNode, end);
                }

                endNodeRecord.CostSoFar = endNodeCost;
                endNodeRecord.Connection = aConnection;
                endNodeRecord.EstimatedTotalCost = endNodeCost + endNodeHeuristic;

                if (!openList.Contains(endNode))
                    openList.AddRecord(endNodeRecord);
            }

            openList.RemoveRecord(currentRecord);
            closedList.AddRecord(currentRecord);
        }

        List<Connection> tempList = new List<Connection>();
        if (!currentRecord.Node.Equals(end))
        {
            return tempList;
        }
        else
        {
            while (!currentRecord.Node.Equals(start))
            {
                tempList.Add(currentRecord.Connection);

                if (currentRecord.Connection == null)
                {
                    Debug.Log("Current Record " + currentRecord.ToString() + " is null");
                }
                if (currentRecord.Connection.FromNode == null)
                {
                    Debug.Log("Current FromNode " + currentRecord.Connection.FromNode.gameObject.name + " is null");
                }

                currentRecord = closedList.Find(currentRecord.Connection.FromNode);
            }

            // Reverse path and return
            List<Connection> tempList2 = new List<Connection>();
            for (int i = tempList.Count - 1; i >= 0; i--)
            {
                tempList2.Add(tempList[i]);
            }
            return tempList2;
        }
    }
}
