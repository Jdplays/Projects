using System;

public class ContextMenuAction
{    
    public Action<ContextMenuAction, Character> Action;
    public string Parameter;

    public bool RequireCharacterSelected { get; set; }

    public string Text { get; set; }

    public void OnClick(MouseController mouseController)
    {
        if (Action != null)
        {
            if (RequireCharacterSelected)
            {
                if (mouseController.IsCharacterSelected())
                {
                    ISelectable actualSelection = mouseController.mySelection.GetSelectedStuff();
                    Action(this, actualSelection as Character);
                }
            }
            else
            {
                Action(this, null);
            }
        }
    }
}