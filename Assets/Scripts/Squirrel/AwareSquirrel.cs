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

    #region MonoBehaviours
    private void Awake()
    {
        m_ui = this.gameObject.AddComponent<CargoUIController>();
    }

    void Start()
    {
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
            Debug.LogError("Unable to drive along connection path. connectionPath invalid");
            return;
        }

        m_connectionPath = acoConnections;
        Debug.Log($"Squirrel '{this.name}' path set! '{m_connectionPath.Count}' connections");

        if (m_currentTargetNode == null)
        {
            // Set Truck position from first FromNode and target to ToNode
            this.transform.position = m_connectionPath[m_currentTargetNodeIndex].FromNode.transform.position;
            m_currentTargetNode = m_connectionPath[m_currentTargetNodeIndex].ToNode;
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
    /// Pauses truck movement
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
    /// Resumes truck movement
    /// </summary>
    public void ResumeMovement()
    {
        if (IsWaiting)
        {
            m_ui.SetStatusText($"Resuming to '{m_connectionPath[m_connectionPath.Count - 1].ToNode.name}'");
        }

        IsWaiting = false;
    }
}
