using MLAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Crea il provider di input per il giocatore
/// </summary> 
public class HumanController : AbstractPlayerInput
{

#if !(UNITY_EDITOR || UNITY_STANDALONE)
    /// <summary>
    /// Roba per il touch
    /// </summary>
    private Vector2 touchBeganAt;
#endif

    /// <summary>
    /// Aggiorna l'input
    /// </summary>
    public override void UpdateInput()
    {
        // Colleziona gli input
#if UNITY_EDITOR || UNITY_STANDALONE
        var h = Input.GetAxisRaw("Horizontal");
        var wantsToJump = Input.GetAxis("Vertical") > 0;
#else
        float h = 0;
        bool wantsToJump = false; 
        
        // Se ci sono touch
        if (Input.touchCount > 0)
        {
            // Consideriamo solo il primo
            var touch = Input.touches[0];
            if(touch.phase == TouchPhase.Began)
            {
                // Salva la posizione del dito
                touchBeganAt = touch.position;
            }
            else if(touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                // Se il dito si sta muovendo o è fermotouch.deltaTime
                var diff = touch.position - touchBeganAt;
                h = diff.normalized.x;
                wantsToJump = diff.y > 80;             // Questa roba andrebbe fatta sulla dimensione dello schermo
            } 
        }
#endif

        // Setup dei dati
        Horizontal = h;
        WantsToJump = wantsToJump;
    }
}
