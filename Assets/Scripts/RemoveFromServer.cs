using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RemoveFromServer : NetworkBehaviour
{
    public GameObject target;
    ///<remarks>We use delay for camera after a server start to render a couple of more frames to actually hide disabled UI elements which otherwise reside in frame buffer</remarks>
    public int frameDelay = 0;

    private GameObject actualTarget { get { return target ?? base.gameObject; } }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if(isServerOnly)
        {
            StartCoroutine(Remove());
        }
    }

    private System.Collections.IEnumerator Remove()
    {
        var counter = frameDelay;
        while (counter-- > 0)
            yield return null;
        actualTarget.SetActive(false);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        actualTarget.SetActive(true);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (isServerOnly)
            actualTarget.SetActive(true);
    }
}
