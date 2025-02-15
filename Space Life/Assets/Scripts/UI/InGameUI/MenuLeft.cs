using UnityEngine;

public class MenuLeft : MonoBehaviour
{
    // This is the parent of the menus.
    private Transform parent;

    public GameObject CurrentlyOpen { get; private set; }

    // Use this for initialization
    public void Start()
    {
        parent = this.gameObject.transform;

        AddMenu("ConstructionMenu");

        GameMenuManager.Instance.AddMenuItem("Build", OnButtonConstruction, 0);
    }

    public void OpenMenu(string menuName)
    {
        GameObject menu = parent.Find(menuName).gameObject;

        CloseMenu();

        menu.SetActive(true);
        CurrentlyOpen = menu;
        if (CurrentlyOpen.name == "ConstructionMenu")
        {
            WorldController.Instance.spawnInventoryController.SetUIVisibility(false);
        }
    }

    public void CloseMenu()
    {
        if (CurrentlyOpen != null)
        {
            CurrentlyOpen.SetActive(false);

            if (CurrentlyOpen.name == "ConstructionMenu")
            {
                WorldController.Instance.spawnInventoryController.SetUIVisibility(Settings.GetSetting("DialogBoxSettings_developerModeToggle", false));
            }

            CurrentlyOpen = null;
        }
    }

    // Use this function to add all the menus.
    private void AddMenu(string menuName)
    {
        GameObject tempGoObj;
        tempGoObj = (GameObject)Instantiate(Resources.Load("UI/MenuLeft/" + menuName));
        tempGoObj.name = menuName;
        tempGoObj.transform.SetParent(parent, false);
    }

    private void OnButtonConstruction()
    {
        if (CurrentlyOpen != null && CurrentlyOpen.gameObject.name == "ConstructionMenu")
        {
            CloseMenu();
        }
        else
        {
            OpenMenu("ConstructionMenu");
        }
    } 
}
