using UnityEngine;

public class Part1_SceneController : AStarSceneController
{
    public GameObject StartWaypoint = null;
    public GameObject DeliveryDestination = null;

    public GameObject TruckObject = null;

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

        if (TruckObject == null)
        {
            Debug.LogError("No TruckObject set!");
            return;
        }

        Truck truck = TruckObject.GetComponent<Truck>();
        if (truck)
        {
            truck.DriveAlong(m_destinationPath);
        }
    }
}
