using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;

public class Part1_SceneController : AStarSceneController
{
    /// <summary>
    /// Start waypoint for the single truck to start from
    /// </summary>
    public GameObject StartWaypoint = null;
    /// <summary>
    /// End waypoint for the single truck to deliver to
    /// </summary>
    public GameObject DeliveryDestination = null;
    /// <summary>
    /// The GameObject containing the Truck component
    /// </summary>
    public GameObject TruckObject = null;

    /// <summary>
    /// Truck component on the TruckObject
    /// </summary>
    private SimpleTruck m_truck;

    private bool m_returningToDepot = false;

    /// <summary>
    /// Amunt of seconds a truck will wait before 'completing' a delivery
    /// </summary>
    private const float WAIT_SECONDS = 3f;

    protected override void Start()
    {
        base.Start();

        if (!DeliveryDestination || !StartWaypoint)
        {
            Debug.LogError("No start or destination waypoints set! Check and try again");
            return;
        }

        /// Navigate a path from start to end waypoints
        List<Connection> connectionPath = this.Navigate(StartWaypoint, DeliveryDestination);
        if (connectionPath == null)
        {
            Debug.LogError("Unable to find a path to destination");
            return;
        }

        Debug.Log($"Found path to '{DeliveryDestination.name}', setting Truck path");

        if (TruckObject != null)
        {
            m_truck = TruckObject.GetComponent<SimpleTruck>();
            if (m_truck)
            {
                m_truck.OnReachedPathEnd += OnReachedEndPath;
                
                m_truck.DriveAlong(connectionPath);
            }
        }
        else
        {
            Debug.LogError("No TruckObject set!");
            return;
        }
    }

    void OnReachedEndPath()
    {
        if (m_returningToDepot)
        {
            // Returned home to depot. Finished.
            Debug.Log("Returned home to Depot");
        }
        else
        {
            // Reached delivery drop off location, wait and then return to start position
            StartCoroutine(WaitNavigate(WAIT_SECONDS, DeliveryDestination, StartWaypoint));

            // Drop off package
            m_truck.DeliverPackage();

            // Truck will be going to Depot next
            m_returningToDepot = true;

            Debug.Log("Reached destination. Delivering and then returning to Depot");
        }
    }

    private IEnumerator WaitNavigate(float seconds, GameObject start, GameObject end)
    {
        yield return new WaitForSeconds(seconds);

        // Determine and set a new drive path for Truck
        List<Connection> returnPath = this.Navigate(start, end);
        m_truck.DriveAlong(returnPath);
    }
}
