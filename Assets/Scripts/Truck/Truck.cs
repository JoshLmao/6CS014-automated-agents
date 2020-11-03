﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class Truck : MonoBehaviour
{
    /// <summary>
    /// Maximum drive speed of the truck
    /// </summary>
    public float Speed = 1.0f;

    /// <summary>
    /// Event for when truck reaches it's start destination after returning
    /// </summary>
    public event Action OnReachedPathStart;
    /// <summary>
    /// Event for when truck reached it's end destination
    /// </summary>
    public event Action OnReachedPathEnd;

    /// <summary>
    /// Current path for the truck to drive along
    /// </summary>
    private List<Connection> m_connectionDrivePath = null;

    /// <summary>
    /// Vector offset of the truck when moving along the coordinates of the connection
    /// </summary>
    //[SerializeField]
    //private Vector3 ModelOffset = new Vector3(0, 1f, 0);
    [SerializeField]
    private Vector3 ModelRotationOffset = Vector3.zero;

    /// <summary>
    /// Current target node to move toward
    /// </summary>
    private GameObject m_currentTargetNode = null;
    /// <summary>
    /// Current target node index in the connection drive path list
    /// </summary>
    private int m_currentTargetNodeIndex = 0;
    /// <summary>
    /// Is the truck currently returning to it's home destination
    /// </summary>
    private bool m_isReturningHome = false;

    /// <summary>
    /// Amount of distance to target to consider the truck to be at it's destination
    /// </summary>
    private const float DESTINATION_TOLERANCE = 0.001f;
    
    #region MonoBehaviours
    void Start()
    {
        
    }

    void Update()
    {
        if (m_currentTargetNode)
        {
            DoMovement();

            // If nearly reached destination
            float nextNodeDistance = Vector3.Distance(transform.position, m_currentTargetNode.transform.position);
            if (nextNodeDistance < DESTINATION_TOLERANCE)
            {
                // Increment and set new target game object
                m_currentTargetNodeIndex++;

                // Move target node to next node index
                if (m_currentTargetNodeIndex > 0 && m_currentTargetNodeIndex < m_connectionDrivePath.Count)
                {
                    m_currentTargetNode = m_connectionDrivePath[m_currentTargetNodeIndex].ToNode;
                }
                else
                {
                    Debug.LogError("TargetNodeIndex is out of bounds!");
                }
            }
            
            float finalNodeDistance = Vector3.Distance(transform.position, m_connectionDrivePath[m_connectionDrivePath.Count - 1].ToNode.transform.position);
            if (finalNodeDistance < DESTINATION_TOLERANCE)
            {
                // Invoke event for reached path end and remove target
                OnReachedPathEnd?.Invoke();
                ResetPath();

                // Invoke event for reaching path start and remove target
                //OnReachedPathStart?.Invoke();
                //ResetPath();
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
        if (connectionPath == null|| connectionPath != null && connectionPath.Count <= 0)
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

    private void DoMovement()
    {
        // Calculate distance this step and move object
        float step = Speed * Time.deltaTime;

        // Move towards destination with offset
        Vector3 stepVector = Vector3.MoveTowards(this.transform.position, m_currentTargetNode.transform.position, step);
        this.transform.position = stepVector;

        // Set rotation to look at next connection
        this.transform.LookAt(m_currentTargetNode.transform.position);
        transform.eulerAngles = transform.eulerAngles - ModelRotationOffset;
    }

    protected void SetIsReturningHome(bool isReturningHome)
    {
        m_isReturningHome = isReturningHome;
    }

    public void DeliverPackage()
    {
        Debug.Log($"Delivered package at '{m_currentTargetNode.name}'");
    }

    private void ResetPath()
    {
        m_currentTargetNode = null;
        m_currentTargetNodeIndex = 0;
        m_connectionDrivePath = null;
    }
}
