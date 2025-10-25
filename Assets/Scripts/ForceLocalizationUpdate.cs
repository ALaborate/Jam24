using UnityEngine;
using UnityEngine.Localization.PropertyVariants;

public class ForceLocalizationUpdate : MonoBehaviour
{

    private GameObjectLocalizer gameObjectLocalizer;

    private void Awake()
    {
        gameObjectLocalizer = GetComponent<GameObjectLocalizer>();
    }

    private void OnEnable()
    {
        if (gameObjectLocalizer)
            gameObjectLocalizer.ApplyLocaleVariant(UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale);
    }

}
