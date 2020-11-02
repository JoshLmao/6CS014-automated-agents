using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingList
{
    private List<NodeRecord> NodeRecordList = new List<NodeRecord>();

    public void AddRecord(NodeRecord record)
    {
        NodeRecordList.Add(record);
    }

    public void RemoveRecord(NodeRecord record)
    {
        NodeRecordList.Remove(record);
    }

    public int GetSize()
    {
        return NodeRecordList.Count;
    }

    public NodeRecord GetSmallestElement()
    {
        NodeRecord tempRecord = new NodeRecord();
        tempRecord.EstimatedTotalCost = float.MaxValue;

        foreach(NodeRecord record in NodeRecordList)
        {
            if (record.EstimatedTotalCost < tempRecord.EstimatedTotalCost)
            {
                tempRecord = record;
            }
        }
        return tempRecord;
    }

    public bool Contains(GameObject node)
    {
        foreach(NodeRecord record in NodeRecordList)
        {
            if (record.Node.Equals(node))
            {
                return true;
            }
        }
        return false;
    }

    public NodeRecord Find(GameObject node)
    {
        foreach(NodeRecord record in NodeRecordList)
        {
            if (record.Node.Equals(node))
            {
                return record;
            }
        }
        return null;
    }
}
