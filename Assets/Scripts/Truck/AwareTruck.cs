using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AStar Pathfinding Truck that is aware of it's surroundings, giving way to 
/// faster AwareTrucks if necessary, that can carry cargo
/// </summary>
public class AwareTruck : MonoBehaviour
{
    /// <summary>
    /// Maximum drive speed of the truck
    /// </summary>
    public float MaxSpeed = 5.0f;
    /// <summary>
    /// Current cargo of ther Truck
    /// </summary>
    public Cargo Cargo = new Cargo();

    public event Action<AwareTruck, Connection> OnTravelNewConnection;
    /// <summary>
    /// Event for when truck reached it's end destination.
    /// Params:
    /// This truck that arrives at end waypoint
    /// GameObject of destination waypoint
    /// </summary>
    public event Action<AwareTruck, GameObject> OnReachedPathEnd;

    /// <summary>
    /// Current path for the truck to drive along
    /// </summary>
    private List<Connection> m_connectionDrivePath = null;

    /// <summary>
    /// Current target node to move toward
    /// </summary>
    private GameObject m_currentTargetNode = null;
    /// <summary>
    /// Current target node index in the connection drive path list
    /// </summary>
    private int m_currentTargetNodeIndex = 0;

    /// <summary>
    /// Amount of rotation to apply to model when setting look at rotation
    /// </summary>
    [SerializeField]
    private Vector3 ModelRotationOffset = Vector3.zero;

    /// <summary>
    /// Amount of distance to target to consider the truck to be at it's destination
    /// </summary>
    private const float DESTINATION_TOLERANCE = 0.001f;

    #region MonoBehaviours
    void Update()
    {
        if (m_currentTargetNode)
        {
            PerformMovement();

            // If nearly reached destination
            float nextNodeDistance = Vector3.Distance(transform.position, m_currentTargetNode.transform.position);
            if (nextNodeDistance < DESTINATION_TOLERANCE)
            {
                // Increment and set new target game object
                m_currentTargetNodeIndex++;

                // Move target node to next node index
                if (m_currentTargetNodeIndex > 0 && m_currentTargetNodeIndex < m_connectionDrivePath.Count)
                {
                    Connection currentTargetConnection = m_connectionDrivePath[m_currentTargetNodeIndex];
                    m_currentTargetNode = currentTargetConnection.ToNode;

                    OnTravelNewConnection?.Invoke(this, currentTargetConnection);
                }
                else
                {
                    //Debug.LogError("TargetNodeIndex is out of bounds!");
                }
            }

            // Check if reached final node yet
            float finalNodeDistance = Vector3.Distance(transform.position, m_connectionDrivePath[m_connectionDrivePath.Count - 1].ToNode.transform.position);
            if (finalNodeDistance < DESTINATION_TOLERANCE)
            {
                // Invoke event for reached path end and remove target
                OnReachedPathEnd?.Invoke(this, m_connectionDrivePath[m_connectionDrivePath.Count - 1].ToNode);
                ResetPath();
            }
        }
    }
    #endregion

    /// <summary>
    /// Drives the truck along a certain connection path
    /// </summary>
    /// <param name="connectionPath">List of connections to drive along</param>
    public void DriveAlong(List<Connection> connectionPath)
    {
        // Reset any previous paths
        m_connectionDrivePath = null;
        m_currentTargetNode = null;

        if (connectionPath == null || connectionPath != null && connectionPath.Count <= 0)
        {
            Debug.LogError("Unable to drive along connection path. connectionPath invalid");
            return;
        }

        m_connectionDrivePath = connectionPath;
        Debug.Log($"Truck '{this.name}' path set! '{m_connectionDrivePath.Count}' connections");

        if (m_currentTargetNode == null)
        {
            // Set Truck position from first FromNode and target to ToNode
            this.transform.position = m_connectionDrivePath[m_currentTargetNodeIndex].FromNode.transform.position;
            m_currentTargetNode = m_connectionDrivePath[m_currentTargetNodeIndex].ToNode;
        }
    }

    /// <summary>
    /// Update loop to perform the truck's movement to the next target node
    /// </summary>
    private void PerformMovement()
    {
        // Calculate speed reduction from Cargo
        float totalPackageSpeedReduction = CalculateCargoSpeedReduction();
        float currentCargoSpeed = MaxSpeed - totalPackageSpeedReduction;

        // Calculate step from the target speed
        float step = currentCargoSpeed * Time.deltaTime;

        // Move towards destination with offset
        Vector3 stepVector = Vector3.MoveTowards(this.transform.position, m_currentTargetNode.transform.position, step);
        this.transform.position = stepVector;

        // Set rotation to look at next connection
        this.transform.LookAt(m_currentTargetNode.transform.position);
        this.transform.eulerAngles = transform.eulerAngles - ModelRotationOffset;
    }

    /// <summary>
    /// Deliveres a package form the cargo
    /// </summary>
    public void DeliverPackage()
    {
        Debug.Log($"Truck '{this.gameObject.name}' delivered package at '{m_currentTargetNode.name}'");
        Cargo.RemovePackages(1);
    }

    /// <summary>
    /// Set the amount of packages in the Truck's cargo
    /// </summary>
    /// <param name="packageAmt"></param>
    public void SetPackages(int packageAmt)
    {
        Cargo.AddPackages(packageAmt);
    }

    /// <summary>
    /// Resets the truck's current drive path
    /// </summary>
    private void ResetPath()
    {
        m_connectionDrivePath = null;
        m_currentTargetNode = null;
        m_currentTargetNodeIndex = 0;
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

    public void Pause()
    {
        
    }
}
