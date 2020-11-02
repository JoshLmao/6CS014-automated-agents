using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Heuristic
{
    public float Estimate(GameObject startNode, GameObject goalNode)
    {
        return Vector3.Distance(startNode.transform.position, goalNode.transform.position);
    }
}
