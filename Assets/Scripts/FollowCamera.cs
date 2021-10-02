using MLAPI;
using MLAPI.Connection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Telecamera scema che segue il giocatore
/// </summary>
public class FollowCamera : MonoBehaviour
{
    /// <summary>
    /// Target da seguire
    /// </summary>
    public NetworkControllerBunny Target;

    /// <summary>
    /// Offset del target
    /// </summary>
    public Vector3 Offset;

    /// <summary>
    /// Limite minimo
    /// </summary>
    public Vector2 MinLimits;

    /// <summary>
    /// Limite massimo
    /// </summary>
    public Vector2 MaxLimits;

    // Update is called once per frame
    void LateUpdate()
    {
        if (Target == null)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out NetworkClient client))
            {
                if (client.PlayerObject != null)
                    Target = client.PlayerObject.GetComponent<NetworkControllerBunny>();
            }
        }

        // Dove andiamo?
        Vector3 destination = this.transform.position;
        if (Target != null)
        {
            if (Target.SpriteRenderer.enabled)
                destination = Target.transform.position + Offset;
        }

        // Applichiamo i limiti
        destination.x = Mathf.Clamp(destination.x, MinLimits.x, MaxLimits.x);
        destination.y = Mathf.Clamp(destination.y, MinLimits.y, MaxLimits.y);

        // Impostiamo la destinazione
        this.transform.position = destination;
    }
}