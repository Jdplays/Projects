using System.Collections.Generic;
using SpaceLife.Rooms;
using Scheduler;

/// <summary>
/// A class that holds the Prototype Maps of each entity that requires it.
/// </summary>
public class PrototypeManager
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PrototypeManager"/> class.
    /// </summary>
    public PrototypeManager()
    {
        Inventory = new PrototypeMap<InventoryCommon>("Inventories", "Inventory");
        TileType = new PrototypeMap<TileType>("Tiles", "Tile");
        NestedObject = new PrototypeMap<NestedObject>("NestedObjects", "NestedObject");
        NestedObjectConstructJob = new PrototypeMap<Job>();
        NestedObjectDeconstructJob = new PrototypeMap<Job>();
        Utility = new PrototypeMap<Utility>("Utilities", "Utility");
        UtilityConstructJob = new PrototypeMap<Job>();
        UtilityDeconstructJob = new PrototypeMap<Job>();
        RoomBehavior = new PrototypeMap<RoomBehavior>("RoomBehaviors", "RoomBehavior");
        Need = new PrototypeMap<Need>("Needs", "Need");
        Trader = new PrototypeMap<TraderPrototype>("Traders", "Trader");
        Drone = new PrototypeMap<DronePrototype>("Drones", "Drone");
        Currency = new PrototypeMap<Currency>("Currencies", "Currency");
        Quest = new PrototypeMap<Quest>("Quests", "Quest");
        Stat = new PrototypeMap<Stat>("Stats", "Stat");
        GameEvent = new PrototypeMap<GameEvent>("GameEvents", "GameEvent");
        ScheduledEvent = new PrototypeMap<ScheduledEvent>("ScheduledEvents", "ScheduledEvent");
        Headline = new PrototypeMap<Headline>("Headlines", "Headline");
        Overlay = new PrototypeMap<OverlayDescriptor>("Overlays", "Overlay");
        Ship = new PrototypeMap<Ship>("Ships", "Ship");
    }

    /// <summary>
    /// Gets the tile type prototype map.
    /// </summary>
    /// <value>The NestedObject prototype map.</value>
    public static PrototypeMap<TileType> TileType { get; private set; }

    /// <summary>
    /// Gets the NestedObject prototype map.
    /// </summary>
    /// <value>The NestedObject prototype map.</value>
    public static PrototypeMap<NestedObject> NestedObject { get; private set; }

    /// <summary>
    /// Gets the NestedObject job construct prototype map.
    /// </summary>
    /// <value>The NestedObject job construct prototype map.</value>
    public static PrototypeMap<Job> NestedObjectConstructJob { get; private set; }

    /// <summary>
    /// Gets the NestedObject job deconstruct prototype map.
    /// </summary>
    /// <value>The NestedObject job deconstruct prototype map.</value>
    public static PrototypeMap<Job> NestedObjectDeconstructJob { get; private set; }

    /// <summary>
    /// Gets the utility prototype map.
    /// </summary>
    /// <value>The utility prototype map.</value>
    public static PrototypeMap<Utility> Utility { get; private set; }

    /// Gets the NestedObject construct job prototype map.
    /// </summary>
    /// <value>The NestedObject construct job prototype map.</value>
    public static PrototypeMap<Job> UtilityConstructJob { get; private set; }

    /// Gets the NestedObject deconstruct job prototype map.
    /// </summary>
    /// <value>The NestedObject deconstructjob prototype map.</value>
    public static PrototypeMap<Job> UtilityDeconstructJob { get; private set; }

    /// Gets the roomBehavior prototype map.
    /// </summary>
    /// <value>The roomBehavior prototype map.</value>
    public static PrototypeMap<RoomBehavior> RoomBehavior { get; private set; }

    /// <summary>
    /// Gets the inventory prototype map.
    /// </summary>
    /// <value>The inventory prototype map.</value>
    public static PrototypeMap<InventoryCommon> Inventory { get; private set; }

    /// <summary>
    /// Gets the need prototype map.
    /// </summary>
    /// <value>The need prototype map.</value>
    public static PrototypeMap<Need> Need { get; private set; }

    /// <summary>
    /// Gets the trader prototype map.
    /// </summary>
    /// <value>The trader prototype map.</value>
    public static PrototypeMap<TraderPrototype> Trader { get; private set; }

    /// <summary>
    /// Gets the trader prototype map.
    /// </summary>
    /// <value>The drone prototype map.</value>
    public static PrototypeMap<DronePrototype> Drone { get; private set; }

    /// <summary>
    /// Gets the currency prototype map.
    /// </summary>
    /// <value>The currency prototype map.</value>
    public static PrototypeMap<Currency> Currency { get; private set; }

    /// <summary>
    /// Gets the quest prototype map.
    /// </summary>
    /// <value>The quest prototype map.</value>
    public static PrototypeMap<Quest> Quest { get; private set; }

    /// <summary>
    /// Gets the stat prototype map.
    /// </summary>
    /// <value>The stat prototype map.</value>
    public static PrototypeMap<Stat> Stat { get; private set; }

    /// <summary>
    /// Gets the game event prototype map.
    /// </summary>
    /// <value>The game event prototype map.</value>
    public static PrototypeMap<GameEvent> GameEvent { get; private set; }

    /// <summary>
    /// Gets the scheduled event prototype map.
    /// </summary>
    /// <value>The scheduled event prototype map.</value>
    public static PrototypeMap<ScheduledEvent> ScheduledEvent { get; private set; }

    /// <summary>
    /// Gets the headline prototype map.
    /// </summary>
    /// <value>The headline prototype map.</value>
    public static PrototypeMap<Headline> Headline { get; private set; }

    /// <summary>
    /// Gets the overlay prototype map.
    /// </summary>
    /// <value>The overlay prototype map.</value>
    public static PrototypeMap<OverlayDescriptor> Overlay { get; private set; }

    /// <summary>
    /// Gets the ship prototype map.
    /// </summary>
    /// <value>The ship prototype map.</value>
    public static PrototypeMap<Ship> Ship { get; private set; }
}
