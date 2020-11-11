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

    /// <summary>
    /// List of Instantiated tricks and their travel info
    /// </summary>
    public Dictionary<AwareTruck, TruckInfo> InstantiatedTrucks = new Dictionary<AwareTruck, TruckInfo>();

    /// <summary>
    /// List of active trucks and their current travel to connection
    /// </summary>
    private Dictionary<AwareTruck, Connection> m_currentTruckConnections = new Dictionary<AwareTruck, Connection>();

    /// <summary>
    /// Transform to spawn trucks under
    /// </summary>
    [SerializeField]
    private Transform m_truckParentTransform = null;

    #region MonoBehaviours
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
            Part2_Truck p2Truck = spawnedTruck.GetComponent<Part2_Truck>();
            InstantiatedTrucks.Add(p2Truck, info);
            
            if (p2Truck)
            {
                bool canNavigate = this.Navigate(info.Start, info.End);

                if (canNavigate)
                {
                    // Configure truck
                    p2Truck.OnTravelNewConnection += OnTruckTravelNewConnection;
                    p2Truck.OnReachedPathEnd += OnTruckReachedPathEnd;
                    p2Truck.SetPackages(info.PackageCount);

                    // Set Truck drive path
                    p2Truck.DriveAlong(m_destinationPath);

                    Debug.Log($"Truck '{spawnedTruck.name}' determined path to '{info.End.name}' with '{m_destinationPath.Count}' connections");
                }
            }
        }
    }

    private void Update()
    {
        if (m_currentTruckConnections.Count > 0)
        {
            /// For each truck and it's current connection
            foreach(KeyValuePair<AwareTruck, Connection> thisKvp in m_currentTruckConnections)
            {
                /// Compare waypoint names to check if they are the same
                KeyValuePair<AwareTruck, Connection> matchingKvp = m_currentTruckConnections
                    .FirstOrDefault( kvp => kvp.Value.ToNode.name == thisKvp.Value.ToNode.name && kvp.Key.name != thisKvp.Key.name );
                AwareTruck truckOne = thisKvp.Key;
                AwareTruck truckTwo = matchingKvp.Key;

                /// If current iterarte truck is waiting, check if they can resume
                /// Only check truck one as the others will be checked next iteration
                if (truckOne.IsWaiting)
                {
                    List<AwareTruck> waitingTrucks = new List<AwareTruck>();
                    bool isOnSameConnection = false;
                    foreach (KeyValuePair<AwareTruck, Connection> checkKvp in m_currentTruckConnections)
                    {
                        if (thisKvp.Value.ToNode.name == checkKvp.Value.ToNode.name)
                        {
                            //isOnSameConnection = true;
                            waitingTrucks.Add(thisKvp.Key);
                        }
                    }

                    // More than 1 truck waiting at same connection, resume first one in list
                    if (waitingTrucks.Count > 1)
                    {
                        waitingTrucks[0].ResumeMovement();
                        Debug.Log($"Resuming Truck '{waitingTrucks[0]}' in queue of '{waitingTrucks.Count}'");
                    }
                    // else if truckOne has no others waiting, resume
                    else if (!isOnSameConnection)
                    {
                        truckOne.ResumeMovement();
                        Debug.Log($"Resuming Truck '{truckOne.name}'");
                    }
                }

                // Check match isn't null
                if ( matchingKvp.Key != null && matchingKvp.Value != null )
                {
                    /// If isn't waiting and connections are matching...
                    bool eitherTruckWaiting = truckOne.IsWaiting || truckTwo.IsWaiting;
                    if (!eitherTruckWaiting && thisKvp.Value.ToNode.name == matchingKvp.Value.ToNode.name)
                    {
                        if (truckOne.Cargo.PackageCount > truckTwo.Cargo.PackageCount)
                        {
                            ///truckOne is slower, pause it
                            truckOne.PauseMovement();
                            Debug.Log($"Pausing Truck '{truckOne.name};");
                        }
                        else
                        {
                            /// truckTwo is slower
                            /// or cargo is same so prioritise truckTwo
                            truckTwo.PauseMovement();
                            Debug.Log($"Pausing Truck '{truckTwo.name}'");
                        }
                    }
                }
            }
        }
    }
    #endregion

    private void OnTruckTravelNewConnection(AwareTruck truck, Connection nextTravelConnection)
    {
        // Update or add new connection to list
        if (m_currentTruckConnections.ContainsKey(truck))
        {
            m_currentTruckConnections[truck] = nextTravelConnection;
        }
        else
        {
            m_currentTruckConnections.Add(truck, nextTravelConnection);
        }
    }

    /// <summary>
    /// Functionality for when a truck reaches its final path end
    /// </summary>
    /// <param name="truck"></param>
    /// <param name="arrivalWaypoint"></param>
    private void OnTruckReachedPathEnd(AwareTruck truck, GameObject arrivalWaypoint)
    {
        // Get the instantiated truck and it's info
        KeyValuePair<AwareTruck, TruckInfo> kvp = InstantiatedTrucks.FirstOrDefault(x => x.Key.gameObject.name == truck.gameObject.name);
        
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

    /// <summary>
    /// Coroutine for waiting X seconds and then navigating from a start and end on an AwareTruck
    /// </summary>
    /// <param name="seconds"></param>
    /// <param name="truck"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
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
