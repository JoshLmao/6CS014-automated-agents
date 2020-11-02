using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingTester : MonoBehaviour
{
    private AStarManager m_aStarManager = new AStarManager();

    private List<GameObject> m_waypoints = new List<GameObject>();

    private List<Connection> m_connectionArray = new List<Connection>();

    [SerializeField]
    private GameObject m_start;
    [SerializeField]
    private GameObject m_end;

    private Vector3 m_offset = new Vector3(0, 0.3f, 0);

    private GameObject m_currentTargetNode = null;
    private int m_currentTargetNodeIndex = 0;
    private bool m_isReversing = false;

    private void Start()
    {
        if (m_start == null || m_end == null)
        {
            Debug.Log("No start or end waypoints");
            return;
        }

        GameObject[] goWithWaypointTags = GameObject.FindGameObjectsWithTag("Waypoint");

        foreach (GameObject waypoint in goWithWaypointTags)
        {
            WaypointCON tempCon = waypoint.GetComponent<WaypointCON>();
            if (tempCon)
                m_waypoints.Add(waypoint);
        }

        foreach (GameObject waypoint in m_waypoints)
        {
            WaypointCON tempCon = waypoint.GetComponent<WaypointCON>();
            foreach (GameObject waypointConNode in tempCon.Connections)
            {
                Connection aConnection = new Connection();
                aConnection.FromNode = waypoint;
                aConnection.ToNode = waypointConNode;

                m_aStarManager.AddConnection(aConnection);
            }
        }

        m_connectionArray = m_aStarManager.PathfindAStar(m_start, m_end);

        // If no target node set, set initial position and target position
        if (m_currentTargetNode == null)
        {
            m_currentTargetNode = m_connectionArray[m_currentTargetNodeIndex].ToNode;
            this.transform.position = m_currentTargetNode.transform.position;
        }
    }

    private void OnDrawGizmos()
    {
        foreach (Connection aConnection in m_connectionArray)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(aConnection.FromNode.transform.position + m_offset, aConnection.ToNode.transform.position + m_offset);
        }
    }

    private void Update()
    {
        float speed = 1.0f;
        float step = speed * Time.deltaTime; // calculate distance to move
        this.transform.position = Vector3.MoveTowards(transform.position, m_currentTargetNode.transform.position, step);

        // If nearly reached destination
        if (Vector3.Distance(transform.position, m_currentTargetNode.transform.position) < 0.001f)
        {
            if (m_currentTargetNodeIndex >= m_connectionArray.Count)
            {
                m_isReversing = true;
            }
            else if (m_currentTargetNodeIndex <= 0)
            {
                m_isReversing = false;
            }

            // Check if target index is before last
            if (!m_isReversing)
            {
                // Increment and set new target game object
                m_currentTargetNodeIndex++;
            }
            else if (m_isReversing)
            {
                m_currentTargetNodeIndex--;
            }

            m_currentTargetNode = m_connectionArray[m_currentTargetNodeIndex].ToNode;
        }
    }
}
