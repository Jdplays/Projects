using System.Collections;
using SpaceLife.Localization;
using UnityEngine;
using UnityEngine.UI;

/// Every frame, this script checks to see which tile
/// is under the mouse and then updates the GetComponent<Text>.text
/// parameter of the object it is attached to.
public class MouseOverNestedObjectTypeText : MonoBehaviour
{
    private Text text;
    private MouseController mouseController;

    // Use this for initialization.
    private void Start()
    {
        text = GetComponent<Text>();

        if (text == null)
        {
            Debug.ULogErrorChannel("MouseOver", "No 'Text' UI component on this object.");
            this.enabled = false;
            return;
        }

        mouseController = WorldController.Instance.mouseController;

        if (mouseController == null)
        {
            Debug.ULogErrorChannel("MouseOver", "How do we not have an instance of mouse controller?");
            return;
        }
    }

    // Update is called once per frame.
    private void Update()
    {
        Tile t = mouseController.GetMouseOverTile();
        string s = "NULL";

        if (t != null && t.NestedObject != null)
        {
            s = t.NestedObject.Name;
            text.text = "NestedObject" + ": " + s;
        }
        else
        {
            text.text = string.Empty;
        }
    }
}
