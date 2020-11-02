using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointCON : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> m_connections = new List<GameObject>();

    public List<GameObject> Connections
    {
        get { return m_connections; }
    }

    [SerializeField]
    private enum waypointPropsList {  Standard, Goal };
    [SerializeField]
    private waypointPropsList WaypointType = waypointPropsList.Standard;

    private bool m_objectSelected = false;
    private Vector3 NoOffset = new Vector3(0, 0, 0);
    private Vector3 UpOffset = new Vector3(0, 0.1f, 0);

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDrawGizmos()
    {
        if (m_objectSelected)
        {
            DrawWaypoint(Color.red, Color.magenta, UpOffset);
        }
        else
        {
            DrawWaypoint(Color.yellow, Color.blue, NoOffset);
        }

        m_objectSelected = false;
    }

    void OnDrawGizmosSelected()
    {
        m_objectSelected = true;
    }

    private void DrawWaypoint(Color waypointColor, Color connectionsColor, Vector3 offset)
    {
        Gizmos.color = waypointColor;
        Gizmos.DrawSphere(transform.position, 0.2f);

        for (int i = 0; i < Connections.Count; i++)
        {
            if (Connections[i] != null)
            {
                Gizmos.color = connectionsColor;
                Gizmos.DrawLine(transform.position + offset, Connections[i].transform.position + offset);
            }
        }
    }
}
