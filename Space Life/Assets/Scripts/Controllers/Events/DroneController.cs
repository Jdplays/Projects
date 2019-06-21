using System;
using System.Collections.Generic;
using System.Linq;
using Animation;
using MoonSharp.Interpreter;
using Scheduler;
using UnityEngine;
using Random = UnityEngine.Random;

[MoonSharpUserData]
public class DroneController
{
    public List<MiningDroneController> DroneShips;

    private ScheduledEvent DroneVisitEvent;

    private ScheduledEvent DroneReturnEvent;

    private enum ShipDir
    {
        N,
        E,
        S,
        W
    }

    private ShipDir lastDir;

    public DroneController()
    {
        DroneShips = new List<MiningDroneController>();
    }

    public void CallMiningDrone(NestedObject landingPad)
    {
        Debug.Log("CallMiningDrone");
        DronePrototype prototype = PrototypeManager.Drone[Random.Range(0, PrototypeManager.Drone.Count - 1)];
        Drone drone = prototype.CreateDrone();

        GameObject go = new GameObject(drone.name);
        go.transform.parent = WorldController.Instance.transform;
        MiningDroneController controller = go.AddComponent<MiningDroneController>();
        DroneShips.Add(controller);
        controller.Drone = drone;
        controller.Speed = 3f;
        controller.parentObject = landingPad;

        // Figure out where the tradeship comes from, where it lands and where it leaves
        Vector3 entryPoint = Vector3.zero;
        Vector3 landingPoint = new Vector3(landingPad.Tile.X, landingPad.Tile.Y, 0);
        Vector3 exitPoint = Vector3.zero;
        System.Random rnd = new System.Random();

        // Get Entry Point
        ShipDir entryDir = GetRandomDirection();
        rnd = new System.Random();
        if (entryDir == ShipDir.N)
        {
            int x = rnd.Next(0, World.Current.Width - 1);
            entryPoint = new Vector3(x, World.Current.Height + 100, 0);
        }
        else if (entryDir == ShipDir.S)
        {
            int x = rnd.Next(0, World.Current.Width - 1);
            entryPoint = new Vector3(x, -100, 0);
        }
        else if (entryDir == ShipDir.E)
        {
            int y = rnd.Next(0, World.Current.Height - 1);
            entryPoint = new Vector3(World.Current.Width + 100, y, 0);
        }
        else if (entryDir == ShipDir.W)
        {
            int y = rnd.Next(0, World.Current.Width - 1);
            entryPoint = new Vector3(-100, y, 0);
        }

        // Get Exit Point
        ShipDir exitDir = GetRandomDirection();
        rnd = new System.Random();
        if (exitDir == ShipDir.N)
        {
            int x = rnd.Next(0, World.Current.Width - 1);
            exitPoint = new Vector3(x, World.Current.Height + 100, 0);
        }
        else if (exitDir == ShipDir.S)
        {
            int x = rnd.Next(0, World.Current.Width - 1);
            exitPoint = new Vector3(x, -100, 0);
        }
        else if (exitDir == ShipDir.E)
        {
            int y = rnd.Next(0, World.Current.Height - 1);
            exitPoint = new Vector3(World.Current.Width + 100, y, 0);
        }
        else if (exitDir == ShipDir.W)
        {
            int y = rnd.Next(0, World.Current.Width - 1);
            exitPoint = new Vector3(-100, y, 0);
        }


        go.transform.position = entryPoint;
        controller.LandingCoordinates = new Vector3(landingPad.Tile.X + 2, landingPad.Tile.Y + 2, 0);
        controller.LeavingCoordinates = exitPoint;

        go.transform.localScale = new Vector3(1, 1, 1);
        SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = SpriteManager.GetSprite("Trader", prototype.AnimationIdle.CurrentFrameName);
        spriteRenderer.sortingLayerName = "TradeShip";

        controller.AnimationFlying = prototype.AnimationFlying.Clone();
        controller.AnimationIdle = prototype.AnimationIdle.Clone();
        controller.Renderer = spriteRenderer;
    }

    public void DropOffItems(NestedObject landingPoint, Vector3 tradingCoordinates)
    {
        foreach (List<Inventory> inventories in landingPoint.InternalInventory.Values)
        {
            foreach (Inventory inventory in inventories)
            {
                if (landingPoint.GetTotalInternalInventory() > 0)
                {
                    Tile tile = World.Current.GetTileAt((int)tradingCoordinates.x, (int)tradingCoordinates.y, (int)tradingCoordinates.z);
                    Inventory inv = new Inventory(inventory.Type, inventory.StackSize, inventory.MaxStackSize);
                    World.Current.InventoryManager.PlaceInventoryAround(tile, inv, 8);
                }
            }
        }
    }

    public void MiningComplete(NestedObject parentObject)
    {
        parentObject.Parameters["mine_complete"].SetValue("true");

        DroneVisitEvent = new ScheduledEvent(
            "EvaluateMiningDroneVisit",
            EvaluateMiningDroneVisit,
            (int)TimeSpan.FromMinutes(0.5).TotalSeconds,
            false,
            parentObject);

        Scheduler.Scheduler.Current.RegisterEvent(DroneVisitEvent);
    }


    public void ReturningToPlanet(NestedObject parentObject)
    {
        DroneReturnEvent = new ScheduledEvent(
           "EvaluateReturnToPlanet",
           EvaluateReturnToPlanet,
           (int)TimeSpan.FromMinutes(0.5).TotalSeconds,
           false,
           parentObject);

        Scheduler.Scheduler.Current.RegisterEvent(DroneReturnEvent);
    }

    private void EvaluateMiningDroneVisit(ScheduledEvent scheduledEvent)
    {
        CallMiningDrone(scheduledEvent.ParentObject);
    }

    private void EvaluateReturnToPlanet(ScheduledEvent scheduledEvent)
    {
        scheduledEvent.ParentObject.Parameters["mine_complete"].SetValue("false");
        scheduledEvent.ParentObject.InternalInventory = new Dictionary<string, List<Inventory>>();
    }

    private ShipDir GetRandomDirection()
    {
        System.Random rnd = new System.Random();
        int dir;
        ShipDir endDir;

        dir = rnd.Next(0, 3);
        if ((ShipDir)dir == lastDir)
        {
            endDir = GetRandomDirection();
        }
        else
        {
            endDir = (ShipDir)dir;
            lastDir = endDir;
        }

        return endDir;
    }
}
