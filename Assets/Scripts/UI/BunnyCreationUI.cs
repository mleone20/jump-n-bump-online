using MLAPI;
using MLAPI.Connection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BunnyCreationUI : MonoBehaviour
{ 
    /// <summary>
    /// Colore scelto
    /// </summary>
    public Image BunnySprite;

    /// <summary>
    /// Nome del coniglietto
    /// </summary>
    public InputField BunnyName;

    /// <summary>
    /// Colori disponibili
    /// </summary>
    private Color[] Colors = new Color[]
    {
        Color.red,
        Color.white,
        Color.blue,
        Color.green,
        Color.grey,
        Color.cyan,
        Color.magenta,
    };

    // Start is called before the first frame update
    void Start()
    {
    }

    private void OnEnable()
    {
        // Imposta il nome di default di queto coniglio
        BunnyName.text = PlayerPrefs.GetString("Bunny.Name", "Bunny" + (int)(10000 * Random.value));
        BunnySprite.color = new Color(PlayerPrefs.GetFloat("Bunny.Color.R", 0), PlayerPrefs.GetFloat("Bunny.Color.G", 0), PlayerPrefs.GetFloat("Bunny.Color.B", 0));
        if (BunnySprite.color.r == 0 && BunnySprite.color.g == 0 && BunnySprite.color.b == 0)
            ChangeColor();
    }

    private void OnDisable()
    {
        PlayerPrefs.SetString("Bunny.Name", BunnyName.text);
        PlayerPrefs.SetFloat("Bunny.Color.R", BunnySprite.color.r);
        PlayerPrefs.SetFloat("Bunny.Color.G", BunnySprite.color.g);
        PlayerPrefs.SetFloat("Bunny.Color.B", BunnySprite.color.b);
        PlayerPrefs.Save();
    }
     
    /// <summary>
    /// Cambia mettendo un colore a caso
    /// </summary>
    public void ChangeColor()
    {
        // Ottiene un colore casuale
        BunnySprite.color = Colors[(int)(Random.value * 100) % Colors.Length]; 
    }
}
