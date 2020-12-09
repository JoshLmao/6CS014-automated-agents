using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

public class ACOTruck : MonoBehaviour
{
    private enum NavigationTarget { None, StartToACO, ACO, ACOToStart };

    private class NavigateToInfo
    {
        public GameObject TargetNode;
        public int TargetIndex;
    }

    /// <summary>
    /// Maximum drive speed
    /// </summary>
    public float MaxSpeed = 5.0f;
    /// <summary>
    /// Current cargo the agent is holding
    /// </summary>
    public Cargo Cargo = new Cargo();

    /// <summary>
    /// Is the Agent waiting for another to pass?
    /// </summary>
    public bool IsWaiting = false;
    /// <summary>
    /// Is the agent delivering a parcel?
    /// </summary>
    public bool IsDelivering = false;

    /// <summary>
    /// Event for when the aware agent truck finished travelling a connection and starts towards another
    /// </summary>
    public event Action<ACOTruck, IConnection> OnTravelNewConnection;
    /// <summary>
    /// Event for when the aware reached it's end destination.
    /// Params:
    /// This AwareAgent that arrives at end waypoint
    /// GameObject of destination waypoint
    /// </summary>
    public event Action<ACOTruck, GameObject> OnReachedPathEnd;
    /// <summary>
    /// Event for when agent reaches an ACO goal
    /// </summary>
    public event Action<ACOTruck, GameObject> OnReachedGoal;

    /// <summary>
    /// A* path from Agent Start to ACO start
    /// </summary>
    private List<Connection> m_startToACOPath = null;
    /// <summary>
    /// Navigation Info for moving along the startToACOPath A* path
    /// </summary>
    private NavigateToInfo m_startToACONavInfo = null;

    /// <summary>
    /// A* path from the end of ACO to the agent start
    /// </summary>
    private List<Connection> m_acoToStartPath = null;
    /// <summary>
    /// Navigation Info for moving along the acoToStartPath A* path
    /// </summary>
    private NavigateToInfo m_acoToStartNavInfo = null;

    /// <summary>
    /// Current path for the Agent to move along
    /// </summary>
    private List<ACOConnection> m_acoConnectionPath = null;

    /// <summary>
    /// Current ACOConnection to navigate along during ACO path stage
    /// </summary>
    private ACOConnection m_currentTargetACOConn = null;
    /// <summary>
    /// Current ACOConnection index to navigate along
    /// </summary>
    private int m_currentTargetACOConnIndex = 0;
    /// <summary>
    /// Current ACOConnection Route index to navigate to
    /// </summary>
    private int m_currentACOConnRouteIndex = 0;

    /// <summary>
    /// Current stage of movement for agent
    /// </summary>
    private NavigationTarget m_currentDrivePathTarget = NavigationTarget.None;

    /// <summary>
    /// Amount of rotation to apply to model when setting look at rotation
    /// </summary>
    [SerializeField]
    private Vector3 ModelRotationOffset = Vector3.zero;

    private float m_totalDuration = 0f;

    /// <summary>
    /// Controller for UI to display how much is carrying in Cargo
    /// </summary>
    private CargoUIController m_ui = null;

    /// <summary>
    /// Amount of distance to target to consider the truck to be at it's destination
    /// </summary>
    private const float DESTINATION_TOLERANCE = 0.001f;

    #region MonoBehaviours
    private void Awake()
    {
        /// Add UI controller to the Agent
        m_ui = this.gameObject.AddComponent<CargoUIController>();
    }

    void Start()
    {
        /// Set initial cargo amount
        m_ui.SetCargoAmount(Cargo.PackageCount);
    }

    void Update()
    {
        /// Navigate on the correct path depending on the current path target
        switch(m_currentDrivePathTarget)
        {
            case NavigationTarget.StartToACO:
                NavigateStartToACO();
                break;
            case NavigationTarget.ACO:
                NavigateACO();
                break;
            case NavigationTarget.ACOToStart:
                NavigateACOToStart();
                break;
            default:
                break;
        }

        if (m_currentDrivePathTarget != NavigationTarget.None)
        {
            m_totalDuration += Time.deltaTime;
        }
    }

    /// <summary>
    /// Only draw path gizmos if truck is selected
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Vector3 offset = new Vector3(0, 0.3f, 0);

        /// Draw Start StartToACO path
        if (m_startToACOPath != null && m_startToACOPath.Count > 0)
        {
            foreach (Connection aConn in m_startToACOPath)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(aConn.FromNode.transform.position + offset, aConn.ToNode.transform.position + offset);
            }
        }

        /// Draw last ACOToStart path
        if (m_acoToStartPath != null && m_acoToStartPath.Count > 0)
        {
            foreach (Connection conn in m_acoToStartPath)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(conn.FromNode.transform.position + offset, conn.ToNode.transform.position + offset);
            }
        }

        /// Draw ACO direct line
        if (m_acoConnectionPath != null && m_acoConnectionPath.Count > 0)
        {
            foreach (ACOConnection acoConn in m_acoConnectionPath)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(acoConn.FromNode.transform.position + offset, acoConn.ToNode.transform.position);

                /// Draw A* route between each ACOConnection
                foreach (Connection routeConn in acoConn.Route)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(routeConn.FromNode.transform.position + offset, routeConn.ToNode.transform.position + offset);
                }
            }
        }
    }
    #endregion

    private void NavigateStartToACO()
    {
        if (m_startToACOPath != null && m_startToACOPath.Count > 0 && m_startToACONavInfo != null)
        {
            NavigateGeneric(m_startToACONavInfo.TargetNode);

            /// Check if agent reached next route node
            float nextNodeDistance = Vector3.Distance(transform.position, m_startToACONavInfo.TargetNode.transform.position);
            if (nextNodeDistance < DESTINATION_TOLERANCE)
            {
                /// Increment target index
                m_startToACONavInfo.TargetIndex += 1;
                
                /// Check if index is more or equal to path length
                if (m_startToACONavInfo.TargetIndex >= m_startToACOPath.Count)
                {
                    /// Set drive path to ACO, the next path
                    m_currentDrivePathTarget = NavigationTarget.ACO;
                    Debug.Log($"Agent '{this.gameObject.name}' completed A* Navigation. Proceeding with ACO path...");
                }
                else
                {
                    /// Set next to node if inside range
                    Connection nextConn = m_startToACOPath[m_startToACONavInfo.TargetIndex];
                    OnTravelNewConnection?.Invoke(this, nextConn);
                    
                    m_startToACONavInfo.TargetNode = nextConn.ToNode;
                }
            }
        }
    }

    private void NavigateACO()
    {
        if (m_currentTargetACOConn != null)
        {
            /// Look at next target node
            GameObject targetNodeObj = m_currentTargetACOConn.Route[m_currentACOConnRouteIndex].ToNode;
            PerformLookAt(targetNodeObj.transform);

            if (!IsWaiting)
            {
                /// if not waiting, move toward next route node
                PerformMovementTo(targetNodeObj.transform.position);

                /// Check if agent reached next route node
                float nextNodeDistance = Vector3.Distance(transform.position, targetNodeObj.transform.position);
                if (nextNodeDistance < DESTINATION_TOLERANCE)
                {
                    /// Reached next route node, increment to next route node or to new ACOConnection
                    m_currentACOConnRouteIndex++;

                    /// Check RouteIndex is within route bounds
                    if (m_currentACOConnRouteIndex >= m_currentTargetACOConn.Route.Count)
                    {
                        /// If index is more than route, we've reached end and can move to next ACOConnection
                        m_currentACOConnRouteIndex = 0;

                        m_currentTargetACOConnIndex++;
                        /// Check if reached end of ACOConnection path
                        if (m_currentTargetACOConnIndex >= m_acoConnectionPath.Count)
                        {
                            ACOConnection finalConnection = m_acoConnectionPath[m_acoConnectionPath.Count - 1];
                            OnReachedPathEnd?.Invoke(this, finalConnection.ToNode);

                            m_ui.SetStatusText($"Finished path to '{finalConnection.ToNode.name}'");

                            m_currentDrivePathTarget = NavigationTarget.ACOToStart;
                            Debug.Log($"Agent '{this.name}' finished ACO path. Navigating A* path to Start");

                            return;
                        }
                        else
                        {
                            /// Move to next node in Route
                            //Debug.Log($"Reached ACO goal {m_currentTargetACOConn.ToNode.name}");

                            OnReachedGoal?.Invoke(this, m_currentTargetACOConn.ToNode);

                            /// Continue moving through ACOConnection path if not at end
                            m_currentTargetACOConn = m_acoConnectionPath[m_currentTargetACOConnIndex];

                            OnTravelNewConnection?.Invoke(this, m_currentTargetACOConn);
                            return;
                        }
                    }
                    else
                    {
                        /// Still more Route nodes to travel to, invoke event to specify the connection
                        Connection nextRouteConnection = m_currentTargetACOConn.Route[m_currentACOConnRouteIndex];
                        OnTravelNewConnection?.Invoke(this, nextRouteConnection);
                    }
                }
            }
        }
    }

    private void NavigateACOToStart()
    {
        if (m_acoToStartPath != null && m_acoToStartPath.Count > 0 && m_acoToStartNavInfo != null)
        {
            NavigateGeneric(m_acoToStartNavInfo.TargetNode);

            /// Check if agent reached next route node
            float nextNodeDistance = Vector3.Distance(transform.position, m_acoToStartNavInfo.TargetNode.transform.position);
            if (nextNodeDistance < DESTINATION_TOLERANCE)
            {
                /// Increment target index
                m_acoToStartNavInfo.TargetIndex += 1;

                /// Check if index is more or equal to path length
                if (m_acoToStartNavInfo.TargetIndex >= m_acoToStartPath.Count)
                {
                    /// Finished all paths, reset and sleep
                    m_currentDrivePathTarget = NavigationTarget.None;

                    m_ui.SetStatusText("Finished and returned home. Sleeping (zzz)");
                    Debug.Log($"Agent '{this.gameObject.name}' finished ACO path, duration of '{m_totalDuration}s'");

                    ResetPath();
                }
                else
                {
                    /// Set next to node if inside range
                    Connection nextConn = m_acoToStartPath[m_acoToStartNavInfo.TargetIndex];
                    OnTravelNewConnection?.Invoke(this, nextConn);

                    m_acoToStartNavInfo.TargetNode = nextConn.ToNode;
                }
            }
        }
    }

    /// <summary>
    /// Generic function for navigating agent to a target node. Sets the direction and position
    /// </summary>
    /// <param name="targetNode">Target waypoint/node to move toward</param>
    private void NavigateGeneric(GameObject targetNode)
    {
        PerformLookAt(targetNode.transform);

        /// Check if agent is waiting or delivering, dont continue if so
        if (IsDelivering || IsWaiting)
            return;

        PerformMovementTo(targetNode.transform.position);
    }

    /// <summary>
    /// Drives the  along a certain connection path
    /// </summary>
    /// <param name="startToACOAStarPath">A* path from the agent start to the ACO path</param>
    /// <param name="acoConnections">The ACO connections path that goal around all goals</param>
    /// <param name="acoToStartAStarPath">A* path from the final ACO node back to the agent's start path</param>
    public void SetMovePath(List<Connection> startToACOAStarPath, List<ACOConnection> acoConnections, List<Connection> acoToStartAStarPath)
    {
        if (acoConnections == null || acoConnections != null && acoConnections.Count <= 0)
        {
            Debug.LogError("Unable to drive along connection path. acoConnections invalid");
            return;
        }

        /// Set navigation paths for this agent. Contains three paths:
        /// Agent start to the initial ACO start node
        /// The Total ACO path
        /// Final ACO node to the initial Agent start node
        m_acoConnectionPath = acoConnections;
        m_startToACOPath = startToACOAStarPath;
        m_acoToStartPath = acoToStartAStarPath;

        /// Set Move states to go from Start to the ACO start
        m_currentDrivePathTarget = NavigationTarget.StartToACO;

        /// Set movement vars to default values
        m_currentTargetACOConnIndex = 0;
        m_currentACOConnRouteIndex = 0;
        m_currentTargetACOConn = m_acoConnectionPath[m_currentTargetACOConnIndex];

        /// Set NavigateInfo for each A* path
        m_startToACONavInfo = new NavigateToInfo
        {
            TargetNode = m_startToACOPath.FirstOrDefault().ToNode,
            TargetIndex = 0
        };
        m_acoToStartNavInfo = new NavigateToInfo
        {
            TargetNode = m_acoToStartPath.FirstOrDefault().ToNode,
            TargetIndex = 0,
        };

        /// Set start position for agent
        this.transform.position = startToACOAStarPath.FirstOrDefault().FromNode.transform.position;

        Debug.Log($"Agent '{this.name}' path set! Start to ACO '{m_startToACOPath.Count}' Waypoints, ACO Total '{m_acoConnectionPath.Count}' waypoints, ACO to Start '{m_acoToStartPath.Count}' waypoints");
        m_ui.SetStatusText($"New Path: Moving to '{m_acoConnectionPath[m_acoConnectionPath.Count - 1].ToNode.name}'");
    }

    /// <summary>
    /// Update loop to perform the truck's movement to the next target node
    /// </summary>
    private void PerformMovementTo(Vector3 targetPosition)
    {
        // Calculate speed reduction from Cargo
        float totalPackageSpeedReduction = CalculateCargoSpeedReduction();
        float currentCargoSpeed = MaxSpeed - totalPackageSpeedReduction;

        // Calculate step from the target speed
        float step = currentCargoSpeed * Time.deltaTime;

        // Move towards destination with offset
        Vector3 stepVector = Vector3.MoveTowards(this.transform.position, targetPosition, step);
        this.transform.position = stepVector;
    }

    /// <summary>
    /// Performes the look at rotation for the truck to look at the target node
    /// </summary>
    private void PerformLookAt(Transform lookAtTransform)
    {
        // Set rotation to look at next connection
        this.transform.LookAt(lookAtTransform.position);
        this.transform.eulerAngles = this.transform.eulerAngles - ModelRotationOffset;
    }

    /// <summary>
    /// Determines the amount to reduce the total speed by of the truck by it's carrying cargo
    /// </summary>
    /// <returns>The amount of speed to remove from the MaxSpeed</returns>
    private float CalculateCargoSpeedReduction()
    {
        // Maximum percentage the maxSpeed will be reduced by 
        const float MAX_PERCENT_REDUCE = 0.9f;

        // Reduce the speed by maximum 90% per package in cargo
        float nintyPercentSpeed = MaxSpeed * MAX_PERCENT_REDUCE;
        float valueReducePer = nintyPercentSpeed / Cargo.MAX_PACKAGES;
        float totalPackageSpeedReduction = valueReducePer * Cargo.PackageCount;

        return totalPackageSpeedReduction;
    }

    /// <summary>
    /// Pauses movement of the agent
    /// </summary>
    public void PauseMovement()
    {
        if (!IsWaiting)
        {
            string nodeName = "Unknown Node";
            switch (m_currentDrivePathTarget)
            {
                case NavigationTarget.StartToACO:
                    nodeName = m_startToACOPath[m_startToACONavInfo.TargetIndex].FromNode.name;
                    break;
                case NavigationTarget.ACO:
                    nodeName = m_acoConnectionPath[m_currentTargetACOConnIndex].FromNode.name;
                    break;
                case NavigationTarget.ACOToStart:
                    nodeName = m_acoToStartPath[m_acoToStartNavInfo.TargetIndex].FromNode.name;
                    break;
            }
            m_ui.SetStatusText($"Waiting at '{nodeName}'");
        }

        IsWaiting = true;
    }

    /// <summary>
    /// Resumes movement of the agent
    /// </summary>
    public void ResumeMovement()
    {
        if (IsWaiting)
        {
            string nodeName = "Unknown Node";
            switch (m_currentDrivePathTarget)
            {
                case NavigationTarget.StartToACO:
                    nodeName = m_startToACOPath[m_startToACONavInfo.TargetIndex].ToNode.name;
                    break;
                case NavigationTarget.ACO:
                    nodeName = m_acoConnectionPath[m_currentTargetACOConnIndex].ToNode.name;
                    break;
                case NavigationTarget.ACOToStart:
                    nodeName = m_acoToStartPath[m_acoToStartNavInfo.TargetIndex].ToNode.name;
                    break;
            }
            m_ui.SetStatusText($"Resuming to '{nodeName}'");
        }

        IsWaiting = false;
    }

    /// <summary>
    /// Deliveres a package from the cargo after a wait
    /// </summary>
    public void DeliverPackage()
    {
        StartCoroutine(DeliveryWait(3f));
    }

    private IEnumerator DeliveryWait(float seconds)
    {
        IsDelivering = true;

        yield return new WaitForSeconds(seconds);

        Debug.Log($"Agent '{this.gameObject.name}' delivered package");
        Cargo.RemovePackages(1);

        m_ui.SetCargoAmount(Cargo.PackageCount);
        m_ui.SetStatusText("Delivered a parcel!");

        IsDelivering = false;
    }

    /// <summary>
    /// Resets the agent's current drive path
    /// </summary>
    private void ResetPath()
    {
        m_acoConnectionPath = null;
        m_currentTargetACOConn = null;
        m_currentTargetACOConnIndex = 0;
        m_currentACOConnRouteIndex = 0;
        m_startToACOPath = null;
        m_startToACONavInfo = null;
        m_acoToStartPath = null;
        m_acoToStartNavInfo = null;
        m_totalDuration = 0f;
    }
}
