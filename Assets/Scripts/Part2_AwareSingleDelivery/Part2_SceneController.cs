using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class Part2_SceneController : AStarSceneController
{
    [Serializable]
    public class TruckInfo
    {
        /// <summary>
        /// Truck GameObject used to navigate along the path
        /// </summary>
        public GameObject TruckPrefab;
        /// <summary>
        /// Start waypoint for this truck
        /// </summary>
        public GameObject Start;
        /// <summary>
        /// End waypoint for this waypoint
        /// </summary>
        public GameObject End;
        /// <summary>
        /// Amount of packages this truck has on start
        /// </summary>
        public int PackageCount;
    }

    /// <summary>
    /// All Trucks within the scene, with their start and end points
    /// </summary>
    public List<TruckInfo> Trucks = new List<TruckInfo>();

    public Dictionary<GameObject, TruckInfo> InstantiatedTrucks = new Dictionary<GameObject, TruckInfo>();

    [SerializeField]
    private Transform m_truckParentTransform = null;

    protected override void Start()
    {
        base.Start();

        foreach(TruckInfo info in Trucks)
        {
            if (info.Start == null|| info.End == null)
            {
                Debug.LogError("No Start or End set of truck info. Can't pathfind");
                continue;
            }

            GameObject spawnedTruck = Instantiate(info.TruckPrefab, m_truckParentTransform);
            InstantiatedTrucks.Add(spawnedTruck, info);

            Part2_Truck p2Truck = spawnedTruck.GetComponent<Part2_Truck>();
            if (p2Truck)
            {
                bool canNavigate = this.Navigate(info.Start, info.End);

                if (canNavigate)
                {
                    // Configure truck
                    p2Truck.OnReachedPathEnd += OnTruckReachedPathEnd;
                    p2Truck.SetPackages(info.PackageCount);

                    p2Truck.DriveAlong(m_destinationPath);

                    Debug.Log($"Truck '{spawnedTruck.name}' determined path to '{info.End.name}' with '{m_destinationPath.Count}' connections");
                }
            }
        }
    }

    private void OnTruckReachedPathEnd(AwareTruck truck, GameObject arrivalWaypoint)
    {
        // Get the instantiated truck and it's info
        KeyValuePair<GameObject, TruckInfo> kvp = InstantiatedTrucks.FirstOrDefault(x => x.Key.name == truck.gameObject.name);
        
        // Check it isn't null
        if (kvp.Key != null && kvp.Value != null)
        {
            TruckInfo info = kvp.Value;

            // Check if the arrivalWaypoint is equal to the set end point and not arriving at Start waypoint
            if (arrivalWaypoint == info.End)
            {
                StartCoroutine(WaitAndNavigate(3f, truck, info.End, info.Start));

                truck.DeliverPackage();
            }
        }
    }

    private IEnumerator WaitAndNavigate(float seconds, AwareTruck truck, GameObject start, GameObject end)
    {
        yield return new WaitForSeconds(seconds);

        // Truck has arrived at destined End point, make it travel back
        bool canNav = this.Navigate(start, end);
        if (canNav)
        {
            truck.DriveAlong(m_destinationPath);

            Debug.Log($"Truck '{truck.gameObject.name}' returning to depot/start at '{end.name}'");
        }
    }
}
