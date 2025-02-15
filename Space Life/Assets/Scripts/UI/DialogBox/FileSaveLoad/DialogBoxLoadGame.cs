using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class DialogBoxLoadGame : DialogBoxLoadSaveGame
{
    public bool pressedDelete;
    private Component fileItem;

    public override void ShowDialog()
    {
        base.ShowDialog();
        DialogListItem[] listItems = GetComponentsInChildren<DialogListItem>();
        foreach (DialogListItem listItem in listItems)
        {
            listItem.doubleclick = OkayWasClicked;
        }
    }

    public void SetFileItem(Component item)
    {
        fileItem = item;
    }

    public void SetButtonLocation(Component item)
    {
        GameObject go = GameObject.FindGameObjectWithTag("DeleteButton");
        go.transform.position = new Vector3(item.transform.position.x + 140f, item.transform.position.y - 8f);
    }

    public void OkayWasClicked()
    {
        string fileName = gameObject.GetComponentInChildren<InputField>().text;

        if (fileName == string.Empty)
        {
            DialogBoxManager dbm = GameObject.Find("Dialog Boxes").GetComponent<DialogBoxManager>();
            dbm.dialogBoxPromptOrInfo.SetAsInfo("message_file_needed_for_load");
            dbm.dialogBoxPromptOrInfo.ShowDialog();
            return;
        }

        // TODO: Is the filename valid?  I.E. we may want to ban path-delimiters (/ \ or :) and
        // maybe periods?      ../../some_important_file

        // Right now fileName is just what was in the dialog box.  We need to pad this out to the full
        // path, plus an extension!
        // In the end, we're looking for something that's going to be similar to this (depending on OS)
        //    C:\Users\Quill18\ApplicationData\MyCompanyName\MyGameName\Saves\SaveGameName123.sav

        // Application.persistentDataPath == C:\Users\<username>\ApplicationData\MyCompanyName\MyGameName\
        string saveDirectoryPath = WorldController.Instance.FileSaveBasePath();

        EnsureDirectoryExists(saveDirectoryPath);

        string filePath = System.IO.Path.Combine(saveDirectoryPath, fileName + ".sav");

        // At this point, filePath should look very much like
        //     C:\Users\Quill18\ApplicationData\MyCompanyName\MyGameName\Saves\SaveGameName123.sav
        if (File.Exists(filePath) == false)
        {
            //// TODO: Do file overwrite dialog box.

            DialogBoxManager dbm = GameObject.Find("Dialog Boxes").GetComponent<DialogBoxManager>();
            dbm.dialogBoxPromptOrInfo.SetAsInfo("message_file_doesn't_exist");
            dbm.dialogBoxPromptOrInfo.ShowDialog();
            return;
        }

        CloseDialog();

        LoadWorld(filePath);
    }

    public override void CloseDialog()
    {
        GameObject go = GameObject.FindGameObjectWithTag("DeleteButton");
        go.GetComponent<Image>().color = new Color(255, 255, 255, 0);
        pressedDelete = false;
        base.CloseDialog();
    }

    public void DeleteFile()
    {
        string fileName = gameObject.GetComponentInChildren<InputField>().text;

        string saveDirectoryPath = WorldController.Instance.FileSaveBasePath();

        EnsureDirectoryExists(saveDirectoryPath);

        string filePath = System.IO.Path.Combine(saveDirectoryPath, fileName + ".sav");

        if (File.Exists(filePath) == false)
        {
            DialogBoxManager dbm = GameObject.Find("Dialog Boxes").GetComponent<DialogBoxManager>();
            dbm.dialogBoxPromptOrInfo.SetAsInfo("message_file_doesn't_exist");
            return;
        }

        File.Delete(filePath);

        gameObject.GetComponentInChildren<InputField>().text = string.Empty;

        CloseDialog();
        ShowDialog();
    }

    public void DeleteWasClicked()
    {
        string fileName = gameObject.GetComponentInChildren<InputField>().text;

        DialogBoxManager dbm = GameObject.Find("Dialog Boxes").GetComponent<DialogBoxManager>();
        dbm.dialogBoxPromptOrInfo.Closed = () =>
        {
            if (dbm.dialogBoxPromptOrInfo.Result == DialogBoxResult.Yes)
            {
                DeleteFile();
            }
        };
        dbm.dialogBoxPromptOrInfo.SetPrompt("Are you sure you want to delete this file?", fileName);
        dbm.dialogBoxPromptOrInfo.SetButtons(DialogBoxResult.Yes, DialogBoxResult.No);
        dbm.dialogBoxPromptOrInfo.ShowDialog();
    }

    public void LoadWorld(string filePath)
    {
        // This function gets called when the user confirms a filename
        // from the load dialog box.

        // Get the file name from the save file dialog box.
        Debug.ULogChannel("DialogBoxLoadGame", "LoadWorld button was clicked.");

        DialogBoxManager dbm = GameObject.Find("Dialog Boxes").GetComponent<DialogBoxManager>();
        dbm.dialogBoxPromptOrInfo.SetPrompt("Woruld you like to save before loading a new game?");
        dbm.dialogBoxPromptOrInfo.ShowDialog();

        WorldController.Instance.LoadWorld(filePath);
    }

    private void Update()
    {
        if (pressedDelete)
        {
            SetButtonLocation(fileItem);
        }
    }
}
