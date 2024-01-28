using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public class BonusSpawner : NetworkBehaviour
{
    [SerializeField] GameObject prefab;
    [SerializeField] float minSpawnTime = 32f;
    [SerializeField] float varSpawnTime = 64f;
    [SerializeField] AnimationCurve timeMultiplierWrtPlayerQuantity = AnimationCurve.EaseInOut(0f, 4f, 8f, .5f);

    //ParticleSystem ps;
    RoflBomb spawned = null;
    static EventHandler.EvtKind[] allKinds;
    private void Start()
    {
        if (allKinds == null)
            allKinds = System.Enum.GetValues(typeof(EventHandler.EvtKind)).Cast<EventHandler.EvtKind>().Skip(1).ToArray();

        //ps.Stop();
        nextTimeToSpawn = minSpawnTime + Random.value * varSpawnTime;
    }

    float nextTimeToSpawn = float.MaxValue;
    private void Update()
    {
        if (isServer)
        {
            if (spawned == null && nextTimeToSpawn == float.MaxValue)
            {
                var spawnDelay = minSpawnTime + Random.value * varSpawnTime;
                spawnDelay *= timeMultiplierWrtPlayerQuantity.Evaluate(EventManager.Instance?.PlayerCount ?? 0f);
                nextTimeToSpawn = Time.time + spawnDelay;
            }

            if (spawned == null && Time.time > nextTimeToSpawn)
            {
                Spawn();
            }
        }
    }

    [Server]
    private void Spawn()
    {
        var go = Instantiate(prefab, transform.position, Quaternion.identity);
        var bonus = go.GetComponent<RoflBomb>();
        bonus.kind = allKinds[Random.Range(0, allKinds.Length)];
        NetworkServer.Spawn(go);

        spawned = bonus;
        nextTimeToSpawn = float.MaxValue;
    }

}
