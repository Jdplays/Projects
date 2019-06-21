using System.Collections.Generic;
using UnityEngine;

public class NestedObjectSpriteController : BaseSpriteController<NestedObject>
{
    private Dictionary<NestedObject, GameObject> powerStatusGameObjectMap;

    // Use this for initialization
    public NestedObjectSpriteController(World world) : base(world, "NestedObject")
    {
        // Instantiate our dictionary that tracks which GameObject is rendering which Tile data.
        powerStatusGameObjectMap = new Dictionary<NestedObject, GameObject>();

        // Register our callback so that our GameObject gets updated whenever
        // the tile's type changes.
        world.NestedObjectManager.Created += OnCreated;

        // Go through any EXISTING NestedObject (i.e. from a save that was loaded OnEnable) and call the OnCreated event manually.
        foreach (NestedObject nestedObject in world.NestedObjectManager)
        {
            OnCreated(nestedObject);
        }
    }

    public override void RemoveAll()
    {
        world.NestedObjectManager.Created -= OnCreated;

        foreach (NestedObject nestedObject in world.NestedObjectManager)
        {
            nestedObject.Changed -= OnChanged;
            nestedObject.Removed -= OnRemoved;
            nestedObject.IsOperatingChanged -= OnIsOperatingChanged;
        }

        foreach (NestedObject nestedObject in powerStatusGameObjectMap.Keys)
        {
            GameObject.Destroy(powerStatusGameObjectMap[nestedObject]);
        }
            
        powerStatusGameObjectMap.Clear();
        base.RemoveAll();
    }

    public Sprite GetSpriteForNestedObject(string type)
    {
        NestedObject proto = PrototypeManager.NestedObject.Get(type);
        string spriteName = proto.GetSpriteName();
        Sprite s = SpriteManager.GetSprite("NestedObject", spriteName + (proto.LinksToNeighbour != string.Empty && !proto.OnlyUseDefaultSpriteName ? "_" : string.Empty));

        return s;
    }

    public Sprite GetSpriteForNestedObject(NestedObject nestedObject)
    {
        string spriteName = nestedObject.GetSpriteName();

        if (nestedObject.LinksToNeighbour == string.Empty || nestedObject.OnlyUseDefaultSpriteName)
        {
            return SpriteManager.GetSprite("NestedObject", spriteName);
        }

        // Otherwise, the sprite name is more complicated.
        spriteName += "_";

        // Check for neighbours North, East, South, West, Northeast, Southeast, Southwest, Northwest
        int x = nestedObject.Tile.X;
        int y = nestedObject.Tile.Y;
        string suffix = string.Empty;

        suffix += GetSuffixForNeighbour(nestedObject, x, y + 1, nestedObject.Tile.Z, "N");
        suffix += GetSuffixForNeighbour(nestedObject, x + 1, y, nestedObject.Tile.Z, "E");
        suffix += GetSuffixForNeighbour(nestedObject, x, y - 1, nestedObject.Tile.Z, "S");
        suffix += GetSuffixForNeighbour(nestedObject, x - 1, y, nestedObject.Tile.Z, "W");

        // Now we check if we have the neighbours in the cardinal directions next to the respective diagonals
        // because pure diagonal checking would leave us with diagonal walls and stockpiles, which make no sense.
        suffix += GetSuffixForDiagonalNeighbour(suffix, "N", "E", nestedObject, x + 1, y + 1, nestedObject.Tile.Z);
        suffix += GetSuffixForDiagonalNeighbour(suffix, "S", "E", nestedObject, x + 1, y - 1, nestedObject.Tile.Z);
        suffix += GetSuffixForDiagonalNeighbour(suffix, "S", "W", nestedObject, x - 1, y - 1, nestedObject.Tile.Z);
        suffix += GetSuffixForDiagonalNeighbour(suffix, "N", "W", nestedObject, x - 1, y + 1, nestedObject.Tile.Z);

        // For example, if this object has all eight neighbours of
        // the same type, then the string will look like:
        //       Wall_NESWneseswnw
        return SpriteManager.GetSprite("NestedObject", spriteName + suffix);
    }

    protected override void OnCreated(NestedObject nestedObject)
    {
        // FIXME: Does not consider rotated objects
        GameObject obj_go = new GameObject();

        // Add our tile/GO pair to the dictionary.
        objectGameObjectMap.Add(nestedObject, obj_go);

        // FIXME: This hardcoding is not ideal!
        if (nestedObject.HasTypeTag("Door"))
        {
            // Check to see if we actually have a wall north/south, and if so
            // set the NestedObject verticalDoor flag to true.
            Tile northTile = world.GetTileAt(nestedObject.Tile.X, nestedObject.Tile.Y + 1, nestedObject.Tile.Z);
            Tile southTile = world.GetTileAt(nestedObject.Tile.X, nestedObject.Tile.Y - 1, nestedObject.Tile.Z);

            if (northTile != null && southTile != null && northTile.NestedObject != null && southTile.NestedObject != null &&
                northTile.NestedObject.HasTypeTag("Wall") && southTile.NestedObject.HasTypeTag("Wall"))
            {
                nestedObject.VerticalDoor = true;
            }
        }

        SpriteRenderer sr = obj_go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSpriteForNestedObject(nestedObject);
        sr.sortingLayerName = "NestedObject";
        sr.color = nestedObject.Tint;

        obj_go.name = nestedObject.Type + "_" + nestedObject.Tile.X + "_" + nestedObject.Tile.Y;
        obj_go.transform.position = nestedObject.Tile.Vector3 + ImageUtils.SpritePivotOffset(sr.sprite, nestedObject.Rotation);
        obj_go.transform.Rotate(0, 0, nestedObject.Rotation);
        obj_go.transform.SetParent(objectParent.transform, true);

        sr.sortingOrder = Mathf.RoundToInt(obj_go.transform.position.y * -1);

        if (nestedObject.PowerConnection != null && nestedObject.PowerConnection.IsPowerConsumer)
        {
            GameObject powerGameObject = new GameObject();
            powerStatusGameObjectMap.Add(nestedObject, powerGameObject);
            powerGameObject.transform.parent = obj_go.transform;
            powerGameObject.transform.position = obj_go.transform.position;

            SpriteRenderer powerSpriteRenderer = powerGameObject.AddComponent<SpriteRenderer>();
            powerSpriteRenderer.sprite = GetPowerStatusSprite();
            powerSpriteRenderer.sortingLayerName = "Power";
            powerSpriteRenderer.color = Color.red;

            if (nestedObject.IsOperating)
            {
                powerGameObject.SetActive(false);
            }
            else
            {
                powerGameObject.SetActive(true);
            }
        }

        if (nestedObject.Animation != null)
        { 
            nestedObject.Animation.Renderer = sr;
        }

        // Register our callback so that our GameObject gets updated whenever
        // the object's into changes.
        nestedObject.Changed += OnChanged;
        nestedObject.Removed += OnRemoved;
        nestedObject.IsOperatingChanged += OnIsOperatingChanged;
    }

    protected override void OnChanged(NestedObject obj)
    {
        // Make sure the NestedObject's graphics are correct.
        if (objectGameObjectMap.ContainsKey(obj) == false)
        {
            Debug.ULogErrorChannel("NestedObjectSpriteController", "OnNestedObjectChanged -- trying to change visuals for NestedObject not in our map.");
            return;
        }

        GameObject obj_go = objectGameObjectMap[obj];

        if (obj.HasTypeTag("Door"))
        {
            // Check to see if we actually have a wall north/south, and if so
            // set the NestedObject verticalDoor flag to true.
            Tile northTile = world.GetTileAt(obj.Tile.X, obj.Tile.Y + 1, obj.Tile.Z);
            Tile southTile = world.GetTileAt(obj.Tile.X, obj.Tile.Y - 1, obj.Tile.Z);
            Tile eastTile = world.GetTileAt(obj.Tile.X + 1, obj.Tile.Y, obj.Tile.Z);
            Tile westTile = world.GetTileAt(obj.Tile.X - 1, obj.Tile.Y, obj.Tile.Z);

            if (northTile != null && southTile != null && northTile.NestedObject != null && southTile.NestedObject != null &&
                northTile.NestedObject.HasTypeTag("Wall") && southTile.NestedObject.HasTypeTag("Wall"))
            {
                obj.VerticalDoor = true;
            }
            else if (eastTile != null && westTile != null && eastTile.NestedObject != null && westTile.NestedObject != null &&
                eastTile.NestedObject.HasTypeTag("Wall") && westTile.NestedObject.HasTypeTag("Wall"))
            {
                obj.VerticalDoor = false;
            }
        }

        // don't change sprites on NestedObject with animations
        if (obj.Animation != null)
        {
            obj.Animation.OnNestedObjectChanged();
            return;
        }
        
        obj_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForNestedObject(obj);
        obj_go.GetComponent<SpriteRenderer>().color = obj.Tint;
    }
        
    protected override void OnRemoved(NestedObject obj)
    {
        if (objectGameObjectMap.ContainsKey(obj) == false)
        {
            Debug.ULogErrorChannel("NestedObjectSpriteController", "OnNestedObjectRemoved -- trying to change visuals for NestedObject not in our map.");
            return;
        }

        obj.Changed -= OnChanged;
        obj.Removed -= OnRemoved;
        obj.IsOperatingChanged -= OnIsOperatingChanged;
        GameObject obj_go = objectGameObjectMap[obj];
        objectGameObjectMap.Remove(obj);
        GameObject.Destroy(obj_go);

        if (powerStatusGameObjectMap.ContainsKey(obj) == false)
        {
            return;
        }

        powerStatusGameObjectMap.Remove(obj);
    }
        
    private void OnIsOperatingChanged(NestedObject nestedObject)
    {
        if (nestedObject == null)
        {
            return;
        }

        if (powerStatusGameObjectMap.ContainsKey(nestedObject) == false)
        {
            return;
        }

        GameObject powerGameObject = powerStatusGameObjectMap[nestedObject];
        if (nestedObject.IsOperating)
        {
            powerGameObject.SetActive(false);
        }
        else
        {
            powerGameObject.SetActive(true);
        }
    }

    private string GetSuffixForNeighbour(NestedObject obj, int x, int y, int z, string suffix)
    {
         Tile t = world.GetTileAt(x, y, z);
         if (t != null && t.NestedObject != null && t.NestedObject.LinksToNeighbour == obj.LinksToNeighbour)
         {
             return suffix;
         }

        return string.Empty;
    }

    private string GetSuffixForDiagonalNeighbour(string suffix, string coord1, string coord2, NestedObject obj, int x, int y, int z)
    {
        if (suffix.Contains(coord1) && suffix.Contains(coord2))
        {
            return GetSuffixForNeighbour(obj, x, y, z, coord1.ToLower() + coord2.ToLower());
        }

        return string.Empty;
    }

    private Sprite GetPowerStatusSprite()
    {
        return SpriteManager.GetSprite("Power", "PowerIcon");
    }
}
