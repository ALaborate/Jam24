using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RemoveFromServer : NetworkBehaviour
{
    public GameObject target;

    private new GameObject actualTarget { get { return target ?? base.gameObject; } }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if(isServerOnly)
        {
            actualTarget.SetActive(false);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        actualTarget.SetActive(true);
    }
}
