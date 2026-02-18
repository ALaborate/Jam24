using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine;
using Mirror;

public class SceneLoader : MonoBehaviour
{
    public string sceneName;
    public GameModeType gameMode = GameModeType.ScoreCompete;

    private void Awake()
    {
        var enm = NetworkManager.singleton as EventfulNetworkManager;
        enm.OnServerStarted += () => enm.StartCoroutine(InitializeScene(isServer: true));
        enm.OnServerStopped += () => enm.StartCoroutine(Deinit());
        enm.OnClientStarted += () => enm.StartCoroutine(InitializeScene(isServer: false));
        enm.OnClientStopped += () => { enm.StartCoroutine(Deinit()); };
    }

    private void Start()
    {
#if UNITY_WEBGL
        NetworkManager.singleton.StartHost();
#endif
    }


    private bool isInitialized = false;

    private System.Collections.IEnumerator InitializeScene(bool isServer)
    {
        if(isServer)
            SceneManager.sceneLoaded += FinishInitialization;
        if (!isInitialized)
        {
            isInitialized = true;
            var parameters = new LoadSceneParameters(LoadSceneMode.Additive);
            var scene = SceneManager.LoadScene(sceneName, parameters);
            yield return null; //dont need coroutine without async operation, but let it be
        }
    }

    [Server]
    private void FinishInitialization(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != sceneName)
        {
            return;
        }

        EventManager.Instance?.InitializeGameMode(scene, gameMode);
    }



    private System.Collections.IEnumerator Deinit()
    {
        if (isInitialized)
        {
            if (SceneManager.GetSceneByName(sceneName).IsValid() || SceneManager.GetSceneByPath(sceneName).IsValid())
                yield return SceneManager.UnloadSceneAsync(sceneName);
            isInitialized = false;
            SceneManager.sceneLoaded -= FinishInitialization;
            yield return Resources.UnloadUnusedAssets(); 
        }
    }
}
