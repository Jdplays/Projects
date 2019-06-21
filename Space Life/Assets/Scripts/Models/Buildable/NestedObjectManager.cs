using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

public class NestedObjectManager : IEnumerable<NestedObject>
{
    private List<NestedObject> nestedObjects;

    // A temporary list of all visible NestedObject. Gets updated when camera moves.
    private List<NestedObject> nestedObjectsVisible;

    // A temporary list of all invisible NestedObject. Gets updated when camera moves.
    private List<NestedObject> nestedObjectsInvisible;

    /// <summary>
    /// Initializes a new instance of the <see cref="NestedObjectManager"/> class.
    /// </summary>
    public NestedObjectManager()
    {
        nestedObjects = new List<NestedObject>();
        nestedObjectsVisible = new List<NestedObject>();
        nestedObjectsInvisible = new List<NestedObject>();
    }

    /// <summary>
    /// Occurs when a NestedObject is created.
    /// </summary>
    public event Action<NestedObject> Created;

    /// <summary>
    /// Creates a NestedObject with the given type and places it at the given tile.
    /// </summary>
    /// <returns>The NestedObject.</returns>
    /// <param name="type">The type of the NestedObject.</param>
    /// <param name="tile">The tile to place the NestedObject at.</param>
    /// <param name="doRoomFloodFill">If set to <c>true</c> do room flood fill.</param>
    /// /// <param name="rotation">The rotation applied to te NestedObject.</param>
    public NestedObject PlaceNestedObject(string type, Tile tile, bool doRoomFloodFill = true, float rotation = 0f)
    {
        if (PrototypeManager.NestedObject.Has(type) == false)
        {
            Debug.ULogErrorChannel("World", "NestedObjectPrototypes doesn't contain a proto for key: " + type);
            return null;
        }

        NestedObject obj = PrototypeManager.NestedObject.Get(type).Clone();
        obj.SetRotation(rotation);

        return PlaceNestedObject(obj, tile, doRoomFloodFill);
    }

    /// <summary>
    /// Places the given NestedObject prototype at the given tile.
    /// </summary>
    /// <returns>The NestedObject.</returns>
    /// <param name="prototype">The NestedObject prototype.</param>
    /// <param name="tile">The tile to place the NestedObject at.</param>
    /// <param name="doRoomFloodFill">If set to <c>true</c> do room flood fill.</param>
    public NestedObject PlaceNestedObject(NestedObject prototype, Tile tile, bool doRoomFloodFill = true)
    {
        NestedObject nestedObject = NestedObject.PlaceInstance(prototype, tile);

        if (nestedObject == null)
        {
            // Failed to place object -- most likely there was already something there.
            return null;
        }

        nestedObject.Removed += OnRemoved;

        nestedObjects.Add(nestedObject);
        nestedObjectsVisible.Add(nestedObject);

        // Do we need to recalculate our rooms?
        if (doRoomFloodFill && nestedObject.RoomEnclosure)
        {
            World.Current.RoomManager.DoRoomFloodFill(nestedObject.Tile, true);
        }

        if (Created != null)
        {
            Created(nestedObject);
        }

        return nestedObject;
    }

    /// <summary>
    /// When a construction job is completed, place the NestedObject.
    /// </summary>
    /// <param name="job">The completed job.</param>
    public void ConstructJobCompleted(Job job)
    {
        NestedObject obj = (NestedObject)job.buildablePrototype;

        // Let our workspot tile know it is no longer reserved for us
        World.Current.UnreserveTileAsWorkSpot(obj, job.tile);

        PlaceNestedObject(obj, job.tile);

        // FIXME: I don't like having to manually and explicitly set
        // flags that prevent conflicts. It's too easy to forget to set/clear them!
        job.tile.PendingBuildJob = null;
    }

    /// <summary>
    /// Determines whether the placement of a NestedObject with the given type at the given tile is valid.
    /// </summary>
    /// <returns><c>true</c> if the placement is valid; otherwise, <c>false</c>.</returns>
    /// <param name="type">The NestedObject type.</param>
    /// <param name="tile">The tile where the NestedObject will be placed.</param>
    /// <param name="rotation">The rotation applied to the NestedObject.</param>
    public bool IsPlacementValid(string type, Tile tile, float rotation = 0f)
    {
        NestedObject obj = PrototypeManager.NestedObject.Get(type).Clone();
        obj.SetRotation(rotation);
        return obj.IsValidPosition(tile);
    }

    /// <summary>
    /// Determines whether the work spot of the NestedObject with the given type at the given tile is clear.
    /// </summary>
    /// <returns><c>true</c> if the work spot at the give tile is clear; otherwise, <c>false</c>.</returns>
    /// <param name="NestedObject">NestedObject type.</param>
    /// <param name="tile">The tile we want to check.</param>
    public bool IsWorkSpotClear(string type, Tile tile)
    {
        NestedObject proto = PrototypeManager.NestedObject.Get(type);

        // If the workspot is internal, we don't care about NestedObject blocking it, this will be stopped or allowed
        //      elsewhere depending on if the NestedObject being placed can replace the NestedObject already in this tile.
        if (proto.Jobs.WorkSpotIsInternal())
        {
            return true;
        }

        if (proto.Jobs != null && World.Current.GetTileAt((int)(tile.X + proto.Jobs.WorkSpotOffset.x), (int)(tile.Y + proto.Jobs.WorkSpotOffset.y), (int)tile.Z).NestedObject != null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns the amount of NestedObject with the given type.
    /// </summary>
    /// <returns>The amount of NestedObject with the given type.</returns>
    /// <param name="type">The NestedObject type.</param>
    public int CountWithType(string type)
    {
        return nestedObjects.Count(f => f.Type == type);
    }

    /// <summary>
    /// Reuturns a list of NestedObject using the given filter function.
    /// </summary>
    /// <returns>A list of NestedObject.</returns>
    /// <param name="filterFunc">The filter function.</param>
    public List<NestedObject> Find(Func<NestedObject, bool> filterFunc)
    {
        return nestedObjects.Where(filterFunc).ToList();
    }

    /// <summary>
    /// Calls the NestedObject's update function on every frame.
    /// The list needs to be copied temporarily in case NestedObject's are added or removed during the update.
    /// </summary>
    /// <param name="deltaTime">Delta time.</param>
    public void TickEveryFrame(float deltaTime)
    {
        List<NestedObject> tempNestedObjectsVisible = new List<NestedObject>(nestedObjectsVisible);
        foreach (NestedObject nestedObject in tempNestedObjectsVisible)
        {
            nestedObject.EveryFrameUpdate(deltaTime);
        }
    }

    /// <summary>
    /// Calls the NestedObject update function on a fixed frequency.
    /// The list needs to be copied temporarily in case NestedObject's are added or removed during the update.
    /// </summary>
    /// <param name="deltaTime">Delta time.</param>
    public void TickFixedFrequency(float deltaTime)
    {
        // TODO: Further optimization could divide eventNestedObjects in multiple lists
        //       and update one of the lists each frame.
        //       FixedFrequencyUpdate on invisible NestedObject could also be even slower.

        // Update NestedObject outside of the camera view
        List<NestedObject> tempNestedObjectsInvisible = new List<NestedObject>(nestedObjectsInvisible);
        foreach (NestedObject nestedObject in tempNestedObjectsInvisible)
        {
            nestedObject.EveryFrameUpdate(deltaTime);
        }

        // Update all NestedObject with EventActions
        List<NestedObject> tempNestedObjects = new List<NestedObject>(nestedObjects);
        foreach (NestedObject nestedObject in tempNestedObjects)
        {
            nestedObject.FixedFrequencyUpdate(deltaTime);
        }
    }

    /// <summary>
    /// Gets the NestedObject enumerator.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator GetEnumerator()
    {
        return nestedObjects.GetEnumerator();
    }

    /// <summary>
    /// Gets each NestedObject.
    /// </summary>
    /// <returns>Each NestedObject.</returns>
    IEnumerator<NestedObject> IEnumerable<NestedObject>.GetEnumerator()
    {
        foreach (NestedObject nestedObject in nestedObjects)
        {
            yield return nestedObject;
        }
    }

    /// <summary>
    /// Notify world that the camera moved, so we can check which entities are visible to the camera.
    /// The invisible enities can be updated less frequent for better performance.
    /// </summary>
    public void OnCameraMoved(Bounds cameraBounds)
    {        
        // Expand bounds to include tiles on the edge where the centre isn't inside the bounds
        cameraBounds.Expand(1);

        foreach (NestedObject obj in nestedObjects)
        {
            // Multitile NestedObject base tile is bottom left - so add width and height 
            Bounds nestedObjectBounds = new Bounds(
                new Vector3(obj.Tile.X - 0.5f + (obj.Width / 2), obj.Tile.Y - 0.5f + (obj.Height / 2), 0),
                new Vector3(obj.Width, obj.Height));

            if (cameraBounds.Intersects(nestedObjectBounds))
            {
                if (nestedObjectsInvisible.Contains(obj))
                {
                    nestedObjectsInvisible.Remove(obj);
                    nestedObjectsVisible.Add(obj);
                }
            }
            else
            {
                if (nestedObjectsVisible.Contains(obj))
                {
                    nestedObjectsVisible.Remove(obj);
                    nestedObjectsInvisible.Add(obj);
                }
            }            
        }
    }

    /// <summary>
    /// Writes the NestedObject to the XML.
    /// </summary>
    /// <param name="writer">The Xml Writer.</param>
    public void WriteXml(XmlWriter writer)
    {
        foreach (NestedObject obj in nestedObjects)
        {
            writer.WriteStartElement("NestedObject");
            obj.WriteXml(writer);
            writer.WriteEndElement();
        }
    }

    /// <summary>
    /// Called when a NestedObject is removed so that it can be deleted from the list.
    /// </summary>
    /// <param name="NestedObject">The NestedObject being removed.</param>
    private void OnRemoved(NestedObject nestedObject)
    {
        nestedObjects.Remove(nestedObject);

        if (nestedObjectsInvisible.Contains(nestedObject))
        {
            nestedObjectsInvisible.Remove(nestedObject);            
        }
        else if (nestedObjectsVisible.Contains(nestedObject))
        {
            nestedObjectsVisible.Remove(nestedObject);
        }
    }
}
