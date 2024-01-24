using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using Mirror;

public class SceneLoader : NetworkBehaviour
{
    public string sceneName;
    public int numberOfFeathers = 1;
    public GameObject featherPrefab;


    public override void OnStartServer()
    {
        base.OnStartServer();
        InitializeScene();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        InitializeScene();
    }


    private bool isInitialized = false;
    private void InitializeScene()
    {
        if (!isInitialized)
        {
            isInitialized = true;
            SceneManager.sceneLoaded += FinishInitialization;
            var parameters = new LoadSceneParameters(LoadSceneMode.Additive);
            var scene = SceneManager.LoadSceneAsync(sceneName, parameters);

        }
    }

    private void FinishInitialization(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != sceneName)
        {
            return;
        }

        var spawns = GameObject.FindGameObjectsWithTag("FeatherSpawn");
        if (isServer)
        {
            for (int i = 0; i < numberOfFeathers; i++)
            {
                Vector3 spawnPos = Vector3.up * 30;
                if (spawns.Length > 0)
                {
                    var spawnInx = Random.Range(0, spawns.Length);
                    spawnPos = spawns[spawnInx].transform.position;
                }

                var feather = Instantiate(featherPrefab, spawnPos + Random.onUnitSphere, Quaternion.identity);
                SceneManager.MoveGameObjectToScene(feather, scene);
                NetworkServer.Spawn(feather);
            }
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        SceneManager.UnloadSceneAsync(sceneName);
        Deinit();
    }
    public override void OnStopServer()
    {
        base.OnStopServer();
        SceneManager.UnloadSceneAsync(sceneName);
        Deinit();
    }

    private void Deinit()
    {
        isInitialized = false;
        SceneManager.sceneLoaded -= FinishInitialization;
    }
}
