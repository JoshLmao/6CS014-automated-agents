using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarSceneController : MonoBehaviour
{
    /// <summary>
    /// The AStarManager for the scene
    /// </summary>
    private AStarManager m_astarManager = new AStarManager();
    
    /// <summary>
    /// All waypoints inside the active scene
    /// </summary>
    protected List<GameObject> m_allWaypoints = new List<GameObject>();

    /// <summary>
    /// List of Connections to the target destination waypoint
    /// </summary>
    //protected List<Connection> m_destinationPath = new List<Connection>();

    #region MonoBehaviours
    protected virtual void Start()
    {
        InitAStarManager();
        Debug.Log($"Initialized AStarManager with '{m_allWaypoints.Count}' waypoints");
    }
    #endregion

    private void InitAStarManager()
    {
        /// Get all waypoints in the active scene
        GameObject[] allWaypoints = GameObject.FindGameObjectsWithTag("Waypoint");
        foreach (GameObject waypoint in allWaypoints)
        {
            WaypointCON tempCon = waypoint.GetComponent<WaypointCON>();
            if (tempCon)
                m_allWaypoints.Add(waypoint);
        }

        /// Add connections from waypoints into the AStarManager
        foreach (GameObject waypoint in m_allWaypoints)
        {
            WaypointCON tempCon = waypoint.GetComponent<WaypointCON>();
            foreach (GameObject waypointConNode in tempCon.Connections)
            {
                Connection aConnection = new Connection();
                aConnection.FromNode = waypoint;
                aConnection.ToNode = waypointConNode;

                m_astarManager.AddConnection(aConnection);
            }
        }
    }

    /// <summary>
    /// Navigates using AStar from a start Waypoint GameObject to an End Waypoint GameObject 
    /// and stores the waypoint path in m_destinationPath
    /// </summary>
    /// <param name="start">The start waypoint gameobject</param>
    /// <param name="end">The end waypoint gameobject</param>
    /// <returns>If the AStar path was able to determine a path</returns>
    protected List<Connection> Navigate(GameObject start, GameObject end)
    {
        /// Validate start and end are legit
        if (start == null || end == null)
        {
            return null;
        }

        /// Pathfind from start to end and check connection path is valid
        List<Connection> connectionPath = m_astarManager.PathfindAStar(start, end);

        if (connectionPath == null || connectionPath != null && connectionPath.Count <= 0)
        {
            return null;
        }

        /// Set destination array and return true
        //m_destinationPath = connectionPath;
        return connectionPath;
    }
}
