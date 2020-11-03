using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadEnvironment : MonoBehaviour
{
    [SerializeField]
    private string m_environmentSceneName = "";

    void Start()
    {
        if (string.IsNullOrEmpty(m_environmentSceneName))
        {
            Debug.LogError("No Environment Scene Name! Unable to load an environments");
        }
        else
        {
            // Only load environment if it isn't already
            if (!SceneManager.GetSceneByName(m_environmentSceneName).isLoaded)
            {
                SceneManager.LoadScene(m_environmentSceneName, LoadSceneMode.Additive);
            }
        }
    }
}
