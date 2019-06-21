using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceLife.Localization;
using UnityEngine;
using UnityEngine.UI;

public class SelectionInfoTextField : MonoBehaviour
{
    public CanvasGroup canvasGroup;

    private MouseController mc;
    private Text txt;

    // Use this for initialization.
    private void Start()
    {
        mc = WorldController.Instance.mouseController;
        txt = GetComponent<Text>();
    }

    // Update is called once per frame.
    private void Update()
    {
        if (mc.mySelection == null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            return;
        }

        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        ISelectable actualSelection = mc.mySelection.GetSelectedStuff();

        string additionalInfoText = string.Join(Environment.NewLine, actualSelection.GetAdditionalInfo().ToArray());

        if (actualSelection.GetType() == typeof(Character))
        {
            // TODO: Change the hitpoint stuff.
            txt.text = 
                actualSelection.GetName() + "\n" + 
                actualSelection.GetDescription() + "\n" +
                actualSelection.GetJobDescription() + "\n" + 
                additionalInfoText;
        }
        else
        {
            List<string> internalInventories = new List<string>();

            if (actualSelection.GetInternalInventory() != null)
            {
                foreach (List<Inventory> inventories in actualSelection.GetInternalInventory().Values)
                {
                    foreach (Inventory inventory in inventories)
                    {
                        internalInventories.Add(inventory.Type + ": " + inventory.StackSize + "/" + inventory.MaxStackSize);
                    }
                }
            }
            else
            {
                internalInventories.Add(string.Empty);
            }

            // TODO: Change the hitpoint stuff.
            txt.text =
                actualSelection.GetName() + "\n" +
                actualSelection.GetDescription() + "\n" +
                actualSelection.GetStatus() + "\n" +
                additionalInfoText + "\n" + "\n" + 
                internalInventories;
        }
    }
}
