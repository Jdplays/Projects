using UnityEngine;
using UnityEngine.UI;

public class ContextMenuItem : MonoBehaviour
{
    public ContextMenu ContextMenu;
    public Text text;
    public ContextMenuAction Action;
    private MouseController mouseController;

    public void Start()
    {
        mouseController = WorldController.Instance.mouseController;
    }

    /// <summary>
    /// Builds the interface.
    /// </summary>
    public void BuildInterface()
    {
        text.text = Action.Text;
    }

    /// <summary>
    /// Raises the click event.
    /// </summary>
    public void OnClick()
    {
        if (Action != null)
        {
            Action.OnClick(mouseController);
        }

        if (ContextMenu != null)
        {
            ContextMenu.Close();
        }
    }
}
