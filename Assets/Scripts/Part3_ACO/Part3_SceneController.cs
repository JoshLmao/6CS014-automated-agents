using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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
    public class AgentInfo
    {
        [Tooltip("Prefab model to use as the agent")]
        public GameObject AgentPrefab;
        [Tooltip("Amount of cargo for Agent to start with")]
        public int CargoAmount;
        [Tooltip("Start waypoint the agent will start from. Needs to be one of the goals")]
        public GameObject Start;
        [Tooltip("List of goals the agent will navigate through")]
        public List<GameObject> Goals;
    }

    /// <summary>
    /// Config values for Ant Colony Optimization to be edited in the inspector
    /// </summary>
    public ACOConfig AntColonyConfig = new ACOConfig();
    /// <summary>
    /// List of agent info to be edited in inspector
    /// </summary>
    public List<AgentInfo> AgentsInfo = new List<AgentInfo>();

    [SerializeField, Tooltip("Parent transform to instantiate new agents under")]
    private Transform m_agentParent = null;

    /// <summary>
    /// List of agents instantiated by the scene controller
    /// </summary>
    private List<GameObject> m_instantiatedAgents = new List<GameObject>();

    /// <summary>
    /// List of active trucks and their current travel to connection
    /// </summary>
    private Dictionary<ACOTruck, IConnection> m_currentTruckConnections = new Dictionary<ACOTruck, IConnection>();

    /// <summary>
    /// Path to draw in gizmos for debugging
    /// </summary>
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

        /// Create agents
        InitAgents();
    }

    public void Update()
    {
        if (m_currentTruckConnections.Count > 0)
        {
            /// For each truck and it's current connection
            foreach (KeyValuePair<ACOTruck, IConnection> thisKvp in m_currentTruckConnections)
            {
                /// Compare waypoint names to check if they are the same
                KeyValuePair<ACOTruck, IConnection> matchingKvp = m_currentTruckConnections
                    .FirstOrDefault(kvp => kvp.Value.ToNode.name == thisKvp.Value.ToNode.name && kvp.Key.name != thisKvp.Key.name);
                ACOTruck truckOne = thisKvp.Key;
                ACOTruck truckTwo = matchingKvp.Key;

                /// If current iterarte truck is waiting, check if they can resume
                /// Only check truck one as the others will be checked next iteration
                if (truckOne.IsWaiting)
                {
                    List<ACOTruck> waitingTrucks = new List<ACOTruck>();
                    bool isOnSameConnection = false;
                    foreach (KeyValuePair<ACOTruck, IConnection> checkKvp in m_currentTruckConnections)
                    {
                        if (!checkKvp.Key.IsWaiting)
                            continue;

                        if (thisKvp.Value.ToNode.name == checkKvp.Value.ToNode.name)
                        {
                            //isOnSameConnection = true;
                            waitingTrucks.Add(checkKvp.Key);
                        }
                    }

                    // More than 1 truck waiting at same connection, resume first one in list
                    if (waitingTrucks.Count > 1)
                    {
                        waitingTrucks[0].ResumeMovement();
                        Debug.Log($"Resuming Truck '{waitingTrucks[0]}' in queue of '{waitingTrucks.Count}'");
                    }
                    // else if truckOne has no others waiting, resume
                    else if (!isOnSameConnection)
                    {
                        truckOne.ResumeMovement();
                        Debug.Log($"Resuming Truck '{truckOne.name}'");
                    }
                }

                // Check match isn't null
                if (matchingKvp.Key != null && matchingKvp.Value != null)
                {
                    /// If isn't waiting and connections are matching...
                    bool eitherTruckWaiting = truckOne.IsWaiting || truckTwo.IsWaiting;
                    if (!eitherTruckWaiting && thisKvp.Value.ToNode.name == matchingKvp.Value.ToNode.name)
                    {
                        if (truckOne.Cargo.PackageCount > truckTwo.Cargo.PackageCount)
                        {
                            ///truckOne is slower, pause it
                            truckOne.PauseMovement();
                            Debug.Log($"Pausing Truck '{truckOne.name};");
                        }
                        else
                        {
                            /// truckTwo is slower
                            /// or cargo is same so prioritise truckTwo
                            truckTwo.PauseMovement();
                            Debug.Log($"Pausing Truck '{truckTwo.name}'");
                        }
                    }
                }
            }
        }
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

    private void InitAgents()
    {
        /// Iterate over each SquirrelInfo and configure
        foreach(AgentInfo truckInfo in AgentsInfo)
        {
            if (truckInfo.AgentPrefab == null || truckInfo.Start == null)
            {
                Debug.LogError($"SquirrelInfo is incorrectly configured!");
                continue;
            }

            /// instantiate the squirrel and add to list
            GameObject inst = Instantiate(truckInfo.AgentPrefab, m_agentParent);
            m_instantiatedAgents.Add(inst);

            /// Get the Aware component and listen to events and set move path
            ACOTruck acoTruck = inst.GetComponent<ACOTruck>();
            if (acoTruck)
            {
                acoTruck.Cargo.AddPackages(truckInfo.CargoAmount);

                /// Create ACOConnection list between each goal location
                List<ACOConnection> goalACOConnections = CalculateGoalsAndRoutes(truckInfo.Goals);

                /// Get the closest ACO goal node to use as start
                GameObject acoPathFirstNode = GetClosestACOGoalNodeToPosition(goalACOConnections, truckInfo.Start.transform.position);

                // Calculate ACO path from all waypoints, using the ACOConnections generated
                List<ACOConnection> route = this.GenerateACOPath(AntColonyConfig.MaximumIterations, 
                                                                    AntColonyConfig.Ants, 
                                                                    m_allWaypoints.ToArray(), 
                                                                    goalACOConnections,
                                                                    acoPathFirstNode,
                                                                    AntColonyConfig.MaxPathLength);

                /// Get last ACO node to calculate A* path back to target start position
                GameObject acoPathLastNode = route.LastOrDefault().ToNode;

                /// A* calculate paths from start to ACO start, and from ACO end to start
                List<Connection> startToACOStart = this.NavigateAStar(truckInfo.Start, acoPathFirstNode);
                List<Connection> acoEndToStart = this.NavigateAStar(acoPathLastNode, truckInfo.Start);

                /// Set this agent to move along ACO path
                acoTruck.SetMovePath(startToACOStart, route, acoEndToStart);

                acoTruck.OnTravelNewConnection += OnTruckTravelNewConnection;
                acoTruck.OnReachedGoal += OnAgentReachedACOGoal;

                /// Set Gizmo path for highlighting in Editor
                m_gizmoPath = goalACOConnections;
            }
            else
            {
                Debug.LogError($"Missing ACOTruck script on Prefab '{truckInfo.AgentPrefab.name}'!");
            }
        }
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
                    List<Connection> aStarRoute = this.NavigateAStar(acoConnection.FromNode, acoConnection.ToNode);
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
    /// Finds the closest ACO goal node to the agent's location
    /// </summary>
    /// <param name="acoGoalConnections">List of ACO goal connections </param>
    /// <param name="agentPosition">The world position</param>
    /// <returns></returns>
    private GameObject GetClosestACOGoalNodeToPosition(List<ACOConnection> acoGoalConnections, Vector3 position)
    {
        GameObject closest = null;
        float lastDist = float.MaxValue;
        foreach(ACOConnection conn in acoGoalConnections)
        {
            float dist = Vector3.Distance(conn.FromNode.transform.position, position);
            if (dist < lastDist)
            {
                closest = conn.FromNode;
                lastDist = dist;
            }
        }

        return closest;
    }

    private void OnTruckTravelNewConnection(ACOTruck truck, IConnection nextTravelConnection)
    {
        // Update or add new connection to list
        if (m_currentTruckConnections.ContainsKey(truck))
        {
            m_currentTruckConnections[truck] = nextTravelConnection;
        }
        else
        {
            m_currentTruckConnections.Add(truck, nextTravelConnection);
        }
    }

    private void OnAgentReachedACOGoal(ACOTruck agent, GameObject arg2)
    {
        agent.DeliverPackage();
    }
}
