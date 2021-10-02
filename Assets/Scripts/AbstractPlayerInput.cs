using MLAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Crea il provider di input per il giocatore
/// </summary> 
public abstract class AbstractPlayerInput : MonoBehaviour 
{  
    /// <summary>
    /// Vuole saltare?
    /// </summary>
    public bool WantsToJump { get; set; }

    /// <summary>
    /// Vuole andare in orizzontale?
    /// </summary>
    public float Horizontal { get; set; }

    /// <summary>
    /// Richiede l'update
    /// </summary>
    public abstract void UpdateInput();
}
