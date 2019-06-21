using System;
using System.Collections.Generic;
using System.Linq;
using Animation;
using MoonSharp.Interpreter;
using Scheduler;
using UnityEngine;
using Random = UnityEngine.Random;

[MoonSharpUserData]
public class TradeController
{
    public List<TraderShipController> TradeShips;

    private readonly ScheduledEvent traderVisitEvaluationEvent;

    private enum ShipDir
    {
        N,
        E,
        S,
        W
    }

    private ShipDir lastDir;

    public TradeController()
    {
        TradeShips = new List<TraderShipController>();

        traderVisitEvaluationEvent = new ScheduledEvent(
            "EvaluateTraderVisit",
            EvaluateTraderVisit,
            (int)TimeSpan.FromMinutes(5).TotalSeconds,
            true);
        Scheduler.Scheduler.Current.RegisterEvent(traderVisitEvaluationEvent);
    }
    
    public void CallTradeShipTest(NestedObject landingPad)
    {
        Debug.Log("CallTradeShipTest");
        TraderPrototype prototype = PrototypeManager.Trader[Random.Range(0, PrototypeManager.Trader.Count - 1)];
        Trader trader = prototype.CreateTrader();

        GameObject go = new GameObject(trader.Name);
        go.transform.parent = WorldController.Instance.transform;
        TraderShipController controller = go.AddComponent<TraderShipController>();
        TradeShips.Add(controller);
        controller.Trader = trader;
        controller.Speed = 5f;

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
        controller.LandingCoordinates = new Vector3(landingPad.Tile.X + 1, landingPad.Tile.Y + 1, 0);
        controller.LeavingCoordinates = exitPoint;

        go.transform.localScale = new Vector3(1, 1, 1);
        SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = SpriteManager.GetSprite("Trader", prototype.AnimationIdle.CurrentFrameName);
        spriteRenderer.sortingLayerName = "TradeShip";

        controller.AnimationFlying = prototype.AnimationFlying.Clone();
        controller.AnimationIdle = prototype.AnimationIdle.Clone();
        controller.Renderer = spriteRenderer;
    }

    public void ShowTradeDialogBox(TraderShipController tradeShip)
    {
        DialogBoxManager dbm = GameObject.Find("Dialog Boxes").GetComponent<DialogBoxManager>();

        Trader playerTrader = Trader.FromPlayer(World.Current.Wallet[tradeShip.Trader.Currency.Name]);
        Trade trade = new Trade(playerTrader, tradeShip.Trader);
        dbm.dialogBoxTrade.SetupTrade(trade);
        dbm.dialogBoxTrade.TradeCancelled = () =>
        {
            tradeShip.TradeCompleted = true;
            TradeShips.Remove(tradeShip);
        };
        dbm.dialogBoxTrade.TradeCompleted = () =>
        {
            tradeShip.TradeCompleted = true;
            TrasfertTradedItems(trade, tradeShip.LandingCoordinates);
            TradeShips.Remove(tradeShip);
        };
        dbm.dialogBoxTrade.ShowDialog();
    }

    private void TrasfertTradedItems(Trade trade, Vector3 tradingCoordinates)
    {
        trade.Player.Currency.Balance += trade.TradeCurrencyBalanceForPlayer;

        foreach (TradeItem tradeItem in trade.TradeItems)
        {
            if (tradeItem.TradeAmount > 0)
            {
                Tile tile = World.Current.GetTileAt((int)tradingCoordinates.x, (int)tradingCoordinates.y, (int)tradingCoordinates.z);
                Inventory inv = new Inventory(tradeItem.Type, tradeItem.TradeAmount, tradeItem.TradeAmount);
                World.Current.InventoryManager.PlaceInventoryAround(tile, inv, 6);
            }
            else if (tradeItem.TradeAmount < 0)
            {
                World.Current.InventoryManager.RemoveInventoryOfType(tradeItem.Type, -tradeItem.TradeAmount, true);
            }
        }
    }
    private void EvaluateTraderVisit(ScheduledEvent scheduledEvent)
    {
        NestedObject landingPad = FindRandomLandingPadWithouTrader();

        if (landingPad != null)
        {
            CallTradeShipTest(landingPad);
        }
    }

    private NestedObject FindRandomLandingPadWithouTrader()
    {
        List<NestedObject> landingPads = World.Current.NestedObjectManager.Find(f => f.HasTypeTag("LandingPad"));

        if (landingPads.Any())
        {
            return landingPads[Random.Range(0, landingPads.Count - 1)];
        }

        return null;
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