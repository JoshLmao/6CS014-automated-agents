using System.Collections;
using UnityEngine;
using System;

public class Part1_SceneController : AStarSceneController
{
    public GameObject StartWaypoint = null;
    public GameObject DeliveryDestination = null;

    public GameObject TruckObject = null;

    private Truck m_truck;
    
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
        bool canNavigate = this.Navigate(StartWaypoint, DeliveryDestination);
        if (!canNavigate)
        {
            Debug.LogError("Unable to find a path to destination");
            return;
        }

        Debug.Log($"Found path to '{DeliveryDestination.name}', setting Truck path");

        if (TruckObject != null)
        {
            m_truck = TruckObject.GetComponent<Truck>();
            if (m_truck)
            {
                m_truck.DriveAlong(m_destinationPath);

                m_truck.OnReachedPathStart += OnReachedStartPath;
                m_truck.OnReachedPathEnd += OnReachedEndPath;
            }
        }
        else
        {
            Debug.LogError("No TruckObject set!");
            return;
        }
    }

    void OnReachedStartPath()
    {
        // Reached start position. Completed delivery, stop.
    }

    void OnReachedEndPath()
    {
        // Reached delivery drop off location, wait and then return to start position
        StartCoroutine(WaitNavigate(WAIT_SECONDS, DeliveryDestination, StartWaypoint));

        // Drop off package
        m_truck.DeliverPackage();
    }

    private IEnumerator WaitNavigate(float seconds, GameObject start, GameObject end)
    {
        yield return new WaitForSeconds(seconds);

        this.Navigate(start, end);
        m_truck.DriveAlong(m_destinationPath);
    }
}
