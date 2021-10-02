using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crea la UI per un bunny
/// </summary>
public class BunnyStatsUI : MonoBehaviour
{
    /// <summary>
    /// Coniglio dal quale prendere i dati
    /// </summary>
    public NetworkBunny Player; 

    /// <summary>
    /// Sprite di preview
    /// </summary>
    public Image BunnyPreview;

    /// <summary>
    /// Nome del coniglietto
    /// </summary>
    public Text BunnyName;

    /// <summary>
    /// Nome del coniglietto
    /// </summary>
    public Text BunnyPoints;

    // Update is called once per frame
    void Update()
    {
        // Aggiorna i dati
        if (Player != null)
        {
            BunnyName.text = Player.PlayerName.Value;
            BunnyPoints.text = ""+ Player.Points.Value;
            BunnyPreview.color = Player.PlayerColor.Value;
        }
    }
}
