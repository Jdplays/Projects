using SpaceLife.Localization;
using UnityEngine;
using UnityEngine.UI;

public class LanguageDropdownUpdater : MonoBehaviour
{
    private void Start()
    {
        UpdateLanguageDropdown();
        LocalizationTable.CBLocalizationFilesChanged += UpdateLanguageDropdown;
    }

    private void UpdateLanguageDropdown()
    {
        Dropdown dropdown = GetComponent<Dropdown>();

        string[] languages = LocalizationTable.GetLanguages();

        dropdown.options.RemoveRange(0, dropdown.options.Count);

        foreach (string lang in languages)
        {
            dropdown.options.Add(new Dropdown.OptionData(lang));
        }

        for (int i = 0; i < languages.Length; i++)
        {
            if (languages[i] == LocalizationTable.currentLanguage)
            {
                // This tbh quite stupid looking code is necessary due to a Unity (optimization?, bug(?)).
                dropdown.value = i + 1;
                dropdown.value = i;
            }
        }

        // Set scroll sensitivity based on the save-item count.
        dropdown.template.GetComponent<ScrollRect>().scrollSensitivity = dropdown.options.Count / 3;
    }
}
