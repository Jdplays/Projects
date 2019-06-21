using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Animation;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using SpaceLife.Buildable.Components;
using SpaceLife.Jobs;
using SpaceLife.PowerNetwork;
using UnityEngine;

/// <summary>
/// InstalledObjects are things like walls, doors, and NestedObject (e.g. a sofa).
/// </summary>
[MoonSharpUserData]
public class NestedObject : IXmlSerializable, ISelectable, IPrototypable, IContextActionProvider, IBuildable
{
    #region Private Variables
    // Prevent construction too close to the world's edge
    private const int MinEdgeDistance = 5;

    private string isEnterableAction;

    /// <summary>
    /// This action is called to get the sprite name based on the NestedObject parameters.
    /// </summary>
    private string getSpriteNameAction;

    /// <summary>
    /// This action is called to get the progress info based on the NestedObject parameters.
    /// </summary>
    private string getProgressInfoNameAction;

    private List<string> replaceableNestedObject = new List<string>();

    /// <summary>
    /// These context menu lua action are used to build the context menu of the NestedObject.
    /// </summary>
    private List<ContextMenuLuaAction> contextMenuLuaActions;
    
    private HashSet<BuildableComponent> components;

    // This is the generic type of object this is, allowing things to interact with it based on it's generic type
    private HashSet<string> typeTags;

    private string name = null;

    private string description = string.Empty;

    private HashSet<string> tileTypeBuildPermissions;

    private bool isOperating;

    private List<Inventory> deconstructInventory;

    // did we have power in the last update?
    private bool prevUpdatePowerOn;
    #endregion

    #region Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="NestedObject"/> class.
    /// This constructor is used to create prototypes and should never be used ouside the Prototype Manager.
    /// </summary>
    public NestedObject()
    {
        Tint = Color.white;
        VerticalDoor = false;
        EventActions = new EventActions();

        contextMenuLuaActions = new List<ContextMenuLuaAction>();
        Parameters = new Parameter();
        Jobs = new BuildableJobs(this);
        typeTags = new HashSet<string>();
        tileTypeBuildPermissions = new HashSet<string>();
        PathfindingWeight = 1f;
        PathfindingModifier = 0f;
        Height = 1;
        Width = 1;
        CanRotate = false;
        Rotation = 0f;
        DragType = "single";
        LinksToNeighbour = string.Empty;
        components = new HashSet<BuildableComponent>();
        InternalInventory = new Dictionary<string, List<Inventory>>();
    }

    /// <summary>
    /// Copy Constructor -- don't call this directly, unless we never
    /// do ANY sub-classing. Instead use Clone(), which is more virtual.
    /// </summary>
    private NestedObject(NestedObject other)
    {
        Type = other.Type;
        Name = other.Name;
        typeTags = new HashSet<string>(other.typeTags);
        description = other.description;
        MovementCost = other.MovementCost;
        PathfindingModifier = other.PathfindingModifier;
        PathfindingWeight = other.PathfindingWeight;
        RoomEnclosure = other.RoomEnclosure;
        Width = other.Width;
        Height = other.Height;
        CanRotate = other.CanRotate;
        Rotation = other.Rotation;
        Tint = other.Tint;
        LinksToNeighbour = other.LinksToNeighbour;
        deconstructInventory = other.deconstructInventory;
        InternalInventory = other.InternalInventory;

        Parameters = new Parameter(other.Parameters);
        Jobs = new BuildableJobs(this, other.Jobs);

        // don't need to clone here, as all are prototype things (not changing)
        components = new HashSet<BuildableComponent>(other.components);

        if (other.Animation != null)
        {
            Animation = other.Animation.Clone();
        }

        if (other.EventActions != null)
        {
            EventActions = other.EventActions.Clone();
        }

        if (other.contextMenuLuaActions != null)
        {
            contextMenuLuaActions = new List<ContextMenuLuaAction>(other.contextMenuLuaActions);
        }

        isEnterableAction = other.isEnterableAction;
        getSpriteNameAction = other.getSpriteNameAction;
        getProgressInfoNameAction = other.getProgressInfoNameAction;

        if (other.PowerConnection != null)
        {
            PowerConnection = other.PowerConnection.Clone() as Connection;
            PowerConnection.NewThresholdReached += OnNewThresholdReached;
        }

        tileTypeBuildPermissions = new HashSet<string>(other.tileTypeBuildPermissions);

        //LocalizationCode = other.LocalizationCode;
        //UnlocalizedDescription = other.UnlocalizedDescription;
    }
    #endregion

    #region Accessors
    /// <summary>
    /// This event will trigger when the NestedObject has been changed.
    /// This is means that any change (parameters, job state etc) to the NestedObject will trigger this.
    /// </summary>
    public event Action<NestedObject> Changed;

    /// <summary>
    /// This event will trigger when the NestedObject has been removed.
    /// </summary>
    public event Action<NestedObject> Removed;

    /// <summary>
    /// This event will trigger if <see cref="IsOperating"/> has been changed.
    /// </summary>
    public event Action<NestedObject> IsOperatingChanged;

    /// <summary>
    /// Gets or sets the NestedObject's <see cref="PathfindingModifier"/> which is added into the Tile's final PathfindingCost.
    /// </summary>
    /// <value>The modifier used in pathfinding.</value>
    public float PathfindingModifier { get; set; }

    /// <summary>
    /// Gets or sets the NestedObject's pathfinding weight which is multiplied into the Tile's final PathfindingCost.
    /// </summary>
    /// <value>The pathfinding weight for the tiles the NestedObject currently occupies.</value>
    public float PathfindingWeight { get; set; }

    /// <summary>
    /// Gets the tint used to change the color of the NestedObject.
    /// </summary>
    /// <value>The Color of the NestedObject.</value>
    public Color Tint { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the door is Vertical or not.
    /// Should be false if the NestedObject is not a door.
    /// This field will most likely be moved to another class.
    /// </summary>
    /// <value>Whether the door is Vertical or not.</value>
    public bool VerticalDoor { get; set; }

    /// <summary>
    /// Gets the EventAction for the current NestedObject.
    /// These actions are called when an event is called. They get passed the NestedObject
    /// they belong to, plus a deltaTime (which defaults to 0).
    /// </summary>
    /// <value>The event actions that is called on update.</value>
    public EventActions EventActions { get; private set; }

    /// <summary>
    /// Gets the Connection that the NestedObject has to the power system.
    /// </summary>
    /// <value>The Connection of the NestedObject.</value>
    public Connection PowerConnection { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the NestedObject is operating or not.
    /// </summary>
    /// <value>Whether the NestedObject is operating or not.</value>
    public bool IsOperating
    {
        get
        {
            return isOperating;
        }

        private set
        {
            if (isOperating == value)
            {
                return;
            }

            isOperating = value;
            OnIsOperatingChanged(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the NestedObject is selected by the player or not.
    /// </summary>
    /// <value>Whether the NestedObject is selected or not.</value>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Gets the BASE tile of the NestedObject. (Large objects can span over multiple tiles).
    /// This should be RENAMED (possibly to BaseTile).
    /// </summary>
    /// <value>The BASE tile of the NestedObject.</value>
    public Tile Tile { get; private set; }

    /// <summary>
    /// Gets the string that defines the type of object the NestedObject is. This gets queried by the visual system to
    /// know what sprite to render for this NestedObject.
    /// </summary>
    /// <value>The type of the NestedObject.</value>
    public string Type { get; private set; }

    /// <summary>
    /// Gets the name of the NestedObject. The name is the object type by default.
    /// </summary>
    /// <value>The name of the NestedObject.</value>
    public string Name
    {
        get
        {
            return string.IsNullOrEmpty(name) ? Type : name;
        }

        private set
        {
            name = value;
        }
    }

    public string Status { get; set; }

    /// <summary>
    /// Gets a list of NestedObject Type this NestedObject can be replaced with.
    /// This should most likely not be a list of strings.
    /// </summary>
    /// <value>A list of NestedObject that this NestedObject can be replaced with.</value>
    public List<string> ReplaceableNestedObject
    {
        get { return replaceableNestedObject; }
    }

    /// <summary>
    /// Gets the movement cost multiplier that this NestedObject has. This can be a float value from 0 to any positive number.
    /// The movement cost acts as a multiplier: e.g. 1 is default, 2 is twice as slow.
    /// Tile types and environmental effects will be combined with this value (additive).
    /// If this value is '0' then the NestedObject is impassable.
    /// </summary>
    /// <value>The movement cost multiplier the NestedObject has.</value>
    public float MovementCost { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the NestedObject can close a room (e.g. act as a wall).
    /// </summary>
    public bool RoomEnclosure { get; private set; }

    /// <summary>
    /// Gets the width of the NestedObject.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Gets the height of the NestedObject.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// If true player is allowed to rotate the NestedObject.
    /// </summary>
    public bool CanRotate { get; private set; }

    /// <summary>
    /// Gets/Set the rotation of the NestedObject.
    /// </summary>
    public float Rotation { get; private set; }

    /// <summary>
    /// Gets the code used for Localization of the NestedObject.
    /// </summary>
    //public string LocalizationCode { get; private set; }

    /// <summary>
    /// Gets the description of the NestedObject. This is used by localization.
    /// </summary>
    //public string UnlocalizedDescription { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this NestedObject is next to any NestedObject of the same type.
    /// This is used to check what sprite to use if NestedObject is next to each other.
    /// </summary>
    public string LinksToNeighbour { get; private set; }

    /// <summary>
    /// Gets the type of dragging that is used to build multiples of this NestedObject.
    /// e.g walls.
    /// </summary>
    public string DragType { get; private set; }

    /// <summary>
    /// Gets or sets the NestedObject animation.
    /// </summary>
    public NestedObjectAnimation Animation { get; set; }

    /// <summary>
    /// Gets or sets the parameters that is tied to the NestedObject.
    /// </summary>
    public Parameter Parameters { get; set; }

    /// <summary>
    /// Gets a component that handles the jobs linked to the NestedObject.
    /// </summary>
    public BuildableJobs Jobs { get; private set; }

    /// <summary>
    /// This flag is set if the NestedObject is tasked to be destroyed.
    /// </summary>
    public bool IsBeingDestroyed { get; protected set; }

    /// Should we only use the default name? If not, then more complex logic is tested, such as walls.
    /// </summary>
    public bool OnlyUseDefaultSpriteName
    {
        get
        {
            return !string.IsNullOrEmpty(getSpriteNameAction);
        }
    }

    /// <summary>
    /// Whether the NestedObject has power or not. Always true if power is not applicable to the NestedObject.
    /// </summary>
    /// <returns>True if the NestedObject has power or if the NestedObject doesn't require power to function.</returns>
    public bool DoesntNeedOrHasPower
    {
        get
        {
            return PowerConnection == null || World.Current.PowerNetwork.HasPower(PowerConnection);
        }
    }

    /// <summary>
    /// This is inventory that is held inside the nestedObject,
    /// used for the mining drone which is on a planet.
    /// </summary>
    public Dictionary<string, List<Inventory>> InternalInventory;
    #endregion

    /// <summary>
    /// Used to place NestedObject in a certain position.
    /// </summary>
    /// <param name="proto">The prototype NestedObject to place.</param>
    /// <param name="tile">The base tile to place the NestedObject on, The tile will be the bottom left corner of the NestedObject (to check).</param>
    /// <returns>NestedObject object.</returns>
    public static NestedObject PlaceInstance(NestedObject proto, Tile tile)
    {
        if (proto.IsValidPosition(tile) == false)
        {
            Debug.ULogErrorChannel("NestedObject", "PlaceInstance -- Position Validity Function returned FALSE. " + proto.Name + " " + tile.X + ", " + tile.Y + ", " + tile.Z);
            return null;
        }

        // We know our placement destination is valid.
        NestedObject nestObj = proto.Clone();
        nestObj.Tile = tile;
        
        if (tile.PlaceNestedObject(nestObj) == false)
        {
            // For some reason, we weren't able to place our object in this tile.
            // (Probably it was already occupied.)

            // Do NOT return our newly instantiated object.
            // (It will be garbage collected.)
            return null;
        }

        // plug-in NestedObject only when it is placed in world
        if (nestObj.PowerConnection != null)
        {
            World.Current.PowerNetwork.PlugIn(nestObj.PowerConnection);
        }

        // need to update reference to NestedObject and call Initialize (so components can place hooks on events there)
        foreach (var comp in nestObj.components)
        {
            comp.Initialize(nestObj);
        }

        if (nestObj.LinksToNeighbour != string.Empty)
        {
            // This type of NestedObject links itself to its neighbours,
            // so we should inform our neighbours that they have a new
            // buddy.  Just trigger their OnChangedCallback.
            int x = tile.X;
            int y = tile.Y;

            for (int xpos = x - 1; xpos < x + proto.Width + 1; xpos++)
            {
                for (int ypos = y - 1; ypos < y + proto.Height + 1; ypos++)
                {
                    Tile tileAt = World.Current.GetTileAt(xpos, ypos, tile.Z);
                    if (tileAt != null && tileAt.NestedObject != null && tileAt.NestedObject.Changed != null)
                    {
                        tileAt.NestedObject.Changed(tileAt.NestedObject);
                    }
                }
            }
        }

        // Let our workspot tile know it is reserved for us
        World.Current.ReserveTileAsWorkSpot(nestObj);

        // Call LUA install scripts
        nestObj.EventActions.Trigger("OnInstall", nestObj);

        // Update thermalDiffusivity using coefficient
        float thermalDiffusivity = Temperature.defaultThermalDiffusivity;
        if (nestObj.Parameters.ContainsKey("thermal_diffusivity"))
        {
            thermalDiffusivity = nestObj.Parameters["thermal_diffusivity"].ToFloat();
        }

        World.Current.temperature.SetThermalDiffusivity(tile.X, tile.Y, tile.Z, thermalDiffusivity);

        return nestObj;
    }

    #region Update and Animation
    /// <summary>
    /// This function is called to update the NestedObject animation in lua.
    /// This will be called every frame and should be used carefully.
    /// </summary>
    /// <param name="deltaTime">The time since the last update was called.</param>
    public void EveryFrameUpdate(float deltaTime)
    {
        if (EventActions != null)
        {
            EventActions.Trigger("OnFastUpdate", this, deltaTime);
        }

        foreach (var cmp in components)
        {
            cmp.EveryFrameUpdate(deltaTime);
        }
    }

    /// <summary>
    /// This function is called to update the NestedObject. This will also trigger EventsActions.
    /// This checks if the NestedObject is a PowerConsumer, and if it does not have power it cancels its job.
    /// </summary>
    /// <param name="deltaTime">The time since the last update was called.</param>
    public void FixedFrequencyUpdate(float deltaTime)
    {
        // requirements from components (gas, ...)
        bool canFunction = true;
        foreach (var cmp in components)
        {
            canFunction &= cmp.CanFunction();
        }

        IsOperating = DoesntNeedOrHasPower && canFunction;

        if ((PowerConnection != null && PowerConnection.IsPowerConsumer && DoesntNeedOrHasPower == false) ||
            canFunction == false)
        {
            if (prevUpdatePowerOn)
            {
                EventActions.Trigger("OnPowerOff", this, deltaTime);
            }

            Jobs.PauseAll();
            prevUpdatePowerOn = false;
            return;
        }

        prevUpdatePowerOn = true;
        Jobs.ResumeAll();

        // TODO: some weird thing happens
        if (EventActions != null)
        {
            EventActions.Trigger("OnUpdate", this, deltaTime);
        }
                
        foreach (var cmp in components)
        {
            cmp.FixedFrequencyUpdate(deltaTime);
        }        
        
        if (Animation != null)
        {
            Animation.Update(deltaTime);
        }
    }

    /// <summary>
    /// Set the animation state. Will only have an effect if stateName is different from current animation stateName.
    /// </summary>
    public void SetAnimationState(string stateName)
    {
        if (Animation == null)
        {
            return;
        }

        Animation.SetState(stateName);
    }

    /// <summary>
    /// Set the animation frame depending on a value. The currentvalue percent of the maxvalue will determine which frame is shown.
    /// </summary>
    public void SetAnimationProgressValue(float currentValue, float maxValue)
    {
        if (Animation == null)
        {
            return;
        }

        if (maxValue == 0)
        {
            Debug.ULogError("SetAnimationProgressValue maxValue is zero");
        }

        float percent = Mathf.Clamp01(currentValue / maxValue);
        Animation.SetProgressValue(percent);
    }
    #endregion

    #region Get Status
    /// <summary>
    /// Whether this NestedObject is an exit for a room.
    /// </summary>
    /// <returns>True if NestedObject is an exit.</returns>
    public bool IsExit()
    {
        if (RoomEnclosure && MovementCost > 0f)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the NestedObject can be Entered.
    /// </summary>
    /// <returns>Enterability state Yes if NestedObject can be entered, Soon if it can be entered after a bit and No
    /// if it cannot be entered.</returns>
    public Enterability IsEnterable()
    {
        if (string.IsNullOrEmpty(isEnterableAction))
        {
            return Enterability.Yes;
        }

        DynValue ret = FunctionsManager.NestedObject.Call(isEnterableAction, this);
        return (Enterability)ret.Number;
    }

    /// <summary>
    /// Check if the NestedObject has a function to determine the sprite name and calls that function.
    /// </summary>
    /// <returns>Name of the sprite.</returns>
    public string GetSpriteName()
    {
        if (!string.IsNullOrEmpty(getSpriteNameAction))
        {
            DynValue ret = FunctionsManager.NestedObject.Call(getSpriteNameAction, this);
            return ret.String;
        }

        // Try to get spritename from animation
        if (Animation != null)
        {
            return Animation.GetSpriteName();
        }

        // Else return default Type string
        return Type;
    }
    #endregion

    #region Save Load
    /// <summary>
    /// This does absolutely nothing.
    /// This is required to implement IXmlSerializable.
    /// </summary>
    /// <returns>NULL and NULL.</returns>
    public XmlSchema GetSchema()
    {
        return null;
    }

    /// <summary>
    /// Writes the NestedObject to XML.
    /// </summary>
    /// <param name="writer">The XML writer to write to.</param>
    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", Tile.X.ToString());
        writer.WriteAttributeString("Y", Tile.Y.ToString());
        writer.WriteAttributeString("Z", Tile.Z.ToString());
        writer.WriteAttributeString("type", Type);
        writer.WriteAttributeString("Rotation", Rotation.ToString());

        // Let the Parameters handle their own xml
        Parameters.WriteXml(writer);
    }

    #endregion

    #region Read Prototype
    /// <summary>
    /// Reads the prototype NestedObject from XML.
    /// </summary>
    /// <param name="readerParent">The XML reader to read from.</param>
    public void ReadXmlPrototype(XmlReader readerParent)
    {
        Type = readerParent.GetAttribute("type");

        XmlReader reader = readerParent.ReadSubtree();

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Name":
                    reader.Read();
                    Name = reader.ReadContentAsString();
                    break;
                case "DefaultStatus":
                    reader.Read();
                    Status = reader.ReadContentAsString();
                    break;
                case "TypeTag":
                    reader.Read();
                    typeTags.Add(reader.ReadContentAsString());
                    break;
                case "Description":
                    reader.Read();
                    description = reader.ReadContentAsString();
                    break;
                case "MovementCost":
                    reader.Read();
                    MovementCost = reader.ReadContentAsFloat();
                    break;
                case "PathfindingModifier":
                    reader.Read();
                    PathfindingModifier = reader.ReadContentAsFloat();
                    break;
                case "PathfindingWeight":
                    reader.Read();
                    PathfindingWeight = reader.ReadContentAsFloat();
                    break;
                case "Width":
                    reader.Read();
                    Width = reader.ReadContentAsInt();
                    break;
                case "Height":
                    reader.Read();
                    Height = reader.ReadContentAsInt();
                    break;
                case "LinksToNeighbours":
                    reader.Read();
                    LinksToNeighbour = reader.ReadContentAsString();
                    break;
                case "EnclosesRooms":
                    reader.Read();
                    RoomEnclosure = reader.ReadContentAsBoolean();
                    break;
                case "CanReplaceNestedObject":
                    replaceableNestedObject.Add(reader.GetAttribute("typeTag").ToString());
                    break;
                case "CanRotate":
                    reader.Read();
                    CanRotate = reader.ReadContentAsBoolean();
                    break;
                case "DragType":
                    reader.Read();
                    DragType = reader.ReadContentAsString();
                    break;
                case "BuildingJob":
                    ReadXmlBuildingJob(reader);
                    break;
                case "DeconstructJob":
                    ReadXmlDeconstructJob(reader);
                    break;
                case "CanBeBuiltOn":
                    tileTypeBuildPermissions.Add(reader.GetAttribute("tileType"));
                    break;
                case "Animations":
                    XmlReader animationReader = reader.ReadSubtree();
                    ReadAnimationXml(animationReader);
                    break;
                case "Action":
                    XmlReader subtree = reader.ReadSubtree();
                    EventActions.ReadXml(subtree);
                    subtree.Close();
                    break;
                case "ContextMenuAction":
                    contextMenuLuaActions.Add(new ContextMenuLuaAction
                    {
                        LuaFunction = reader.GetAttribute("FunctionName"),
                        Text = reader.GetAttribute("Text"),
                        RequireCharacterSelected = bool.Parse(reader.GetAttribute("RequireCharacterSelected")),
                        DevModeOnly = bool.Parse(reader.GetAttribute("DevModeOnly") ?? "false")
                    });
                    break;
                case "IsEnterable":
                    isEnterableAction = reader.GetAttribute("FunctionName");
                    break;
                case "GetSpriteName":
                    getSpriteNameAction = reader.GetAttribute("FunctionName");
                    break;
                case "GetProgressInfo":
                    getProgressInfoNameAction = reader.GetAttribute("functionName");
                    break;
                case "JobWorkSpotOffset":
                    Jobs.ReadWorkSpotOffset(reader);
                    break;
                case "JobInputSpotOffset":
                    Jobs.ReadInputSpotOffset(reader);
                    break;
                case "JobOutputSpotOffset":
                    Jobs.ReadOutputSpotOffset(reader);
                    break;
                case "PowerConnection":
                    PowerConnection = new Connection();
                    PowerConnection.ReadPrototype(reader);
                    break;
                case "Params":
                    ReadXmlParams(reader);  // Read in the Param tag
                    break;
                /*case "LocalizationCode":
                    reader.Read();
                    LocalizationCode = reader.ReadContentAsString();
                    break;
                case "UnlocalizedDescription":
                    reader.Read();
                    UnlocalizedDescription = reader.ReadContentAsString();
                    break;*/
                case "Component":
                    var cmp = BuildableComponent.Deserialize(reader);
                    if (cmp != null)
                    {
                        components.Add(cmp);
                    }

                    break;
            }
        }
    }

    /// <summary>
    /// Reads the specified XMLReader (pass it to <see cref="ReadXmlParams(XmlReader)"/>)
    /// This is used to load NestedObject from a save file.
    /// </summary>
    /// <param name="reader">The XML reader to read from.</param>
    public void ReadXml(XmlReader reader)
    {
        // X, Y, type and rotation have already been set, and we should already
        // be assigned to a tile.  So just read extra data if we have any.
        if (!reader.IsEmptyElement)
        {
            ReadXmlParams(reader);
        }
    }

    /// <summary>
    /// Reads the XML for parameters that this NestedObject has and assign it to the NestedObject.
    /// </summary>
    /// <param name="reader">The reader to read the parameters from.</param>
    public void ReadXmlParams(XmlReader reader)
    {
        Parameters = Parameter.ReadXml(reader);
    }

    /// <summary>
    /// Reads the XML building job.
    /// </summary>
    /// <param name="reader">The XML reader to read from.</param>
    public void ReadXmlBuildingJob(XmlReader reader)
    {
        float jobTime = float.Parse(reader.GetAttribute("jobTime"));
        List<RequestedItem> items = new List<RequestedItem>();
        XmlReader inventoryReader = reader.ReadSubtree();

        while (inventoryReader.Read())
        {
            if (inventoryReader.Name == "Inventory")
            {
                // Found an inventory requirement, so add it to the list!
                int amount = int.Parse(inventoryReader.GetAttribute("amount"));
                items.Add(new RequestedItem(inventoryReader.GetAttribute("type"), amount));
            }
        }

        Job job = new Job(
            null,
            Type,
            (theJob) => World.Current.NestedObjectManager.ConstructJobCompleted(theJob),
            jobTime,
            items.ToArray(),
            Job.JobPriority.High);
        job.JobDescription = "Building: " + Type + ".";

        PrototypeManager.NestedObjectConstructJob.Set(job);
    }

    /// <summary>
    /// Reads the XML building job.
    /// </summary>
    /// <param name="reader">The XML reader to read from.</param>
    public void ReadXmlDeconstructJob(XmlReader reader)
    {
        float jobTime = 0;
        float.TryParse(reader.GetAttribute("jobTime"), out jobTime);
        deconstructInventory = new List<Inventory>();
        XmlReader inventoryReader = reader.ReadSubtree();

        while (inventoryReader.Read())
        {
            if (inventoryReader.Name == "Inventory")
            {
                // Found an inventory requirement, so add it to the list!
                deconstructInventory.Add(new Inventory(
                    inventoryReader.GetAttribute("type"),
                    int.Parse(inventoryReader.GetAttribute("amount"))));
            }
        }

        Job job = new Job(
            null,
            Type,
            null,
            jobTime,
            null,
            Job.JobPriority.High);
        job.JobDescription = "Deconstructing: " + Type + ".";
        job.adjacent = true;

        PrototypeManager.NestedObjectDeconstructJob.Set(job);
    }
    #endregion

    /// <summary>
    /// Accepts for storage.
    /// </summary>
    /// <returns>A list of RequestedItem which the NestedObject accepts for storage.</returns>
    public RequestedItem[] AcceptsForStorage()
    {
        if (HasTypeTag("Storage") == false)
        {
            Debug.ULogChannel("Stockpile_messages", "Someone is asking a non-stockpile to store stuff!?");
            return null;
        }

        // TODO: read this from NestedObject params
        Dictionary<string, RequestedItem> itemsDict = new Dictionary<string, RequestedItem>();
        foreach (InventoryCommon inventoryProto in PrototypeManager.Inventory.Values)
        {
            itemsDict[inventoryProto.type] = new RequestedItem(inventoryProto.type, 1, inventoryProto.maxStackSize);
        }

        return itemsDict.Values.ToArray();
    }

    #region Deconstruct
    /// <summary>
    /// Sets the NestedObject to be deconstructed.
    /// </summary>
    public void SetDeconstructJob()
    {
        if (Settings.GetSetting("DialogBoxSettings_developerModeToggle", false))
        {
            Deconstruct();
            return;
        }

        if (IsBeingDestroyed)
        {
            return; // Already being destroyed, don't do anything more
        }

        IsBeingDestroyed = true;
        Jobs.CancelAll();

        Job job = PrototypeManager.NestedObjectDeconstructJob.Get(Type).Clone();
        job.tile = Tile;
        job.OnJobCompleted += (inJob) => Deconstruct();

        World.Current.jobQueue.Enqueue(job);
    }

    /// <summary>
    /// Deconstructs the NestedObject.
    /// </summary>
    public void Deconstruct()
    { 
        int x = Tile.X;
        int y = Tile.Y;
        int fwidth = 1;
        int fheight = 1;
        string linksToNeighbour = string.Empty;
        if (Tile.NestedObject != null)
        {
            NestedObject nestedObject = Tile.NestedObject;
            fwidth = nestedObject.Width;
            fheight = nestedObject.Height;
            linksToNeighbour = nestedObject.LinksToNeighbour;
            nestedObject.Jobs.CancelAll();
        }

        // We call lua to decostruct
        EventActions.Trigger("OnUninstall", this);

        // Update thermalDiffusifity to default value
        World.Current.temperature.SetThermalDiffusivity(Tile.X, Tile.Y, Tile.Z, Temperature.defaultThermalDiffusivity);

        // Let our workspot tile know it is no longer reserved for us
        World.Current.UnreserveTileAsWorkSpot(this);

        Tile.UnplaceNestedObject();

        if (deconstructInventory != null)
        {
            foreach (Inventory inv in deconstructInventory)
            {
                inv.MaxStackSize = PrototypeManager.Inventory.Get(inv.Type).maxStackSize;
                World.Current.InventoryManager.PlaceInventoryAround(Tile, inv.Clone());
            }
        }

        if (PowerConnection != null)
        {
            World.Current.PowerNetwork.Unplug(PowerConnection);
            PowerConnection.NewThresholdReached -= OnNewThresholdReached;
        }

        if (Removed != null)
        {
            Removed(this);
        }

        // Do we need to recalculate our rooms?
        if (RoomEnclosure)
        {
            World.Current.RoomManager.DoRoomFloodFill(Tile, false);
        }

        ////World.current.InvalidateTileGraph();

        if (World.Current.tileGraph != null)
        {
            World.Current.tileGraph.RegenerateGraphAtTile(Tile);
        }

        // We should inform our neighbours that they have just lost a
        // neighbour regardless of type.  
        // Just trigger their OnChangedCallback. 
        if (linksToNeighbour != string.Empty)
        {
            for (int xpos = x - 1; xpos < x + fwidth + 1; xpos++)
            {
                for (int ypos = y - 1; ypos < y + fheight + 1; ypos++)
                {
                    Tile t = World.Current.GetTileAt(xpos, ypos, Tile.Z);
                    if (t != null && t.NestedObject != null && t.NestedObject.Changed != null)
                    {
                        t.NestedObject.Changed(t.NestedObject);
                    }
                }
            }
        }

        // At this point, no DATA structures should be pointing to us, so we
        // should get garbage-collected.
    }
    #endregion

    #region Get Description Information
    /// <summary>
    /// Checks whether the NestedObject has a certain tag.
    /// </summary>
    /// <param name="typeTag">Tag to check for.</param>
    /// <returns>True if NestedObject has specified tag.</returns>
    public bool HasTypeTag(string typeTag)
    {
        return typeTags.Contains(typeTag);
    }

    /// <summary>
    /// Returns LocalizationCode name for the NestedObject.
    /// </summary>
    /// <returns>LocalizationCode for the name of the NestedObject.</returns>
    public string GetName()
    {
        return this.name; // this.Name;
    }

    /// <summary>
    /// Returns the UnlocalizedDescription of the NestedObject.
    /// </summary>
    /// <returns>Description of the NestedObject.</returns>
    public string GetDescription() 
    {
        return description;
    }

    public string GetStatus()
    {
        return "Status: " + Status;
    }

    public Dictionary<string, List<Inventory>> GetInternalInventory()
    {
        return InternalInventory;
    }

    /// <summary>
    /// Returns the description of the job linked to the NestedObject. NOT INMPLEMENTED.
    /// </summary>
    /// <returns>Job description of the job linked to the NestedObject.</returns>
    public string GetJobDescription()
    {
        return string.Empty;
    }

    public string GetProgressInfo()
    {
        if (string.IsNullOrEmpty(getProgressInfoNameAction))
        {
            return string.Empty;
        }
        else
        {
            DynValue ret = FunctionsManager.NestedObject.Call(getProgressInfoNameAction, this);
            return ret.String;
        }
    }

    public IEnumerable<string> GetAdditionalInfo()
    {
        // try to get some info from components
        foreach (var comp in components)
        {
            string desc = comp.GetDescription();
            if (!string.IsNullOrEmpty(desc))
            {
                yield return desc;
            }
        }
        
        yield return string.Format("Hitpoint 18 / 18");

        if (PowerConnection != null)
        {
            bool hasPower = DoesntNeedOrHasPower;
            string powerColor = hasPower ? "green" : "red";

            yield return string.Format("Power Grid: <color={0}>{1}</color>", powerColor, hasPower ? "Online" : "Offline");

            if (PowerConnection.IsPowerConsumer)
            {
                yield return string.Format("Power Input: <color={0}>{1}</color>", powerColor, PowerConnection.InputRate);
            }

            if (PowerConnection.IsPowerProducer)
            {
                yield return string.Format("Power Output: <color={0}>{1}</color>", powerColor, PowerConnection.OutputRate);
            }

            if (PowerConnection.IsPowerAccumulator)
            {
                yield return string.Format("Power Accumulated: {0} / {1}", PowerConnection.AccumulatedPower, PowerConnection.Capacity);
            }
        }

        yield return GetProgressInfo();
    }
    #endregion

    #region Context Menu
    /// <summary>
    /// Gets the Context Menu Actions.
    /// </summary>
    /// <param name="contextMenu">The context menu to check for actions.</param>
    /// <returns>Context menu actions.</returns>
    public IEnumerable<ContextMenuAction> GetContextMenuActions(ContextMenu contextMenu)
    {
        if (Settings.GetSetting("DialogBoxSettings_developerModeToggle", false) == true || HasTypeTag("Non-deconstructible") == false)
        {
            yield return new ContextMenuAction
            {
                Text = "Deconstruct " + Name,
                RequireCharacterSelected = false,
                Action = (ca, c) => SetDeconstructJob()
            };
        }

        for (int i = 0; i < Jobs.Count; i++)
        {
            if (!Jobs[i].IsBeingWorked)
            {
                yield return new ContextMenuAction
                {
                    Text = "Prioritize " + Name,
                    RequireCharacterSelected = true,
                    Action = (ca, c) => c.PrioritizeJob(Jobs[0])
                };
            }
        }

        // check for context menus of components
        foreach (var comp in components)
        {
            var compContextMenu = comp.GetContextMenu();
            if (compContextMenu != null)
            {
                foreach (ContextMenuAction compContextMenuAction in compContextMenu)
                {
                    yield return compContextMenuAction;
                }
            }
        }
       
        foreach (ContextMenuLuaAction contextMenuLuaAction in contextMenuLuaActions)
        {
            if (!contextMenuLuaAction.DevModeOnly ||
                Settings.GetSetting("DialogBoxSettings_developerModeToggle", false))
            {
                // TODO The Action could be done via a lambda, but it always uses the same space of memory, thus if 2 actions are performed, the same action will be produced for each.
                yield return new ContextMenuAction
                {
                    Text = contextMenuLuaAction.Text,
                    RequireCharacterSelected = contextMenuLuaAction.RequireCharacterSelected,
                    Action = InvokeContextMenuLuaAction,
                    Parameter = contextMenuLuaAction.LuaFunction    // Note that this is only in place because of the problem with the previous statement.
                };
            }
        }
    }
    #endregion

    // <summary>
    // Set rotation on a NestedObject. It will swap height and width.
    // </summary>
    // <param name="rotation">The z rotation.</param>
    public void SetRotation(float rotation)
    {
        if (Math.Abs(Rotation - rotation) == 90 || Math.Abs(Rotation - rotation) == 270)
        {
            int tmp = Height;
            Height = Width;
            Width = tmp;
        }

        Rotation = rotation;
    }

    // Make a copy of the current NestedObject.  Sub-classed should
    // override this Clone() if a different (sub-classed) copy
    // constructor should be run.
    public NestedObject Clone()
    {
        return new NestedObject(this);
    }

    /// <summary>
    /// Check if the position of the NestedObject is valid or not.
    /// This is called when placing the NestedObject.
    /// TODO : Add some LUA special requierments.
    /// </summary>
    /// <param name="t">The base tile.</param>
    /// <returns>True if the tile is valid for the placement of the NestedObject.</returns>
    public bool IsValidPosition(Tile tile)
    {
        bool tooCloseToEdge = tile.X < MinEdgeDistance || tile.Y < MinEdgeDistance ||
                              World.Current.Width - tile.X <= MinEdgeDistance ||
                              World.Current.Height - tile.Y <= MinEdgeDistance;

        if (tooCloseToEdge)
        {
            return false;
        }

        if (HasTypeTag("OutdoorOnly"))
        {
            if (tile.Room == null || !tile.Room.IsOutsideRoom())
            {
                return false;
            }
        }

        for (int x_off = tile.X; x_off < tile.X + Width; x_off++)
        {
            for (int y_off = tile.Y; y_off < tile.Y + Height; y_off++)
            {
                Tile tile2 = World.Current.GetTileAt(x_off, y_off, tile.Z);

                // Check to see if there is NestedObject which is replaceable
                bool isReplaceable = false;

                if (tile2.NestedObject != null)
                {
                    // NestedObject can be replaced, if its typeTags share elements with NestedObject
                    isReplaceable = tile2.NestedObject.typeTags.Overlaps(ReplaceableNestedObject);
                }

                // Make sure tile is FLOOR
                if (tile2.Type != TileType.Floor && tileTypeBuildPermissions.Contains(tile2.Type.Type) == false)
                {
                    return false;
                }

                // Make sure tile doesn't already have NestedObject
                if (tile2.NestedObject != null && isReplaceable == false)
                {
                    return false;
                }

                // Make sure we're not building on another NestedObject's workspot
                if (tile2.IsReservedWorkSpot())
                {
                    return false;
                }
            }
        }

        return true;
    }

    public int GetTotalInternalInventory()
    {
        int invCount = 0;
        foreach (List<Inventory> inventories in InternalInventory.Values)
        {
            foreach (Inventory inventory in inventories)
            {
                invCount += inventory.StackSize;
            }
        }

        Debug.Log(invCount);
        return invCount;
    }

    public bool DoesInternalInventoryContain(string key)
    {
        if (InternalInventory.ContainsKey(key))
        {
            return true;
        }
        return false;
    }

    public void CreateInternalInventoryList(string key)
    {
        if (InternalInventory.ContainsKey(key) == false)
        {
            Debug.Log("CreateInternalInventory");
            List<Inventory> inv = new List<Inventory>();
            InternalInventory[key] = inv;
        }

        return;
    }

    public void CreateInternalInventory(string key)
    {
        if (InternalInventory.ContainsKey(key) == false)
        {
            return;
        }

        int maxStackSize = PrototypeManager.Inventory.Get(key).maxStackSize;
        InternalInventory[key].Add(new Inventory(key, 0, maxStackSize));
        Debug.Log("Max stack size for '" + key + "' is " + maxStackSize);
    }

    public int GetIndexOfFreeInternalInventory(string key, int amountToAdd)
    {
        int index = 0;
        foreach (Inventory inventory in InternalInventory[key])
        {
            if (inventory.StackSize + amountToAdd <= inventory.MaxStackSize)
            {
                index = Array.IndexOf(InternalInventory[key].ToArray(), inventory);
                return index;
            }
        }

        // No free inventory slots create a new one
        CreateInternalInventory(key);
        return GetIndexOfFreeInternalInventory(key, amountToAdd);
    }

    public void AddToInternalInventory(string key, int index, int amout)
    {
        InternalInventory[key][index].StackSize += amout;
    }

    #region Private Context Menu
    private void InvokeContextMenuLuaAction(ContextMenuAction action, Character character)
    {
        FunctionsManager.NestedObject.Call(action.Parameter, this, character);
    }
    #endregion

    #region OnChanges
    [MoonSharpVisible(true)]
    private void UpdateOnChanged(NestedObject obj)
    {
        if (Changed != null)
        {
            Changed(obj);
        }
    }

    private void OnIsOperatingChanged(NestedObject nestedObject)
    {
        Action<NestedObject> handler = IsOperatingChanged;
        if (handler != null)
        {
            handler(nestedObject);
        }
    }

    private void OnNewThresholdReached(Connection connection)
    {
        UpdateOnChanged(this);
    }
    #endregion

    /// <summary>
    /// Reads and creates NestedObject from the prototype xml.
    /// </summary>
    private void ReadAnimationXml(XmlReader animationReader)
    {
        Animation = new NestedObjectAnimation();
        while (animationReader.Read())
        {
            if (animationReader.Name == "Animation")
            {
                string state = animationReader.GetAttribute("state");
                float fps = 1;
                float.TryParse(animationReader.GetAttribute("fps"), out fps);
                bool looping = true;
                bool.TryParse(animationReader.GetAttribute("looping"), out looping);
                bool valueBased = false;
                bool.TryParse(animationReader.GetAttribute("valuebased"), out valueBased);

                // read frames
                XmlReader frameReader = animationReader.ReadSubtree();
                List<string> framesSpriteNames = new List<string>();
                while (frameReader.Read())
                {
                    if (frameReader.Name == "Frame")
                    {
                        framesSpriteNames.Add(frameReader.GetAttribute("name"));
                    }
                }

                Animation.AddAnimation(state, framesSpriteNames, fps, looping, valueBased);
            }
        }
    }
}
