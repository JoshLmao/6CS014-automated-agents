using System.Collections.Generic;
using UnityEngine;

public class Truck : MonoBehaviour
{
    /// <summary>
    /// Maximum drive speed of the truck
    /// </summary>
    public float Speed = 1.0f;

    private List<Connection> m_connectionDrivePath = null;

    /// <summary>
    /// Vector offset of the truck when moving along the coordinates of the connection
    /// </summary>
    //[SerializeField]
    //private Vector3 ModelOffset = new Vector3(0, 1f, 0);
    [SerializeField]
    private Vector3 ModelRotationOffset = Vector3.zero;

    private GameObject m_currentTargetNode = null;
    private int m_currentTargetNodeIndex = 0;
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

        // If nearly reached destination
        if (Vector3.Distance(transform.position, m_currentTargetNode.transform.position) < DESTINATION_TOLERANCE)
        {
            // Check if index has reached destination to make truck drive home
            if (m_currentTargetNodeIndex >= m_connectionDrivePath.Count)
            {
                m_isReturningHome = true;
            }
            else if (m_currentTargetNodeIndex <= 0)
            {
                m_isReturningHome = false;
            }

            // Check if target index is before last
            if (!m_isReturningHome)
            {
                // Increment and set new target game object
                m_currentTargetNodeIndex++;
            }
            else if (m_isReturningHome)
            {
                // Decrement and go back through path
                m_currentTargetNodeIndex--;
            }

            if (m_currentTargetNodeIndex > 0 && m_currentTargetNodeIndex < m_connectionDrivePath.Count)
            {
                m_currentTargetNode = m_connectionDrivePath[m_currentTargetNodeIndex].ToNode;
            }
            else
            {
                Debug.LogError("TargetNodeIndex is out of bounds!");
            }
        }
    }
}
