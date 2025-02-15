using SpaceLife.Localization;
using UnityEngine;
using UnityEngine.UI;

public class DialogBoxJobList : DialogBox
{
    public GameObject JobListItemPrefab;
    public Transform JobList;

    public override void ShowDialog()
    {
        base.ShowDialog();

        // Localization
        string[] formatValues;
        formatValues = new string[0];
        int i = 0;

        foreach (Character character in World.Current.CharacterManager)
        {
            GameObject go = (GameObject)Instantiate(JobListItemPrefab, JobList);
            string jobDescription = character.GetJobDescription();
            go.GetComponentInChildren<Text>().text = string.Format("<b>{0}</b> - {1}", character.GetName(), jobDescription);

            JobListItem listItem = go.GetComponent<JobListItem>();
            listItem.character = character;
            listItem.currentColor = i % 2 == 0 ? ListPrimaryColor : ListSecondaryColor;

            go.GetComponent<Image>().color = listItem.currentColor;
            i++;
        }

        JobList.GetComponentInParent<ScrollRect>().scrollSensitivity = JobList.childCount / 2;

        JobList.GetComponent<AutomaticVerticalSize>().AdjustSize();
    }

    public override void CloseDialog()
    {
        // Clear out all the children of our job list
        while (JobList.childCount > 0)
        {
            Transform c = JobList.GetChild(0);
            c.SetParent(null);  // Become Batman
            Destroy(c.gameObject);
        }

        base.CloseDialog();
    }
}
