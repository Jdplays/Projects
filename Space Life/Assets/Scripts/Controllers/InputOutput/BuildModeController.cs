using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;
using SpaceLife.Rooms;
using UnityEngine;

public enum BuildMode
{
    FLOOR,
    ROOMBEHAVIOR,
    NESTEDOBJECT,
    UTILITY,
    DECONSTRUCT
}

public class BuildModeController
{
    public BuildMode buildMode = BuildMode.FLOOR;
    public string buildModeType;

    private MouseController mouseController;
    private TileType buildModeTile = TileType.Floor;

    public BuildModeController()
    {
        Instance = this;
        CurrentPreviewRotation = 0f;
        KeyboardManager.Instance.RegisterInputAction("RotateNestedObjectLeft", KeyboardMappedInputType.KeyUp, RotateNestedObjectLeft);
        KeyboardManager.Instance.RegisterInputAction("RotateNestedObjectRight", KeyboardMappedInputType.KeyUp, RotateNestedObjectRight);
    }

    public static BuildModeController Instance { get; protected set; }

    // The rotation applied to the object.
    public float CurrentPreviewRotation { get; private set; }

    // Use this for initialization
    public void SetMouseController(MouseController currentMouseController)
    {
        mouseController = currentMouseController;
    }

    public bool IsObjectDraggable()
    {
        if (buildMode == BuildMode.FLOOR || buildMode == BuildMode.DECONSTRUCT || buildMode == BuildMode.UTILITY)
        {
            // floors are draggable
            return true;
        }

        if (buildMode == BuildMode.ROOMBEHAVIOR)
        {
            // Room Behaviors are not draggable
            return false;
        }

        NestedObject proto = PrototypeManager.NestedObject.Get(buildModeType);

        return proto.DragType != "single";
    }

    public string GetFloorTile()
    {
        return buildModeTile.ToString();
    }

    public void SetModeBuildTile(TileType type)
    {
        buildMode = BuildMode.FLOOR;
        buildModeTile = type;

        mouseController.StartBuildMode();
    }

    public void SetMode_DesignateRoomBehavior(string type)
    {
        buildMode = BuildMode.ROOMBEHAVIOR;
        buildModeType = type;
        mouseController.StartBuildMode();
    }

    public void SetMode_BuildNestedObject(string type)
    {
        // Wall is not a Tile!  Wall is a "NestedObject" that exists on TOP of a tile.
        buildMode = BuildMode.NESTEDOBJECT;
        buildModeType = type;
        CurrentPreviewRotation = 0f;
        mouseController.StartBuildMode();
    }

    public void SetMode_BuildUtility(string type)
    {
        // Wall is not a Tile!  Wall is a "NestedObject" that exists on TOP of a tile.
        buildMode = BuildMode.UTILITY;
        buildModeType = type;
        mouseController.StartBuildMode();
    }

    public void SetMode_Deconstruct()
    {
        buildMode = BuildMode.DECONSTRUCT;
        mouseController.StartBuildMode();
    }

    public void DoBuild(Tile tile)
    {
        if (buildMode == BuildMode.ROOMBEHAVIOR)
        {
            string roomBehaviorType = buildModeType;

            if (tile.Room != null && WorldController.Instance.World.IsRoomBehaviorValidForRoom(roomBehaviorType, tile.Room))
            {
                RoomBehavior proto = PrototypeManager.RoomBehavior.Get(roomBehaviorType); 
                tile.Room.DesignateRoomBehavior(proto.Clone());
            }
        }
        else if (buildMode == BuildMode.NESTEDOBJECT)
        {
            // Create the NestedObject and assign it to the tile
            // Can we build the NestedObject in the selected tile?
            // Run the ValidPlacement function!
            string nestedObjectType = buildModeType;

            if ( 
                World.Current.NestedObjectManager.IsPlacementValid(nestedObjectType, tile, CurrentPreviewRotation) &&
                World.Current.NestedObjectManager.IsWorkSpotClear(nestedObjectType, tile) && 
                DoesBuildJobOverlapExistingBuildJob(tile, nestedObjectType, CurrentPreviewRotation) == false)
            {
                // This tile position is valid for this NestedObject

                // Check if there is existing NestedObject in this tile. If so delete it.
                if (tile.NestedObject != null)
                {
                    tile.NestedObject.SetDeconstructJob();
                }

                // Create a job for it to be build
                Job job;

                if (PrototypeManager.NestedObjectConstructJob.Has(nestedObjectType))
                {
                    // Make a clone of the job prototype
                    job = PrototypeManager.NestedObjectConstructJob.Get(nestedObjectType).Clone();

                    // Assign the correct tile.
                    job.tile = tile;
                }
                else
                {
                    Debug.ULogErrorChannel("BuildModeController", "There is no NestedObject job prototype for '" + nestedObjectType + "'");
                    job = new Job(tile, nestedObjectType, World.Current.NestedObjectManager.ConstructJobCompleted, 0.1f, null, Job.JobPriority.High);
                    job.adjacent = true;
                    job.JobDescription = "Deconstructing: " + nestedObjectType + ".";
                }

                NestedObject nestedObjectToBuild = PrototypeManager.NestedObject.Get(nestedObjectType).Clone();
                nestedObjectToBuild.SetRotation(CurrentPreviewRotation);
                job.buildablePrototype = nestedObjectToBuild;

                // Add the job to the queue or build immediately if in Dev mode
                if (Settings.GetSetting("DialogBoxSettings_developerModeToggle", false))
                {
                    World.Current.NestedObjectManager.PlaceNestedObject(nestedObjectToBuild, job.tile);
                }
                else
                {
                    for (int x_off = tile.X; x_off < (tile.X + job.buildablePrototype.Width); x_off++)
                    {
                        for (int y_off = tile.Y; y_off < (tile.Y + job.buildablePrototype.Height); y_off++)
                        {
                            // FIXME: I don't like having to manually and explicitly set
                            // flags that prevent conflicts. It's too easy to forget to set/clear them!
                            Tile offsetTile = World.Current.GetTileAt(x_off, y_off, tile.Z);
                            offsetTile.PendingBuildJob = job;
                            job.OnJobStopped += (theJob) => offsetTile.PendingBuildJob = null;
                        }
                    }

                    World.Current.jobQueue.Enqueue(job);

                    // Let our workspot tile know it is reserved for us
                    World.Current.ReserveTileAsWorkSpot((NestedObject)job.buildablePrototype, job.tile);
                }
            }
        }
        else if (buildMode == BuildMode.UTILITY)
        {
            // Create the NestedObject and assign it to the tile
            // Can we build the NestedObject in the selected tile?
            // Run the ValidPlacement function!
            string utilityType = buildModeType;

            // TODO: Reimplement this later: DoesBuildJobOverlapExistingBuildJob(t, nestedObjectType) == false)
            if ( 
                World.Current.UtilityManager.IsPlacementValid(utilityType, tile)  &&
                DoesSameUtilityTypeAlreadyExist(tile, utilityType) == false)
            {
                // This tile position is valid for this NestedObject

                // Create a job for it to be build
                Job job;

                if (PrototypeManager.UtilityConstructJob.Has(utilityType))
                {
                    // Make a clone of the job prototype
                    job = PrototypeManager.UtilityConstructJob.Get(utilityType).Clone();

                    // Assign the correct tile.
                    job.tile = tile;
                }
                else
                {
                    Debug.ULogErrorChannel("BuildModeController", "There is no NestedObject job prototype for '" + utilityType + "'");
                    job = new Job(tile, utilityType, World.Current.UtilityManager.ConstructJobCompleted, 0.1f, null, Job.JobPriority.High);
                    job.JobDescription = "Building: " + utilityType + ".";
                }

                job.buildablePrototype = PrototypeManager.Utility.Get(utilityType);

                // Add the job to the queue or build immediately if in dev mode
                if (Settings.GetSetting("DialogBoxSettings_developerModeToggle", false))
                {
                    World.Current.UtilityManager.PlaceUtility(job.JobObjectType, job.tile);
                }
                else
                {
                    // FIXME: I don't like having to manually and explicitly set
                    // flags that preven conflicts. It's too easy to forget to set/clear them!
                    Tile offsetTile = World.Current.GetTileAt(tile.X, tile.Y, tile.Z);
                    offsetTile.PendingBuildJob = job;
                    job.OnJobStopped += (theJob) => offsetTile.PendingBuildJob = null;

                    World.Current.jobQueue.Enqueue(job);
                }
            }
        }
        else if (buildMode == BuildMode.FLOOR)
        {
            // We are in tile-changing mode.
            ////t.Type = buildModeTile;

            TileType tileType = buildModeTile;

            if (
                tile.Type != tileType &&
                tile.NestedObject == null &&
                tile.PendingBuildJob == null &&
                tileType.CanBuildHere(tile))
            {
                // This tile position is valid tile type

                // Create a job for it to be build
                Job buildingJob = tileType.BuildingJob;

                buildingJob.tile = tile;

                // Add the job to the queue or build immediately if in Dev mode
                if (Settings.GetSetting("DialogBoxSettings_developerModeToggle", false))
                {
                    buildingJob.tile.Type = buildingJob.JobTileType;
                }
                else
                {
                    // FIXME: I don't like having to manually and explicitly set
                    // flags that prevent conflicts. It's too easy to forget to set/clear them!
                    tile.PendingBuildJob = buildingJob;
                    buildingJob.OnJobStopped += (theJob) => theJob.tile.PendingBuildJob = null;

                    WorldController.Instance.World.jobQueue.Enqueue(buildingJob);
                }
            }
        }
        else if (buildMode == BuildMode.DECONSTRUCT)
        {
            // TODO
            bool canDeconstructAll = Settings.GetSetting("DialogBoxSettings_developerModeToggle", false);
            if (tile.NestedObject != null && (canDeconstructAll || tile.NestedObject.HasTypeTag("Non-deconstructible") == false))
            {
                // check if this is a WALL neighbouring a pressured and pressureless environment, and if so, bail
                if (tile.NestedObject.HasTypeTag("Wall"))
                {
                    Tile[] neighbors = tile.GetNeighbours(); // diagOkay??
                    int pressuredNeighbors = 0;
                    int vacuumNeighbors = 0;
                    foreach (Tile neighbor in neighbors)
                    {
                        if (neighbor != null && neighbor.Room != null)
                        {
                            if (neighbor.Room.IsOutsideRoom() || MathUtilities.IsZero(neighbor.Room.GetTotalGasPressure()))
                            {
                                vacuumNeighbors++;
                            }
                            else
                            {
                                pressuredNeighbors++;
                            }
                        }
                    }

                    if (vacuumNeighbors > 0 && pressuredNeighbors > 0)
                    {
                        Debug.ULogChannel("BuildModeController", "Someone tried to deconstruct a wall between a pressurized room and vacuum!");
                        return;
                    }
                }

                tile.NestedObject.SetDeconstructJob();
            }
            else if (tile.PendingBuildJob != null)
            {
                tile.PendingBuildJob.CancelJob();
            }
            else if (tile.Utilities.Count > 0)
            {
                tile.Utilities.Last().Value.SetDeconstructJob();
            }
        }
        else
        {
            Debug.ULogErrorChannel("BuildModeController", "UNIMPLEMENTED BUILD MODE");
        }
    }

    public bool DoesBuildJobOverlapExistingBuildJob(Tile t, string nestedObjectType, float rotation = 0)
    {
        NestedObject nestedObjectToBuild = PrototypeManager.NestedObject.Get(nestedObjectType).Clone();
        nestedObjectToBuild.SetRotation(rotation);

        for (int x_off = t.X; x_off < (t.X + nestedObjectToBuild.Width); x_off++)
        {
            for (int y_off = t.Y; y_off < (t.Y + nestedObjectToBuild.Height); y_off++)
            {
                Job pendingBuildJob = WorldController.Instance.World.GetTileAt(x_off, y_off, t.Z).PendingBuildJob;
                if (pendingBuildJob != null)
                {
                    // if the existing buildJobs NestedObject is replaceable by the current NestedObject,
                    // we can pretend it does not overlap with the new build
                    return !nestedObjectToBuild.ReplaceableNestedObject.Any(pendingBuildJob.buildablePrototype.HasTypeTag);
                }
            }
        }

        return false;
    }

    public bool DoesSameUtilityTypeAlreadyExist(Tile tile, string nestedObjectType)
    {
        Utility proto = PrototypeManager.Utility.Get(nestedObjectType);
        return tile.Utilities.ContainsKey(proto.Name);
    }

    // Rotate the preview NestedObject to the left.
    private void RotateNestedObjectLeft()
    {
        if (buildMode == BuildMode.NESTEDOBJECT && PrototypeManager.NestedObject.Get(buildModeType).CanRotate)
        {
            CurrentPreviewRotation = (CurrentPreviewRotation + 90) % 360;
        }
    }

    // Rotate the preview NestedObject to the right.
    private void RotateNestedObjectRight()
    {
        if (buildMode == BuildMode.NESTEDOBJECT && PrototypeManager.NestedObject.Get(buildModeType).CanRotate)
        {
            CurrentPreviewRotation = (CurrentPreviewRotation - 90) % 360;
        }
    }
}
