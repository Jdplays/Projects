using System.IO;
using UnityEngine;

namespace SpaceLife.Localization
{

    [AddComponentMenu("Localization/Localization Loader")]
    public class LocalizationLoader : MonoBehaviour
    {
        public void UpdateLocalizationTable()
        {
            LoadLocalizationInDirectory(Application.streamingAssetsPath);

            foreach (DirectoryInfo mod in WorldController.Instance.modsManager.GetMods())
            {
                LoadLocalizationInDirectory(mod.FullName);
            }

            string lang = Settings.GetSettingWithOverwrite("localization", "en_US");

            LocalizationTable.currentLanguage = lang;

            LocalizationTable.LoadingLanguagesFinished();
        }

        private void LoadLocalizationInDirectory(string path)
        {
            string filePath = Path.Combine(path, "Localization");

            if (Directory.Exists(filePath) == false)
            {
                return;
            }

            foreach (string file in Directory.GetFiles(filePath, "*.lang"))
            {
                LocalizationTable.LoadLocalizationFile(file);

                Debug.ULogChannel("LocalizationLoader", "Loaded localization at path: " + file);
            }
        }

        private void Awake()
        {
            if (LocalizationTable.initialized)
            {
                return;
            }

            UpdateLocalizationTable();
        }
    }
}
