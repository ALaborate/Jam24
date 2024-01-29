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
    static Dictionary<EventHandler.EvtKind, int> kindCount = new Dictionary<EventHandler.EvtKind, int>();
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

    List<EventHandler.EvtKind> kindsToSpawn = new();
    [Server]
    private void Spawn()
    {
        var go = Instantiate(prefab, transform.position, Quaternion.identity);
        var bonus = go.GetComponent<RoflBomb>();

        if(kindCount.Count < allKinds.Length)
        {
            foreach (var item in allKinds)
            {
                if (!kindCount.ContainsKey(item))
                    kindCount.Add(item, 0);
            }
        }

        kindsToSpawn.Clear();
        var minCount = kindCount.Values.Min();
        foreach (var item in kindCount)
        {
            if (item.Value == minCount)
                kindsToSpawn.Add(item.Key);
        }

        bonus.kind = kindsToSpawn[Random.Range(0, kindsToSpawn.Count)];

        NetworkServer.Spawn(go);

        spawned = bonus;
        kindCount[bonus.kind] = kindCount.ContainsKey(bonus.kind) ? kindCount[bonus.kind] + 1 : 1;
        nextTimeToSpawn = float.MaxValue;
    }

}
