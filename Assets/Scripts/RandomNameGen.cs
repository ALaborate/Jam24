using System.Linq;
using UnityEngine;

public class RandomNameGen : MonoBehaviour
{
    private const string KEY = "PlayerName";
    private static readonly string[] prefixes = { "Shadow", "Iron", "Fire", "Storm", "Silent", "Dark", "Swift", "Golden", "Able", "Kilo", "Mega", "Femto", "Milli", "Femto", "Yocto", "Exa", "Ronna", "Quecto", "Atto" };
    private static readonly string[] suffixes = { "Wolf", "Blade", "Hunter", "Phoenix", "Raven", "Fang", "Dragon", "Knight", "Tiger", "Tango", "Foxtrot", "Roger", "Baker", "Charlie", "Mike", "Castle", "Yankee", "Bear", "Keeper" };

    private UnityEngine.UI.InputField playerNameField;

    private void Start()
    {
        if (!TryGetComponent(out playerNameField))
        {
            Debug.LogError($"Misplaced {nameof(RandomNameGen)} on {gameObject.name}", gameObject);
            Destroy(this);
        }
        else
        {
            playerNameField.onEndEdit.AddListener(OnFinishEdit);
            if (!PlayerPrefs.HasKey(KEY))
            {
                GenerateNickname();
                PlayerPrefs.SetString(KEY, playerNameField.text);
            }
            else
                playerNameField.text = PlayerPrefs.GetString(KEY);
        }
    }

    public void GenerateNickname()
    {
        string prefix = prefixes[Random.Range(0, prefixes.Length)];
        string suffix = suffixes[Random.Range(0, suffixes.Length)];
        playerNameField.text = prefix + suffix;
    }

    private bool Validate(string input)
    {
        return input.All(c => char.IsLetterOrDigit(c));
    }
    private void OnFinishEdit(string newName)
    {
        if (!Validate(newName))
            playerNameField.text = PlayerPrefs.GetString(KEY);
        else
        {
            PlayerPrefs.SetString(KEY, newName);
        }
    }
}