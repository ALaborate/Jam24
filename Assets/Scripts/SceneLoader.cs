using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using Mirror;

public class SceneLoader : MonoBehaviour
{
    public string sceneName;
    [Space]
    public GameObject featherPrefab;
    public int numberOfFeathers = 1;
    [Space]
    public GameObject bonusSpawnPrefab;
    public float bsMinHeight = 1f;
    public float bsMaxHeight = 3f;

    private void Awake()
    {
        var enm = NetworkManager.singleton as EventfulNetworkManager;
        enm.OnServerStart += () => enm.StartCoroutine(InitializeScene(isServer: true));
        enm.OnServerStop += () => enm.StartCoroutine(Deinit());
        enm.OnClientStart += () => enm.StartCoroutine(InitializeScene(isServer: false));
        enm.OnClientStop += () => { enm.StartCoroutine(Deinit()); };
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

    private void FinishInitialization(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != sceneName)
        {
            return;
        }

        var spawns = GameObject.FindGameObjectsWithTag("FeatherSpawn");

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

        var bonusSpawns = GameObject.FindGameObjectsWithTag("BonusSpawn");
        foreach (var spawn in bonusSpawns)
        {
            var spawnPos = spawn.transform.position;
            if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit))
            {
                if (hit.point != Vector3.zero)
                {
                    spawnPos = hit.point + Vector3.up * Random.Range(bsMinHeight, bsMaxHeight);
                }
            }
            var bonusSpawn = Instantiate(bonusSpawnPrefab, spawnPos, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(bonusSpawn, scene);
            NetworkServer.Spawn(bonusSpawn);
        }
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
