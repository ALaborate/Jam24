using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class GameModeController: NetworkBehaviour
{
    [Space]
    public int numberOfFeathers = 1;
    public GameObject featherPrefab;
    [Space]
    public GameObject bonusSpawnPrefab;
    public float bsMinHeight = 1f;
    public float bsMaxHeight = 3f;

    public abstract GameModeType Type { get; }

    [Server]
    public virtual void Activate() 
    {
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
            SceneManager.MoveGameObjectToScene(feather, gameObject.scene);
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
            SceneManager.MoveGameObjectToScene(bonusSpawn, gameObject.scene);
            NetworkServer.Spawn(bonusSpawn);
        }
    }

    public virtual bool FinishPredicate() => false;

}

public enum GameModeType
{
    ScoreCompete, SentryApocalypse,
}