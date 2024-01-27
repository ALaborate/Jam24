using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class EventHandler : MonoBehaviour
{

    [SerializeField] Image eventPicture;
    [SerializeField] float fullImageTime = .5f;
    [SerializeField] float fadeOutTime = 1f;
    [SerializeField] List<Data> events = new List<Data>();



    public IReadOnlyList<Data> Events => events;

    // Start is called before the first frame update
    void Start()
    {
        eventPicture.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

    }

    [Client]
    public void TriggerEvent(EvtKind kind)
    {
        foreach (var evt in events)
        {
            if (evt.kind == kind)
            {
                evt.audioSrc.Play();
                StartCoroutine(TriggerEventEnding(evt));
                break;
            }
        }
    }

    private IEnumerator TriggerEventEnding(Data evt)
    {
        yield return new WaitForSeconds(evt.audioSrc.clip.length - evt.effectTimeOffset);
        eventPicture.gameObject.SetActive(true);
        eventPicture.sprite = evt.picture;
        eventPicture.color = Color.white;
        yield return new WaitForSeconds(fullImageTime);
        float fadeBegin = Time.time;
        while (Time.time - fadeBegin <= fadeOutTime)
        {
            eventPicture.color = Color.Lerp(Color.white, Color.clear, (Time.time - fadeBegin) / fadeOutTime);
            yield return null;
        }
        yield return null;
        eventPicture.gameObject.SetActive(false);
    }

    [System.Serializable]
    public class Data
    {
        public EvtKind kind;
        public AudioSource audioSrc;
        public Sprite picture;
        public float effectTimeOffset = -.4f;
        public float scoreMultiplier = 1f;
        public float EndingDelay => audioSrc.clip.length - effectTimeOffset;
    }

    public enum EvtKind
    {
        None,
        BuloVze,
        DekanuFacultetu,
        Krivosisi,
    }
}
