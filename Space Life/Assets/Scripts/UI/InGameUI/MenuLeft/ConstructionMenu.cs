using System.Collections.Generic;
using System.Linq;
using SpaceLife.Localization;
using SpaceLife.Rooms;
using UnityEngine;
using UnityEngine.UI;

public class ConstructionMenu : MonoBehaviour
{
    private const string LocalizationDeconstruct = "Deconstruct NestedObject";

    private List<GameObject> furnitureItems;
    private List<GameObject> roomBehaviorItems;
    private List<GameObject> utilityItems;
    private List<GameObject> tileItems;
    private List<GameObject> taskItems;

    private bool showAllFurniture;

    private MenuLeft menuLeft;

    public void RebuildMenuButtons(bool showAllFurniture = false)
    {
        foreach (GameObject gameObject in furnitureItems)
        {
            Destroy(gameObject);
        }

        foreach (GameObject gameObject in roomBehaviorItems)
        {
            Destroy(gameObject);
        }

        foreach (GameObject gameObject in utilityItems)
        {
            Destroy(gameObject);
        }

        foreach (GameObject gameObject in tileItems)
        {
            Destroy(gameObject);
        }

        foreach (GameObject gameObject in taskItems)
        {
            Destroy(gameObject);
        }

        this.showAllFurniture = showAllFurniture;

        RenderDeconstructButton();
        RenderRoomBehaviorButtons();
        RenderTileButtons();
        RenderNestedObjectButtons();
        RenderUtilityButtons();
    }

    public void FilterTextChanged(string filterText)
    {
        Transform contentTransform = this.transform.Find("Scroll View").Find("Viewport").Find("Content");

        List<Transform> childs = contentTransform.Cast<Transform>().ToList();

        foreach (Transform child in childs)
        {
            Text buttonText = child.gameObject.transform.GetComponentInChildren<Text>();

            string buildableName = buttonText.text;

            bool nameMatchFilter = string.IsNullOrEmpty(filterText) || buildableName.ToLower().Contains(filterText.ToLower());

            child.gameObject.SetActive(nameMatchFilter);
        }
    }

    private void Start()
    {
        menuLeft = this.transform.GetComponentInParent<MenuLeft>();

        this.transform.Find("Close Button").GetComponent<Button>().onClick.AddListener(delegate
        {
            menuLeft.CloseMenu();
        });

        RenderDeconstructButton();
        RenderRoomBehaviorButtons();
        RenderTileButtons();
        RenderNestedObjectButtons();
        RenderUtilityButtons();

        InputField filterField = GetComponentInChildren<InputField>();
        KeyboardManager.Instance.RegisterModalInputField(filterField);
    }

    private void RenderNestedObjectButtons()
    {
        furnitureItems = new List<GameObject>();

        UnityEngine.Object buttonPrefab = Resources.Load("UI/MenuLeft/ConstructionMenu/Button");
        Transform contentTransform = this.transform.Find("Scroll View").Find("Viewport").Find("Content");

        BuildModeController buildModeController = WorldController.Instance.buildModeController;

        foreach (string nestedObjectKey in PrototypeManager.NestedObject.Keys)
        {
            if (PrototypeManager.NestedObject.Get(nestedObjectKey).HasTypeTag("Non-buildable") && showAllFurniture == false)
            {
                continue;
            }

            GameObject gameObject = (GameObject)Instantiate(buttonPrefab);
            gameObject.transform.SetParent(contentTransform);
            furnitureItems.Add(gameObject);

            NestedObject proto = PrototypeManager.NestedObject.Get(nestedObjectKey);
            string objectId = nestedObjectKey;

            gameObject.name = "Button - Build " + objectId;

            gameObject.transform.GetComponentInChildren<Text>().text = proto.GetName();

            Button button = gameObject.GetComponent<Button>();

            button.onClick.AddListener(delegate
            {
                buildModeController.SetMode_BuildNestedObject(objectId);
                menuLeft.CloseMenu();
            });

            // http://stackoverflow.com/questions/1757112/anonymous-c-sharp-delegate-within-a-loop
            string nestedObject = nestedObjectKey;
            LocalizationTable.CBLocalizationFilesChanged += delegate
            {
                gameObject.transform.GetComponentInChildren<TextLocalizer>().formatValues = new string[] { LocalizationTable.GetLocalization(PrototypeManager.NestedObject.Get(nestedObject).GetName()) };
            };

            Image image = gameObject.transform.GetChild(0).GetComponentsInChildren<Image>().First();
            image.sprite = WorldController.Instance.nestedObjectSpriteController.GetSpriteForNestedObject(nestedObjectKey);
        }
    }

    private void RenderRoomBehaviorButtons()
    {
        roomBehaviorItems = new List<GameObject>();

        UnityEngine.Object buttonPrefab = Resources.Load("UI/MenuLeft/ConstructionMenu/Button");
        Transform contentTransform = this.transform.Find("Scroll View").Find("Viewport").Find("Content");

        BuildModeController buildModeController = WorldController.Instance.buildModeController;

        // For each furniture prototype in our world, create one instance
        // of the button to be clicked!
        foreach (string roomBehaviorKey in PrototypeManager.RoomBehavior.Keys)
        {
            if (PrototypeManager.RoomBehavior.Get(roomBehaviorKey).HasTypeTag("Non-buildable") && showAllFurniture == false)
            {
                continue;
            }

            GameObject gameObject = (GameObject)Instantiate(buttonPrefab);
            gameObject.transform.SetParent(contentTransform);
            roomBehaviorItems.Add(gameObject);

            RoomBehavior proto = PrototypeManager.RoomBehavior.Get(roomBehaviorKey);
            string objectId = roomBehaviorKey;

            gameObject.name = "Button - Designate " + objectId;

            gameObject.transform.GetComponentInChildren<Text>().text = proto.GetName();

            Button button = gameObject.GetComponent<Button>();

            button.onClick.AddListener(delegate
            {
                buildModeController.SetMode_DesignateRoomBehavior(objectId);
                menuLeft.CloseMenu();
            });

            // http://stackoverflow.com/questions/1757112/anonymous-c-sharp-delegate-within-a-loop
            string roomBehavior = roomBehaviorKey;
            LocalizationTable.CBLocalizationFilesChanged += delegate
            {
                gameObject.transform.GetComponentInChildren<TextLocalizer>().formatValues = new string[] { LocalizationTable.GetLocalization(PrototypeManager.RoomBehavior.Get(roomBehavior).LocalizationCode) };
            };

            Image image = gameObject.transform.GetChild(0).GetComponentsInChildren<Image>().First();
            image.sprite = SpriteManager.GetSprite("RoomBehavior", roomBehaviorKey);
        }
    }

    private void RenderUtilityButtons()
    {
        utilityItems = new List<GameObject>();

        UnityEngine.Object buttonPrefab = Resources.Load("UI/MenuLeft/ConstructionMenu/Button");
        Transform contentTransform = this.transform.Find("Scroll View").Find("Viewport").Find("Content");

        BuildModeController buildModeController = WorldController.Instance.buildModeController;

        // For each furniture prototype in our world, create one instance
        // of the button to be clicked!
        foreach (string utilityKey in PrototypeManager.Utility.Keys)
        {
            if (PrototypeManager.Utility.Get(utilityKey).HasTypeTag("Non-buildable") && showAllFurniture == false)
            {
                continue;
            }

            GameObject gameObject = (GameObject)Instantiate(buttonPrefab);
            gameObject.transform.SetParent(contentTransform);
            furnitureItems.Add(gameObject);

            Utility proto = PrototypeManager.Utility.Get(utilityKey);
            string objectId = utilityKey;

            gameObject.name = "Button - Build " + objectId;

            gameObject.transform.GetComponentInChildren<Text>().text = proto.LocalizationCode;

            Button button = gameObject.GetComponent<Button>();

            button.onClick.AddListener(delegate
            {
                buildModeController.SetMode_BuildUtility(objectId);
                menuLeft.CloseMenu();
            });

            // http://stackoverflow.com/questions/1757112/anonymous-c-sharp-delegate-within-a-loop
            string utility = utilityKey;
            LocalizationTable.CBLocalizationFilesChanged += delegate
            {
                gameObject.transform.GetComponentInChildren<Text>().text  = PrototypeManager.Utility.Get(utility).GetName();
            };

            Image image = gameObject.transform.GetChild(0).GetComponentsInChildren<Image>().First();
            image.sprite = WorldController.Instance.utilitySpriteController.GetSpriteForUtility(utilityKey);
        }
    }

    private void RenderTileButtons()
    {
        tileItems = new List<GameObject>();

        UnityEngine.Object buttonPrefab = Resources.Load("UI/MenuLeft/ConstructionMenu/Button");
        Transform contentTransform = this.transform.Find("Scroll View").Find("Viewport").Find("Content");

        BuildModeController buildModeController = WorldController.Instance.buildModeController;

        foreach (TileType item in PrototypeManager.TileType.Values)
        {
            TileType tileType = item;

            string key = tileType.Name;

            GameObject gameObject = (GameObject)Instantiate(buttonPrefab);
            gameObject.transform.SetParent(contentTransform);
            tileItems.Add(gameObject);

            gameObject.name = "Button - Build Tile " + key;

            gameObject.transform.GetComponentInChildren<Text>().text = key;

            Button button = gameObject.GetComponent<Button>();
            button.onClick.AddListener(delegate
            {
                buildModeController.SetModeBuildTile(tileType);
            });

            LocalizationTable.CBLocalizationFilesChanged += delegate
            {
                gameObject.transform.GetComponentInChildren<TextLocalizer>().formatValues = new string[] { LocalizationTable.GetLocalization(key) };
            };

            Image image = gameObject.transform.GetChild(0).GetComponentsInChildren<Image>().First();
            image.sprite = SpriteManager.GetSprite("Tile", tileType.Type);
        }
    }

    private void RenderDeconstructButton()
    {
        taskItems = new List<GameObject>();

        UnityEngine.Object buttonPrefab = Resources.Load("UI/MenuLeft/ConstructionMenu/Button");
        Transform contentTransform = this.transform.Find("Scroll View").Find("Viewport").Find("Content");

        BuildModeController buildModeController = WorldController.Instance.buildModeController;

        GameObject gameObject = (GameObject)Instantiate(buttonPrefab);
        gameObject.transform.SetParent(contentTransform);
        taskItems.Add(gameObject);

        gameObject.name = "Button - Deconstruct";

        gameObject.transform.GetComponentInChildren<Text>().text = LocalizationDeconstruct;

        Button button = gameObject.GetComponent<Button>();

        button.onClick.AddListener(delegate
        {
            buildModeController.SetMode_Deconstruct();
        });

        LocalizationTable.CBLocalizationFilesChanged += delegate
        {
            gameObject.transform.GetComponentInChildren<TextLocalizer>().formatValues = new string[] { LocalizationTable.GetLocalization(LocalizationDeconstruct) };
        };

        Image image = gameObject.transform.GetChild(0).GetComponentsInChildren<Image>().First();
        image.sprite = SpriteManager.GetSprite("UI", "Deconstruct");
    }
}
