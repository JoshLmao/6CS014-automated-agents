using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part3_SceneController : ACOSceneController
{
    [System.Serializable]
    public class ACOConfig
    {
        [Tooltip("Amount of Alpha to use in Ant Colony Optimization")]
        public float Alpha = ACOCON.DEFAULT_ALPHA;
        [Tooltip("Amount of Beta to use in Ant Colony Optimization")]
        public float Beta = ACOCON.DEFAULT_BETA;
        [Tooltip("Evaporation factor value to use in Ant Colony Optimization")]
        public float EvaporationFactor = ACOCON.DEFAULT_EVAPORATION_FACTOR;
        [Tooltip("Amount of Q to use in Ant Colony Optimization")]
        public float Q = ACOCON.DEFAULT_Q;

        [Tooltip("Maximum length of path in Ant Colony Optimization")]
        public int MaxPathLength = 15;
    }

    [System.Serializable]
    public class SquirrelInfo
    {
        public GameObject SquirrelPrefab;
        public GameObject Start;
        public List<GameObject> Goals;
    }

    public ACOConfig AntColonyConfig = new ACOConfig();

    public List<SquirrelInfo> SquirrelsInfo = new List<SquirrelInfo>();

    [SerializeField, Tooltip("Parent transform to instantiate new squirrels under")]
    private Transform m_squirrelParent = null;

    private List<GameObject> m_instantiatedSquirrels = new List<GameObject>();

    private List<ACOConnection> m_gizmoPath = null;

    private const string WAYPOINT_TAG = "Waypoint";

    #region MonoBehaviours
    protected override void Start()
    {
        base.Start();

        if (AntColonyConfig != null)
        {
            this.ConfigureACO(AntColonyConfig.Alpha, AntColonyConfig.Beta, AntColonyConfig.EvaporationFactor, AntColonyConfig.Q);
        }

        InitSquirrels();
    }

    // Draws debug objects in the editor and during editor play (if option set).
    void OnDrawGizmos()
    {
        if (m_gizmoPath != null && m_gizmoPath.Count > 0)
        {
            Vector3 offset = new Vector3(0, 0.3f, 0);
            foreach (ACOConnection aConn in m_gizmoPath)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(aConn.FromNode.transform.position + offset, aConn.ToNode.transform.position + offset);
            }
        }
    }
    #endregion

    private void InitSquirrels()
    {
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

                /// Create ACOConnection list between each goal location
                List<ACOConnection> goalACOConnections = new List<ACOConnection>();
                foreach (GameObject goal in sInfo.Goals)
                {
                    foreach (GameObject j in sInfo.Goals)
                    {
                        // If j isn't equal to current goal, add as a connection
                        if (goal != j) 
                        {
                            /// Set ACO From and To
                            ACOConnection acoConnection = new ACOConnection();
                            acoConnection.SetConnection(goal, j, 1.0f);

                            /// Query A* navigate to see if route is possible 
                            List<Connection> aStarRoute = this.Navigate(acoConnection.FromNode, acoConnection.ToNode);
                            if (aStarRoute != null && aStarRoute.Count > 0)
                            {
                                /// Is a A* route, set and add
                                acoConnection.SetAStarRoute(aStarRoute);
                                goalACOConnections.Add(acoConnection);
                            }
                            else
                            {
                                Debug.LogError($"Unable to generate an A* path between '{goal.name}' and '{j.name}'");
                            }
                        }
                    }
                }

                // Do ACO Now?
                List<ACOConnection> route = this.GenerateACOPath(iterationsMax, ants, m_allWaypoints.ToArray(), goalACOConnections, sInfo.Start, AntColonyConfig.MaxPathLength);

                // Set this squirrel to move along ACO path
                aware.SetMovePath(sInfo.Start, route);
                
                // Set Gizmo path for highlighting in Editor
                m_gizmoPath = goalACOConnections;
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
