using MLAPI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Crea un semplice bot che salta e si muove a caso
/// </summary> 
public class SimpleStupidBotController : AbstractPlayerInput
{ 
    /// <summary>
    /// AI attiva?
    /// </summary>
    public bool IsActive = true;

    /// <summary>
    /// Livello dei giocatori
    /// </summary>
    public LayerMask PlayerLayerMask;

    /// <summary>
    /// Raggio di collisione
    /// </summary>
    public float MinPlayerDistance = 20;

    /// <summary>
    /// Cambio di direzione
    /// </summary>
    private float changeDirectionTime = 0;

    // Update is called once per frame
    public override void UpdateInput()
    {
        // Se siamo il server o l'host
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {
            if (!IsActive)
            {
                Horizontal = 0;
                WantsToJump = false;
                return;
            }

            // Scegle se andare a destra o a sinistra 
            changeDirectionTime -= Time.deltaTime;
            if (changeDirectionTime <= 0)
            {
                // Set
                Horizontal = Random.value > 0.5f ? 1 : -1;
                changeDirectionTime = Random.value * 2 + 1;
            }

            // Vuole saltare?
            WantsToJump = Random.value > 0.8f;

            // Se ha deciso di non saltare
            if (!WantsToJump)
            {
                var hits = Physics.OverlapSphere(this.transform.position, MinPlayerDistance, PlayerLayerMask);
                if (hits.Where(x => x.transform != this.transform).Count() > 0)
                    WantsToJump = true;
            }
        }
        else
        {
            // Disabilita questo controller
            this.enabled = false;
        }
    }
}
