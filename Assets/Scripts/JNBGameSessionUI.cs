using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Crea l'interfaccia grafica per un <see cref="JNBGameSession"/>
/// </summary>
public class JNBGameSessionUI : MonoBehaviour
{
    /// <summary>
    /// Network
    /// </summary>
    public JNBGameSession Session;

    /// <summary>
    /// Label che mostra la fine della partita
    /// </summary>
    public Text FinishTimeLabelUI; 

    // Update is called once per frame
    void LateUpdate()
    {
        var SessionState = (JNBGameSession.SessionStateEnum)Session.CurrentState.Value;

        // Aggiorna lo stato
        switch (SessionState)
        {
            case JNBGameSession.SessionStateEnum.Waiting:
                // Siamo in attesa
                FinishTimeLabelUI.text = "Riscaldamento";
                break;

            case JNBGameSession.SessionStateEnum.Playing:
                if (Session.EndsAtDateTime > DateTime.UtcNow)
                {
                    var rem = Session.EndsAtDateTime - DateTime.UtcNow;
                    if (rem.Minutes > 0)
                        FinishTimeLabelUI.text = rem.Minutes.ToString("00") + ":" + rem.Seconds.ToString("00");
                    else
                        FinishTimeLabelUI.text = rem.Seconds.ToString("00");
                }
                else
                {
                    // Partita finita!
                    FinishTimeLabelUI.text = "00";
                }
                break;

            case JNBGameSession.SessionStateEnum.Finished:
                // Partita finita!
                FinishTimeLabelUI.text = "Finita"; 
                // Mostra la highscore
                break;
        }

    }

    /// <summary>
    /// Stop al client
    /// </summary>
    public void StopGame()
    {
        // Esci dal gioco
        Session.StopGame();
    }
}
