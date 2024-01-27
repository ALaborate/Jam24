using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerAudio : NetworkBehaviour
{
    private PlayerHealth playerHealth;
    private NetworkCharacterController networkCharacterController;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (isServerOnly)
            gameObject.SetActive(false);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        playerHealth = GetComponentInParent<PlayerHealth>();
        playerHealth.OnRofl.AddListener(OnRofl);
        playerHealth.OnRoflOver.AddListener(OnRoflOver);
        playerHealth.OnBeingTickled.AddListener(OnBeingTickled);
        playerHealth.OnStopTickled.AddListener(OnStopTickled);

        networkCharacterController = GetComponentInParent<NetworkCharacterController>();
        networkCharacterController.OnCollision.AddListener(OnCollision);
    }

    private void OnRofl()
    {
        Debug.Log($"Play rofl on {playerHealth.gameObject.name}");
    }

    private void OnRoflOver()
    {
        Debug.Log($"Stop playing rofl on {playerHealth.gameObject.name}");
    }

    private void OnBeingTickled()
    {
        Debug.Log($"Play tickle on {playerHealth.gameObject.name}");
    }

    private void OnStopTickled()
    {
        Debug.Log($"Stop playing tickle on {playerHealth.gameObject.name}");
    }

    /// <param name="severity">0 - contact at zero speed, 1 - deadly collision on max velocity, >1 collision velocity is more than max player velocity</param>
    private void OnCollision(float severity)
    {
        Debug.Log("Play collision of severity " + severity.ToString("0.00") + $" on {playerHealth.gameObject.name}");
    }
}
