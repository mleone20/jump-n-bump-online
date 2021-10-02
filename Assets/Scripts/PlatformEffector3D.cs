using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Crea una piattaforma sul quale il giocatore può salire "dal basso"
/// </summary>
[RequireComponent(typeof(Collider))]
public class PlatformEffector3D : MonoBehaviour
{
    /// <summary>
    /// Permetti la salita
    /// </summary>
    public bool EnableEffect = false;

    /// <summary>
    /// Nascondi il renderer se presente
    /// </summary>
    public bool HideMeshRender = true;

    /// <summary>
    /// Collider della base
    /// </summary>
    public Collider PlatformCollider;

    /// <summary>
    /// Collider da trigger
    /// </summary>
    public Collider TriggerCollider;

    // Start is called before the first frame update
    void Start()
    {
        // Deve nascondere il renderer?
        if (HideMeshRender)
            foreach (var r in this.GetComponents<MeshRenderer>())
                r.enabled = false;

        // Ottieni il collider 
        TriggerCollider.isTrigger = EnableEffect;
        TriggerCollider.enabled = EnableEffect;
    }
     

    /// <summary>
    /// Evento lanciato quando un collider entra nell'area del trigger
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        // E' un giocatore?
        var player = other.GetComponent<NetworkControllerBunny>();
        if(player != null)
        {
            // E' un giocatore, disabilitiamo il check
            Physics.IgnoreCollision(player.CharacterController, this.PlatformCollider);
        }
    }

    /// <summary>
    /// Evento lanciato quando esce dal trigger
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerExit(Collider other)
    {
        // E' un giocatore?
        var player = other.GetComponent<NetworkControllerBunny>();
        if (player != null)
        {
            // E' un giocatore, disabilitiamo il check
            Physics.IgnoreCollision(player.CharacterController, this.PlatformCollider, false);
        } 
    }
}
