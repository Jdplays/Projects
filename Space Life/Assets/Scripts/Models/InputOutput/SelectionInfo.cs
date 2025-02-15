using System.Collections.Generic;

public class SelectionInfo
{
    private List<ISelectable> stuffInTile;
    private int selectedIndex = 0;

    public SelectionInfo(Tile t)
    {
        Tile = t;

        BuildStuffInTile();
        SelectFirstStuff();
    }

    public Tile Tile
    {
        get;
        protected set;
    }

    public void BuildStuffInTile()
    {
        // Make sure stuffInTile is big enough to handle all the characters, plus the 3 extra values.
        stuffInTile = new List<ISelectable>();

        // Copy the character references.
        for (int i = 0; i < Tile.Characters.Count; i++)
        {
            stuffInTile.Add(Tile.Characters[i]);
        }

        // Now assign references to the other three sub-selections available.
        stuffInTile.Add(Tile.NestedObject);
        stuffInTile.Add(Tile.Inventory);
        stuffInTile.Add(Tile.PendingBuildJob);
        stuffInTile.Add(Tile);
    }

    public void SelectFirstStuff()
    {
        if (stuffInTile[selectedIndex] == null)
        {
            SelectNextStuff();
        }
    }

    public void SelectNextStuff()
    {
        do
        {
            selectedIndex = (selectedIndex + 1) % stuffInTile.Count;
        }
        while (stuffInTile[selectedIndex] == null);
    }

    public ISelectable GetSelectedStuff()
    {
        return stuffInTile[selectedIndex];
    }

    public bool IsCharacterSelected()
    {
        ISelectable actualSelection = stuffInTile[selectedIndex];
        return actualSelection is Character;
    }
}
