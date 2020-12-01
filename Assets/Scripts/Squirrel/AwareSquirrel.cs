﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aware squirrel that will move along a path and be aware of other squirrels in the scene
/// </summary>
public class AwareSquirrel : MonoBehaviour
{
    /// <summary>
    /// Maximum drive speed of the squirrel
    /// </summary>
    public float MaxSpeed = 5.0f;
    /// <summary>
    /// Current cargo of the squirrel
    /// </summary>
    public Cargo Cargo = new Cargo();

    /// <summary>
    /// Is the squirrel waiting for another to pass?
    /// </summary>
    public bool IsWaiting = false;

    /// <summary>
    /// Event for when the aware agent truck finished travelling a connection and starts towards another
    /// </summary>
    public event Action<AwareSquirrel, ACOConnection> OnTravelNewConnection;
    /// <summary>
    /// Event for when the aware reached it's end destination.
    /// Params:
    /// This AwareAgent that arrives at end waypoint
    /// GameObject of destination waypoint
    /// </summary>
    public event Action<AwareSquirrel, GameObject> OnReachedPathEnd;

    /// <summary>
    /// Current path for the squirrel to move along
    /// </summary>
    private List<ACOConnection> m_connectionPath = null;

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
        /// Add UI controller to the Squirrel
        m_ui = this.gameObject.AddComponent<CargoUIController>();
        m_ui.SetUIScale(0.5f);
    }

    void Start()
    {
        /// Set initial cargo amount
        m_ui.SetCargoAmount(Cargo.PackageCount);
    }

    void Update()
    {
        if (m_currentTargetNode)
        {
            // Do target node look at if target is valid
            PerformLookAt(m_currentTargetNode.transform);

            if (!IsWaiting)
            {
                // Move Truck towards next connection
                PerformMovement();

                // If nearly reached destination
                float nextNodeDistance = Vector3.Distance(transform.position, m_currentTargetNode.transform.position);
                if (nextNodeDistance < DESTINATION_TOLERANCE)
                {
                    // Increment and set new target game object
                    m_currentTargetNodeIndex++;

                    // Move target node to next node index
                    if (m_currentTargetNodeIndex > 0 && m_currentTargetNodeIndex < m_connectionPath.Count)
                    {
                        ACOConnection currentTargetConnection = m_connectionPath[m_currentTargetNodeIndex];
                        m_currentTargetNode = currentTargetConnection.ToNode;

                        OnTravelNewConnection?.Invoke(this, currentTargetConnection);
                    }
                    else
                    {
                        //Debug.LogError("TargetNodeIndex is out of bounds!");
                    }
                }

                // Check if reached final node yet
                float finalNodeDistance = Vector3.Distance(transform.position, m_connectionPath[m_connectionPath.Count - 1].ToNode.transform.position);
                if (finalNodeDistance < DESTINATION_TOLERANCE && m_currentTargetNodeIndex >= m_connectionPath.Count)
                {
                    // Invoke event for reached path end and remove target
                    ACOConnection finalConnection = m_connectionPath[m_connectionPath.Count - 1];
                    OnReachedPathEnd?.Invoke(this, finalConnection.ToNode);

                    m_ui.SetStatusText($"Finished path to '{finalConnection.ToNode.name}'");

                    ResetPath();
                }
            }
        }
    }
    #endregion

    /// <summary>
    /// Drives the  along a certain connection path
    /// </summary>
    /// <param name="connectionPath">List of connections to drive along</param>
    public void SetMovePath(List<ACOConnection> acoConnections)
    {
        if (acoConnections == null || acoConnections != null && acoConnections.Count <= 0)
        {
            Debug.LogError("Unable to drive along connection path. acoConnections invalid");
            return;
        }

        m_connectionPath = acoConnections;
        Debug.Log($"Squirrel '{this.name}' path set! '{m_connectionPath.Count}' connections");

        if (m_currentTargetNode == null)
        {
            // Set Truck position from first FromNode and target to ToNode
            this.transform.position = m_connectionPath[m_currentTargetNodeIndex].FromNode.transform.position;
            m_currentTargetNode = m_connectionPath[m_currentTargetNodeIndex].ToNode;

            m_ui.SetStatusText($"New Path: Moving to '{m_connectionPath[m_connectionPath.Count - 1].ToNode.name}'");
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
            m_ui.SetStatusText($"Waiting at '{m_connectionPath[m_currentTargetNodeIndex].FromNode.name}'");
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
            m_ui.SetStatusText($"Resuming to '{m_connectionPath[m_connectionPath.Count - 1].ToNode.name}'");
        }

        IsWaiting = false;
    }

    /// <summary>
    /// Resets the truck's current drive path
    /// </summary>
    private void ResetPath()
    {
        m_connectionPath = null;
        m_currentTargetNode = null;
        m_currentTargetNodeIndex = 0;
    }
}
