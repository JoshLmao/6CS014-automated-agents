using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    /// <summary>
    /// Is the truck waiting for another to pass?
    /// </summary>
    public bool IsWaiting = false;

    /// <summary>
    /// Event for when the aware truck finished travelling a connection and starts towards another
    /// </summary>
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
    /// Text to display how many parcels the truck is carrying
    /// </summary>
    private Text m_parcelText = null;
    /// <summary>
    /// Text that displays the current status of the truck
    /// </summary>
    private Text m_statusText = null;

    /// <summary>
    /// Amount of distance to target to consider the truck to be at it's destination
    /// </summary>
    private const float DESTINATION_TOLERANCE = 0.001f;

    #region MonoBehaviours
    private void Awake()
    {
        InitUI();
    }

    private void Start()
    {
        SetParcelText($"Cargo: {Cargo.PackageCount}");
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
                    Connection finalConnection = m_connectionDrivePath[m_connectionDrivePath.Count - 1];
                    OnReachedPathEnd?.Invoke(this, finalConnection.ToNode);

                    SetStatusText($"Finished path to '{finalConnection.ToNode.name}'");
                    
                    ResetPath();
                }
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
            Connection nextConnection = m_connectionDrivePath[m_currentTargetNodeIndex];
            m_currentTargetNode = nextConnection.ToNode;

            // Fire event to first set new target location
            OnTravelNewConnection?.Invoke(this, nextConnection);

            SetStatusText($"New Path: Driving to '{connectionPath[connectionPath.Count - 1].ToNode.name}'");
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
    /// Deliveres a package form the cargo
    /// </summary>
    public void DeliverPackage()
    {
        Debug.Log($"Truck '{this.gameObject.name}' delivered package at '{m_currentTargetNode.name}'");
        Cargo.RemovePackages(1);

        SetParcelText($"Parcels: {Cargo.PackageCount}");
    }

    /// <summary>
    /// Set the amount of packages in the Truck's cargo
    /// </summary>
    /// <param name="packageAmt"></param>
    public void SetPackages(int packageAmt)
    {
        Cargo.AddPackages(packageAmt);

        SetParcelText($"Parcels: {Cargo.PackageCount}");
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

    /// <summary>
    /// Pauses truck movement
    /// </summary>
    public void PauseMovement()
    {
        if (!IsWaiting)
            SetStatusText($"Waiting at '{m_connectionDrivePath[m_currentTargetNodeIndex].FromNode.name}'");
        
        IsWaiting = true;
    }

    /// <summary>
    /// Resumes truck movement
    /// </summary>
    public void ResumeMovement()
    {
        if (IsWaiting)
            SetStatusText($"Resuming to '{m_connectionDrivePath[m_connectionDrivePath.Count - 1].ToNode.name}'");

        IsWaiting = false;
    }

    private void InitUI()
    {
        // Configure containing game object
        GameObject canvasGO = new GameObject("TruckCanvas", typeof(RectTransform));
        canvasGO.transform.SetParent(this.transform);
        canvasGO.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        canvasGO.transform.localEulerAngles = new Vector3(90f, 90f, 0f);

        // Configure the canvas
        Vector2 canvasSize = new Vector2(100f, 100f);
        float biggerTextHeight = canvasSize.y / 1.5f;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        // Set canvas height/width
        canvas.GetComponent<RectTransform>().sizeDelta = canvasSize;
        // Set Canvas X & Y
        canvas.transform.localPosition = new Vector3(0.0f, 4f, 0f);

        // text game object
        GameObject txtGO = new GameObject("TextGO", typeof(RectTransform));
        txtGO.transform.SetParent(canvasGO.transform);
        txtGO.transform.localPosition = Vector3.zero;

        // RectTransform on text game object
        RectTransform statusRect = txtGO.GetComponent<RectTransform>();
        statusRect.localScale = Vector3.one;
        statusRect.right = statusRect.up = Vector3.zero;
        statusRect.localEulerAngles = Vector3.zero;

        // Stretch preset values
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.offsetMin = statusRect.offsetMax = Vector3.zero;
        // Set Y size half of total canvas
        statusRect.sizeDelta = new Vector2(0f, biggerTextHeight);

        // status text settings
        m_statusText = txtGO.AddComponent<Text>();
        // Set default text cause Unity doesn't do this for some reason 🤔
        m_statusText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        //m_statusText.text = "";
        m_statusText.color = Color.white;
        m_statusText.fontSize = 15;

        GameObject parcelTxtGO = new GameObject("ParcelGO", typeof(RectTransform));
        parcelTxtGO.transform.SetParent(canvasGO.transform);
        parcelTxtGO.transform.localPosition = Vector3.zero;
        
        // Set RectTransform on parcel text
        RectTransform parcelRect = parcelTxtGO.GetComponent<RectTransform>();
        parcelRect.localScale = Vector3.one;
        parcelRect.right = parcelRect.up = Vector3.zero;
        parcelRect.localEulerAngles = Vector3.zero;
        // Stretch preset values
        parcelRect.anchorMin = new Vector2(0f, 1f);
        parcelRect.anchorMax = new Vector2(1f, 1f);
        parcelRect.pivot = new Vector2(0.5f, 1f);
        parcelRect.offsetMin = parcelRect.offsetMax = Vector3.zero;
        // Set Y size half of total canvas
        parcelRect.sizeDelta = new Vector2(0f, canvasSize.y - biggerTextHeight);

        // Add Text component for parcels
        m_parcelText = parcelTxtGO.AddComponent<Text>();
        m_parcelText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        m_parcelText.text = $"Parcels: {Cargo.PackageCount}";
        m_parcelText.color = Color.white;
        m_parcelText.fontSize = 15;
    }

    /// <summary>
    /// Sets the status message text above the truck
    /// </summary>
    /// <param name="message"></param>
    private void SetStatusText(string message)
    {
        if (m_statusText)
        {
            m_statusText.text = message;
        }
    }

    /// <summary>
    /// Sets the parcel message text in the truck's canvas
    /// </summary>
    /// <param name="message"></param>
    private void SetParcelText(string message)
    {
        if (m_parcelText)
        {
            m_parcelText.text = message;
        }
    }
}
