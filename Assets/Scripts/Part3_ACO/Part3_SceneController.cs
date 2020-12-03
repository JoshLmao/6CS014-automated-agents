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

        [Tooltip("Maximum amount of iterations for ACO to perform")]
        public int MaximumIterations = 150;
        [Tooltip("Amount of ants to use around the given Goal path")]
        public int Ants = 50;
        [Tooltip("Maximum length of path in Ant Colony Optimization")]
        public int MaxPathLength = 15;
    }

    [System.Serializable]
    public class SquirrelInfo
    {
        [Tooltip("Prefab to use as the squirrel")]
        public GameObject SquirrelPrefab;
        [Tooltip("Start waypoint the agent will start from. Needs to be one of the goals")]
        public GameObject Start;
        [Tooltip("List of goals the agent will navigate through")]
        public List<GameObject> Goals;
    }

    public ACOConfig AntColonyConfig = new ACOConfig();

    public List<SquirrelInfo> SquirrelsInfo = new List<SquirrelInfo>();

    [SerializeField, Tooltip("Parent transform to instantiate new squirrels under")]
    private Transform m_squirrelParent = null;

    /// list of squirrels instantiated by the scene controller
    private List<GameObject> m_instantiatedSquirrels = new List<GameObject>();

    /// Path to draw in gizmos for debugging
    private List<ACOConnection> m_gizmoPath = null;

    #region MonoBehaviours
    protected override void Start()
    {
        base.Start();

        /// Set any Ant Colony config values set from in the inspector
        if (AntColonyConfig != null)
        {
            this.ConfigureACO(AntColonyConfig.Alpha, AntColonyConfig.Beta, AntColonyConfig.EvaporationFactor, AntColonyConfig.Q);
        }

        /// Create squirrel agents
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
            if (sInfo.SquirrelPrefab == null || sInfo.Start == null)
            {
                Debug.LogError($"SquirrelInfo is incorrectly configured!");
                continue;
            }

            /// instantiate the squirrel and add to list
            GameObject inst = Instantiate(sInfo.SquirrelPrefab, m_squirrelParent);
            m_instantiatedSquirrels.Add(inst);

            /// Get the Aware component and listen to events and set move path
            AwareSquirrel aware = inst.GetComponent<AwareSquirrel>();
            if (aware)
            {
                /// Create ACOConnection list between each goal location
                List<ACOConnection> goalACOConnections = CalculateGoalsAndRoutes(sInfo.Goals);

                // Calculate ACO path from all waypoints, using the ACOConnections generated
                List<ACOConnection> route = this.GenerateACOPath(AntColonyConfig.MaximumIterations, 
                                                                    AntColonyConfig.Ants, 
                                                                    m_allWaypoints.ToArray(), 
                                                                    goalACOConnections, 
                                                                    sInfo.Start, 
                                                                    AntColonyConfig.MaxPathLength);

                // Set this squirrel to move along ACO path
                aware.SetMovePath(sInfo.Start, route);
                
                // Set Gizmo path for highlighting in Editor
                m_gizmoPath = goalACOConnections;
            }
            else
            {
                Debug.LogError("Squirrel Prefab is missing AwareSquirrel script!");
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

    /// Generates ACOConnection list from the given goals and calculates an A* route between each
    private List<ACOConnection> CalculateGoalsAndRoutes(List<GameObject> goals)
    {
        List<ACOConnection> goalACOConnections = new List<ACOConnection>();
        foreach (GameObject goal in goals)
        {
            foreach (GameObject j in goals)
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
        return goalACOConnections;
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
