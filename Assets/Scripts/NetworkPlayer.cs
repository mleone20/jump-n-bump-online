using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

/// <summary>
/// Rappresenta un giocatore sulla rete astratto
/// </summary>
public abstract class NetworkPlayer : NetworkBehaviour
{ 

    /// <summary>
    /// Prima dell'update
    /// </summary>
    protected virtual void BeforeUpdate()
    {
        // 
    }

    /// <summary>
    /// Update per il client locale
    /// </summary>
    protected abstract void UpdateLocal();

    /// <summary>
    /// Update per il client remoto
    /// </summary>
    protected abstract void UpdateRemote();
}
