using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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

    private List<Connection> m_startToACOPath = null;
    private NavigateToInfo m_startToACONavInfo = null;

    private List<Connection> m_acoToStartPath = null;
    private NavigateToInfo m_acoToStartNavInfo = null;

    /// <summary>
    /// Current path for the Agent to move along
    /// </summary>
    private List<ACOConnection> m_acoConnectionPath = null;

    private ACOConnection m_currentTargetACOConn = null;
    private int m_currentTargetACOConnIndex = 0;
    private int m_currentACOConnRouteIndex = 0;

    private NavigationTarget m_currentDrivePathTarget = NavigationTarget.StartToACO;

    /// <summary>
    /// Amount of rotation to apply to model when setting look at rotation
    /// </summary>
    [SerializeField]
    private Vector3 ModelRotationOffset = Vector3.zero;

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
                    Debug.Log("Completed A* Navigation. Proceeding with ACO path...");
                }
                else
                {
                    /// Set next to node if inside range
                    m_startToACONavInfo.TargetNode = m_startToACOPath[m_startToACONavInfo.TargetIndex].ToNode;
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
                    if (m_currentACOConnRouteIndex >= m_currentTargetACOConn.Route.Count)
                    {
                        m_currentACOConnRouteIndex = 0;

                        m_currentTargetACOConnIndex++;
                        /// Check if reached end of ACOConnection path
                        if (m_currentTargetACOConnIndex >= m_acoConnectionPath.Count)
                        {
                            ACOConnection finalConnection = m_acoConnectionPath[m_acoConnectionPath.Count - 1];
                            OnReachedPathEnd?.Invoke(this, finalConnection.ToNode);

                            m_ui.SetStatusText($"Finished path to '{finalConnection.ToNode.name}'");

                            m_currentDrivePathTarget = NavigationTarget.ACOToStart;
                            Debug.Log("Finished ACO path. Navigating A* path to Start");

                            return;
                        }
                        else
                        {
                            /// Continue moving through ACOConnection path if not at end
                            m_currentTargetACOConn = m_acoConnectionPath[m_currentTargetACOConnIndex];

                            OnTravelNewConnection?.Invoke(this, m_currentTargetACOConn);
                            return;
                        }
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
                    m_currentDrivePathTarget = NavigationTarget.None;
                    /// Set drive path to ACO, the next path
                    Debug.Log("Completed A* navigation from ACO to Start");
                }
                else
                {
                    /// Set next to node if inside range
                    m_acoToStartNavInfo.TargetNode = m_acoToStartPath[m_acoToStartNavInfo.TargetIndex].ToNode;
                }
            }
        }
    }

    private void NavigateGeneric(GameObject targetNode)
    {
        PerformLookAt(targetNode.transform);

        if (!IsWaiting)
        {
            PerformMovementTo(targetNode.transform.position);
        }
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

        Debug.Log($"Agent '{this.name}' path set!");

        m_currentDrivePathTarget = NavigationTarget.StartToACO;

        /// Set movement vars to default values
        m_currentTargetACOConnIndex = 0;
        m_currentACOConnRouteIndex = 0;
        m_currentTargetACOConn = m_acoConnectionPath[m_currentTargetACOConnIndex];

        /// Set NavigateInfo for each A* path
        m_startToACONavInfo = new NavigateToInfo
        {
            TargetNode = m_startToACOPath.FirstOrDefault().FromNode,
            TargetIndex = 0
        };
        m_acoToStartNavInfo = new NavigateToInfo
        {
            TargetNode = m_acoToStartPath.FirstOrDefault().ToNode,
            TargetIndex = 0,
        };

        /// Set start position for agent
        this.transform.position = startToACOAStarPath.FirstOrDefault().FromNode.transform.position;

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
    /// Pauses movement
    /// </summary>
    public void PauseMovement()
    {
        if (!IsWaiting)
        {
            m_ui.SetStatusText($"Waiting at '{m_acoConnectionPath[m_currentTargetACOConnIndex].FromNode.name}'");
        }

        IsWaiting = true;
    }

    /// <summary>
    /// Resumes movement
    /// </summary>
    public void ResumeMovement()
    {
        if (IsWaiting)
        {
            m_ui.SetStatusText($"Resuming to '{m_acoConnectionPath[m_acoConnectionPath.Count - 1].ToNode.name}'");
        }

        IsWaiting = false;
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
    }

}
