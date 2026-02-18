using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public abstract class GameModeController : MonoBehaviour
{
    [SerializeField] SpawnInfo feathers;
    [SerializeField] SpawnInfo bonusSpawners;
    [SerializeField] SpawnInfo sentries;


    public abstract GameModeType Type { get; }

    [Server]
    public virtual void Activate()
    {
        ActuateSpawning(feathers, gameObject.scene);
        ActuateSpawning(bonusSpawners, gameObject.scene);
        ActuateSpawning(sentries, gameObject.scene);
    }

    protected virtual void OnValidate()
    {
        feathers.Validate();
        bonusSpawners.Validate();
        sentries.Validate();
    }

    static List<GameObject> _spawnBuffer = new(10);
    private static void ActuateSpawning(SpawnInfo info, Scene targetScene)
    {
        _spawnBuffer.Clear();
        GameObject.FindGameObjectsWithTag(info.tag, _spawnBuffer);
        var number = info.number;
        if(number == -1)
            number = _spawnBuffer.Count;

        for (int i = 0; i < number; i++)
        {
            var spawnPos = Vector3.up * 5f;

            if (_spawnBuffer.Count > 0)
            {
                var spawnInx = Random.Range(0, _spawnBuffer.Count);
                spawnPos = _spawnBuffer[spawnInx].transform.position;
                if (info.dontRepeat)
                    _spawnBuffer.RemoveAt(spawnInx);
            }

            spawnPos = spawnPos + Random.onUnitSphere * info.randPos;

            if (float.IsFinite(info.minHeight + info.maxHeight))
            {
                if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit))
                {
                    if (hit.point != Vector3.zero)
                    {
                        spawnPos = hit.point + Vector3.up * Random.Range(info.minHeight, info.maxHeight);
                    }
                }
            }

            var instance = Instantiate(info.prefab, spawnPos, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(instance, targetScene);
            NetworkServer.Spawn(instance);
        }

    }

    public virtual bool FinishPredicate() => false;

    [System.Serializable]
    public class SpawnInfo
    {
        public GameObject prefab;
        public string tag;
        public int number = 1;
        public float minHeight = float.NaN;
        public float maxHeight = float.NaN;
        public float randPos = 0f;
        public bool dontRepeat = true;

        public void Validate()
        {
#if UNITY_EDITOR
            var tValue = tag;
            if (!UnityEditorInternal.InternalEditorUtility.tags.Contains(tag))
            {
                int min = int.MaxValue;
                var tFound = string.Empty;
                foreach (var item in UnityEditorInternal.InternalEditorUtility.tags)
                {
                    var dist = Levenstein(tValue, item);
                    if (dist < min)
                    {
                        tFound = item;
                        min = dist;
                    }
                }

                if(min <= 3)
                    tag = tFound;
            } 
#endif
        }


        public static int Levenstein(string s, string t)
        {
        //https://www.dotnetperls.com/levenshtein
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0)
                return m;
            if (m == 0)
                return n;

            //init arrays
            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Mathf.Min(
                    Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

    }

}

public enum GameModeType
{
    ScoreCompete, SentryApocalypse,
}