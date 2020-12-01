using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part3_SceneController : ACOSceneController
{
    [System.Serializable]
    public class SquirrelInfo
    {
        public GameObject SquirrelPrefab;
        public GameObject Start;
        public List<GameObject> Goals;
    }

    public List<SquirrelInfo> SquirrelsInfo = new List<SquirrelInfo>();

    public int ACOMaxPathLength = 10;

    [SerializeField]
    private Transform m_squirrelParent = null;

    private List<GameObject> m_instantiatedSquirrels = new List<GameObject>();

    private List<ACOConnection> m_setPath = null;

    private const string WAYPOINT_TAG = "Waypoint";

    #region MonoBehaviours
    protected override void Start()
    {
        base.Start();

        InitSquirrels();
    }

    void Update()
    {
        
    }

    // Draws debug objects in the editor and during editor play (if option set).
    void OnDrawGizmos()
    {
        if (m_setPath != null && m_setPath.Count > 0)
        {
            Vector3 offset = new Vector3(0, 0.3f, 0);
            foreach (ACOConnection aConn in m_setPath)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(aConn.FromNode.transform.position + offset, aConn.ToNode.transform.position + offset);
            }
        }
    }
    #endregion

    private void InitSquirrels()
    {
        List<GameObject> allGoalWaypoints = GetAllSceneGoalWaypoints();
        List<ACOConnection> allWaypointConnections = GetConnectionsFromWaypoints(m_allWaypoints, this.DefaultPheromone);

        /// Iterate over each SquirrelInfo and configure
        foreach(SquirrelInfo sInfo in SquirrelsInfo)
        {
            if (sInfo.Start == null)
            {
                Debug.LogError($"Missing Start on Squirrel {sInfo.SquirrelPrefab.name}");
                continue;
            }

            /// instantiate the squirrel and add to list
            GameObject inst = Instantiate(sInfo.SquirrelPrefab, m_squirrelParent);
            m_instantiatedSquirrels.Add(inst);

            /// Get the Aware component and listen to events and set move path
            AwareSquirrel aware = inst.GetComponent<AwareSquirrel>();
            if (aware)
            {
                int iterationsMax = 150;
                int ants = 50;

                List<ACOConnection> acoPath = this.GenerateACOPath(iterationsMax, ants, allGoalWaypoints.ToArray(), allWaypointConnections, sInfo.Start, ACOMaxPathLength);

                aware.SetMovePath(acoPath);

                m_setPath = acoPath;
            }
        }
    }

    private List<GameObject> GetAllWaypointsInConnection(List<Connection> conns)
    {
        List<GameObject> allObjs = new List<GameObject>();
        foreach(Connection c in conns)
        {
            allObjs.Add(c.FromNode);
        }
        var last = conns.LastOrDefault();
        if (last != null)
            allObjs.Add(last.ToNode);

        return allObjs; 
    }

    /// <summary>
    /// Gets all goal waypoints in the current scene
    /// </summary>
    /// <returns></returns>
    private List<GameObject> GetAllSceneGoalWaypoints()
    {
        List<GameObject> goalWaypoints = new List<GameObject>();

        // Find all the waypoints in the level.
        GameObject[] allWaypoints = GameObject.FindGameObjectsWithTag(WAYPOINT_TAG);
        foreach (GameObject waypoint in allWaypoints)
        {
            WaypointCON tmpWaypointCon = waypoint.GetComponent<WaypointCON>();
            if (tmpWaypointCon)
            {
                // Add waypoint if it is a goal
                if (tmpWaypointCon.WaypointType == WaypointCON.waypointPropsList.Goal)
                    goalWaypoints.Add(waypoint);
            }
        }
        return goalWaypoints;
    }

    /// <summary>
    /// Creates ACO waypoints from the provided waypoints
    /// </summary>
    /// <param name="allWaypoints"></param>
    /// <param name="defaultPheromone"></param>
    /// <returns></returns>
    private List<ACOConnection> GetConnectionsFromWaypoints(List<GameObject> allWaypoints, float defaultPheromone = 1.0f)
    {
        List<ACOConnection> connections = new List<ACOConnection>();
        foreach (GameObject waypoint in allWaypoints)
        {
            WaypointCON tmpWaypointCon = waypoint.GetComponent<WaypointCON>();
            foreach (GameObject WaypointConNode in tmpWaypointCon.Connections)
            {
                ACOConnection aConnection = new ACOConnection();
                aConnection.SetConnection(waypoint, WaypointConNode, defaultPheromone);
                connections.Add(aConnection);
            }
        }
        return connections;
    }
}
