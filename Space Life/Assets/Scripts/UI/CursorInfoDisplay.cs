﻿using System.Collections.Generic;
using SpaceLife.Jobs;
using UnityEngine;

public class CursorInfoDisplay
{
    private MouseController mc;
    private BuildModeController bmc;
    private int validPostionCount;
    private int invalidPositionCount;

    public CursorInfoDisplay(MouseController mouseController, BuildModeController buildModeController)
    {
        mc = mouseController;
        bmc = buildModeController;
    }

    public string MousePosition(Tile t)
    {
        string x = string.Empty;
        string y = string.Empty;

        if (t != null)
        {
            x = t.X.ToString();
            y = t.Y.ToString();

            return "X:" + x + " Y:" + y;
        }
        else
        {
            return string.Empty;
        }
    }

    public void GetPlacementValidationCounts()
    {
        validPostionCount = invalidPositionCount = 0;

        for (int i = 0; i < mc.GetDragObjects().Count; i++)
        {
            Tile t1 = GetTileUnderDrag(mc.GetDragObjects()[i].transform.position);
            if (World.Current.NestedObjectManager.IsPlacementValid(bmc.buildModeType, t1) && t1.PendingBuildJob == null)
            {
                validPostionCount++;
            }
            else
            {
                invalidPositionCount++;
            }
        }
    }

    public string ValidBuildPositionCount()
    {
        return validPostionCount.ToString();
    }

    public string InvalidBuildPositionCount()
    {
        return invalidPositionCount.ToString();
    }

    public string GetCurrentBuildRequirements()
    {
        string temp = string.Empty;
        Dictionary<string, RequestedItem> items = PrototypeManager.NestedObjectConstructJob.Get(bmc.buildModeType).RequestedItems;
        foreach (RequestedItem item in items.Values)
        {
            string requiredMaterialCount = (item.MinAmountRequested * validPostionCount).ToString();
            if (items.Count > 1)
            {
                return temp += requiredMaterialCount + " " + item.Type + "\n";
            }
            else
            {
                return temp += requiredMaterialCount + " " + item.Type;
            }
        }

        return "NestedObjectJobPrototypes is null";
    }

    private Tile GetTileUnderDrag(Vector3 gameObject_Position)
    {
        return WorldController.Instance.GetTileAtWorldCoord(gameObject_Position);
    }
}
