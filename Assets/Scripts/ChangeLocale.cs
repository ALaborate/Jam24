using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class ChangeLocale : MonoBehaviour
{

    Button btn;
    System.Collections.IEnumerator Start()
    {
        if (!TryGetComponent(out btn))
        {
            Destroy(this);
            Debug.LogError("No localization button found", gameObject);
            yield break;
        }
        yield return LocalizationSettings.InitializationOperation;
        btn.onClick.AddListener(OnChangeClicked);
    }

    void OnChangeClicked()
    {
        var count = LocalizationSettings.AvailableLocales.Locales.Count;
        var prev = LocalizationSettings.AvailableLocales.Locales.IndexOf(LocalizationSettings.SelectedLocale);

        var curr = (prev + 1) % count;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[curr];
    }
}
