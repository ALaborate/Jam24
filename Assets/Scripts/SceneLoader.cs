using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using Mirror;

public class SceneLoader : NetworkBehaviour
{
    public string sceneName;


    public override void OnStartClient()
    {
        base.OnStartClient();
        var parameters = new LoadSceneParameters(LoadSceneMode.Additive);
        var scene = SceneManager.LoadScene(sceneName, parameters);
        //SceneManager.SetActiveScene(scene); //no need
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        SceneManager.UnloadSceneAsync(sceneName);
    }
}
