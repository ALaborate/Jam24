using UnityEngine;
using UnityEngine.Localization.PropertyVariants;

public class ForceLocalizationUpdate : MonoBehaviour
{

    private GameObjectLocalizer gameObjectLocalizer;

    private void Awake()
    {
        gameObjectLocalizer = GetComponent<GameObjectLocalizer>();
    }


    Coroutine delayedUpdateRoutine = null;
    private void OnEnable()
    {
        if(delayedUpdateRoutine != null)
            StopCoroutine(delayedUpdateRoutine);
        delayedUpdateRoutine = StartCoroutine(DelayedUpdateRoutine());
    }

    System.Collections.IEnumerator DelayedUpdateRoutine()
    {
        yield return UnityEngine.Localization.Settings.LocalizationSettings.InitializationOperation;
        if (gameObjectLocalizer)
            gameObjectLocalizer.ApplyLocaleVariant(UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale);
    }

}
