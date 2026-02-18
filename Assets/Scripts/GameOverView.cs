using UnityEngine;

public class GameOverView : MonoBehaviour {
    [SerializeField] RectTransform gameOverParent;
    //[SerializeField] RectTransform gameOverList;
    //[SerializeField] GameObject scoreItemPrefab;
    private void Start()
    {
        Prepare();
    }
    public void Prepare()
    {
        //gameOverList.gameObject.SetActive(false);
        gameOverParent.gameObject.SetActive(false);
    }
    public void Trigger()
    {
        gameOverParent.gameObject.SetActive(true);
    }


}
