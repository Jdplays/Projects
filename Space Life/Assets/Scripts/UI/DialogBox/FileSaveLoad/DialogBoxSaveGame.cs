using System.Collections;
using System.IO;
using System.Threading;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;

public class DialogBoxSaveGame : DialogBoxLoadSaveGame
{
    public override void ShowDialog()
    {
        base.ShowDialog();
        DialogListItem[] listItems = GetComponentsInChildren<DialogListItem>();
        foreach (DialogListItem listItem in listItems)
        {
            listItem.doubleclick = OkayWasClicked;
        }
    }

    public void OkayWasClicked()
    {
        StartCoroutine(OkayWasClickedCoroutine());
    }

    public IEnumerator OkayWasClickedCoroutine()
    {
        bool isOkToSave = true;

        // TODO:
        // check to see if the file already exists
        // if so, ask for overwrite confirmation.
        string fileName = gameObject.GetComponentInChildren<InputField>().text;

        // TODO: Is the filename valid?  I.E. we may want to ban path-delimiters (/ \ or :) and 
        //// maybe periods?      ../../some_important_file

        DialogBoxManager dbm = GameObject.Find("Dialog Boxes").GetComponent<DialogBoxManager>();

        if (fileName == string.Empty)
        {
            dbm.dialogBoxPromptOrInfo.SetAsInfo("message_name_or_file_needed_for_save");
            dbm.dialogBoxPromptOrInfo.ShowDialog();
            yield break;
        }

        // Right now fileName is just what was in the dialog box.  We need to pad this out to the full
        // path, plus an extension!
        // In the end, we're looking for something that's going to be similar to this (depending on OS)
        //    C:\Users\Quill18\ApplicationData\MyCompanyName\MyGameName\Saves\SaveGameName123.sav

        // Application.persistentDataPath == C:\Users\<username>\ApplicationData\MyCompanyName\MyGameName\
        string filePath = System.IO.Path.Combine(WorldController.Instance.FileSaveBasePath(), fileName + ".sav");

        // At this point, filePath should look very much like
        ////     C:\Users\Quill18\ApplicationData\MyCompanyName\MyGameName\Saves\SaveGameName123.sav

        if (File.Exists(filePath) == true)
        {
            isOkToSave = false;

            dbm.dialogBoxPromptOrInfo.SetPrompt("This file already exists, do you want to overwrite it? ", new string[] { fileName });
            dbm.dialogBoxPromptOrInfo.SetButtons(DialogBoxResult.Yes, DialogBoxResult.No);

            dbm.dialogBoxPromptOrInfo.Closed = () =>
            {
                if (dbm.dialogBoxPromptOrInfo.Result == DialogBoxResult.Yes)
                {
                    isOkToSave = true;
                }
            };

            dbm.dialogBoxPromptOrInfo.ShowDialog();

            if (!isOkToSave)
            {
                while (dbm.dialogBoxPromptOrInfo.gameObject.activeSelf)
                {
                    yield return null;
                }
            }
        }

        if (isOkToSave)
        {
            dbm.dialogBoxPromptOrInfo.SetPrompt("Saving...");
            dbm.dialogBoxPromptOrInfo.ShowDialog();

            yield return new WaitForSecondsRealtime(.05f);

            SaveWorld(filePath);

            dbm.dialogBoxPromptOrInfo.CloseDialog();

            this.CloseDialog();

            dbm.dialogBoxPromptOrInfo.SetAsInfo("Game Saved!");
            dbm.dialogBoxPromptOrInfo.ShowDialog();

            while (dbm.dialogBoxPromptOrInfo.gameObject.activeSelf)
            {
                yield return null;
            }
        }
    }

    public void SaveWorld(string filePath)
    {
        // This function gets called when the user confirms a filename
        // from the save dialog box.

        // Get the file name from the save file dialog box.
        Debug.ULogChannel("DialogBoxSaveGame", "SaveWorld button was clicked.");

        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextWriter writer = new StringWriter();
        serializer.Serialize(writer, WorldController.Instance.World);
        writer.Close();

        // Leaving this unchanged as UberLogger doesn't handle multi-line messages well.
        Debug.Log(writer.ToString());

        // PlayerPrefs.SetString("SaveGame00", writer.ToString());

        // Make sure the save folder exists.
        if (Directory.Exists(WorldController.Instance.FileSaveBasePath()) == false)
        {
            // NOTE: This can throw an exception if we can't create the folder,
            // but why would this ever happen? We should, by definition, have the ability
            // to write to our persistent data folder unless something is REALLY broken
            // with the computer/device we're running on.
            Directory.CreateDirectory(WorldController.Instance.FileSaveBasePath());
        }

        // Launch saving operation in a separate thread.
        // This reduces lag while saving by a little bit.
        Thread t = new Thread(new ThreadStart(delegate { SaveWorldToHdd(filePath, writer); }));
        t.Start();
    }

    /// <summary>
    /// Create/overwrite the save file with the xml text.
    /// </summary>
    /// <param name="filePath">Full path to file.</param>
    /// <param name="writer">TextWriter that contains serialized World data.</param>
    private void SaveWorldToHdd(string filePath, TextWriter writer)
    {
        File.WriteAllText(filePath, writer.ToString());
    }
}