using UnityEngine;

public interface IConnection
{
    GameObject FromNode { get; }
    GameObject ToNode { get; }
}