using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AlertUI : MonoBehaviour
{
    /// <summary>
    /// UI del testo
    /// </summary>
    public Text TextUI;

    /// <summary>
    /// Tasto OK
    /// </summary>
    public Button OkButtonUI;

    /// <summary>
    /// Imposta il messaggio da mostrare e mostra questo alert
    /// </summary>
    /// <param name="Text"></param>
    public void Open(string Message, string OK = "")
    {
        // Imposta il messaggio
        TextUI.text = Message.Trim();

        // Che facciamo con OK?
        if (!string.IsNullOrEmpty(OK))
        {
            // Setup del tasto OK
            OkButtonUI.GetComponentInChildren<Text>().text = OK;
            OkButtonUI.interactable = true;
            OkButtonUI.gameObject.SetActive(true);
        }
        else
        {
            // Nascondi il tasto OK
            OkButtonUI.gameObject.SetActive(false);
        }

        // Mostra l'oggetto
        this.gameObject.SetActive(true);
    }

    /// <summary>
    /// Nega il click del tasto OK
    /// </summary>
    public void SuspendOK()
    {
        OkButtonUI.interactable = false; 
    }

    /// <summary>
    /// Chiude l'alert
    /// </summary>
    public void Close()
    {
        this.gameObject.SetActive(false);
    }
}
