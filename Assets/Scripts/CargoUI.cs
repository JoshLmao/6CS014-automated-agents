using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CargoUIController : MonoBehaviour
{
    /// <summary>
    /// Text to display how much cargo the object has
    /// </summary>
    private Text m_cargoText = null;
    /// <summary>
    /// Text that displays the current status
    /// </summary>
    private Text m_statusText = null;

    #region MonoBehaviours
    void Awake()
    {
        InitUI();
    }
    #endregion

    /// <summary>
    /// Sets the status message text above the truck
    /// </summary>
    /// <param name="message"></param>
    public void SetStatusText(string message)
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
    public void SetCargoAmount(int amount)
    {
        if (m_cargoText)
        {
            m_cargoText.text = $"Cargo: {amount}";
        }
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
        m_cargoText = parcelTxtGO.AddComponent<Text>();
        m_cargoText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        m_cargoText.text = $"Parcels: 0";
        m_cargoText.color = Color.white;
        m_cargoText.fontSize = 15;
    }
}
