using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using ExileCore2;
using ExileCore2.Shared;
using ExileCore2.Shared.Enums;
using ExileCore2.PoEMemory.Components;

using ItemFilterLibrary;
using static MapCrafter.MapCrafterCore;
using MapCrafter.Classes;

namespace MapCrafter.Compartments;

public static class TaskRunner
{
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> Tasks = [];

    public static void Run(Func<SyncTask<bool>> task, string name)
    {
        var cts = new CancellationTokenSource();
        Tasks[name] = cts;
        Task.Run(async () =>
        {
            var sTask = task();
            while (sTask != null && !cts.Token.IsCancellationRequested)
            {
                TaskUtils.RunOrRestart(ref sTask, () => null);
                await TaskUtils.NextFrame();
            }

            Tasks.TryRemove(new KeyValuePair<string, CancellationTokenSource>(name, cts));
        });
    }

    public static void Stop(string name)
    {
        if (Tasks.TryGetValue(name, out var cts))
        {
            cts.Cancel();
            Tasks.TryRemove(new KeyValuePair<string, CancellationTokenSource>(name, cts));
        }
    }

    public static bool Has(string name)
    {
        return Tasks.ContainsKey(name);
    }
}

internal class ActionCoRoutine
{

    public static async SyncTask<bool> ProcessSwitchToTab(int index)
    {
        Main.DebugTimer.Restart();
        await ActionsHandler.SwitchToTab(index);
        TaskRunner.Stop(CoroutineName);

        Main.DebugTimer.Restart();
        Main.DebugTimer.Stop();
        return true;
    }

    public static void StartDistilingCoroutine()
    {
        Main.DebugTimer.Reset();
        Main.DebugTimer.Start();
        TaskRunner.Run(DistillRoutine, "Distil_Distiling");
    }

    public static void StartPrepmapCoroutine()
    {
        Main.DebugTimer.Reset();
        Main.DebugTimer.Start();
        TaskRunner.Run(PrepmapRoutine, "Distil_Prepmap");
    }
    public static void StartCorruptCoroutine()
    {
        Main.DebugTimer.Reset();
        Main.DebugTimer.Start();
        TaskRunner.Run(CorruptRoutine, "Distil_Corruptmap");
    }

    public static void StopCoroutine(string routineName)
    {
        TaskRunner.Stop(routineName);
        Main.DebugTimer.Stop();
        Main.DebugTimer.Reset();
        ActionsHandler.CleanUp();
        Main.PublishEvent("distil_finish_process", null);
    }

    public static async SyncTask<bool> DistillRoutine()
    {
        var cursorPosPreMoving = Input.ForceMousePosition;

        // get list of map in inventory that need distilling

        // get list of distil items


        //try stashing items 3 times
        // var originTab = ActionsHandler.GetIndexOfCurrentVisibleTab();
        await ParseDistilItems();
        for (var tries = 0; tries < 1 && Main.WaystoneItems.Count > 0; ++tries)
        {
            if (Main.WaystoneItems.Count > 0)
                await ActionsHandler.DistilItemIncrementer();

            // await FilterManager.ParseItems();
            await Task.Delay(Main.Settings.DistilMap.ExtraDelay);
        }

        Input.SetCursorPos(cursorPosPreMoving);
        Input.MouseMove();
        StopCoroutine("Distil_Distiling");
        return true;
    }

    public static async SyncTask<bool> ParseDistilItems()
    {
        // var _serverData = Main.GameController.Game.IngameState.Data.ServerData;
        // var invItems = _serverData.PlayerInventories[0].Inventory.InventorySlotItems;
        var panel = Main.GameController.Game.IngameState.IngameUi.InventoryPanel;
        var invItems = panel[InventoryIndex.PlayerInventory].VisibleInventoryItems;

        await TaskUtils.CheckEveryFrameWithThrow(() => invItems != null, new CancellationTokenSource(500).Token);

        Main.WaystoneItems = [];
        Main.DistilItems = [];

        Main.ClickWindowOffset = Main.GameController.Window.GetWindowRectangle().TopLeft;

        foreach (var invItem in invItems)
        {
            if (invItem.Item == null || invItem.Address == 0)
                continue;

            var testItem = new ItemData(invItem.Item, Main.GameController);
            var isMap = testItem.MapInfo.IsMap && testItem.BaseName == "Waystone (Tier 15)";
            var isDistil = testItem.Path.Contains("Metadata/Items/Currency/DistilledEmotion");

            if (isMap && !testItem.IsCorrupted)
            {
                var deliriousLeft = 3 - testItem.ModsNames.Count(m => m.Contains("InstilledMapDelirium"));
                if (deliriousLeft > 0)
                    Main.WaystoneItems.Add(new WaystoneItem(testItem, deliriousLeft, invItem.GetClientRect().Center));
            }


            if (isDistil)
                Main.DistilItems.Add(new DistilItem(testItem, invItem.GetClientRect().Center));
        }
        return true;
    }

    public static async SyncTask<bool> PrepmapRoutine()
    {
        var cursorPosPreMoving = Input.ForceMousePosition;

        // get list of map need augment

        // get list of map need alchemy

        // get list of map need regal

        // get list of map need exalted

        await ParseInventoryMapItems();

        // run prep map to finish up exal map


        //try stashing items 3 times
        // var originTab = ActionsHandler.GetIndexOfCurrentVisibleTab();
        for (var tries = 0; tries < 1 && Main.WaystoneItems.Count > 0; ++tries)
        {
            if (Main.WaystoneItems.Count > 0)
                await ActionsHandler.PrepMapItemIncrementer();

            // await FilterManager.ParseItems();
            await Task.Delay(Main.Settings.DistilMap.ExtraDelay);
        }

        Input.SetCursorPos(cursorPosPreMoving);
        Input.MouseMove();
        StopCoroutine("Distil_Prepmap");
        return true;
    }

    public static async SyncTask<bool> ParseInventoryMapItems()
    {
        // var _serverData = Main.GameController.Game.IngameState.Data.ServerData;
        // var invItems = _serverData.PlayerInventories[InventoryIndex.PlayerInventory].Inventory.InventorySlotItems;
        var panel = Main.GameController.Game.IngameState.IngameUi.InventoryPanel;
        var invItems = panel[InventoryIndex.PlayerInventory].VisibleInventoryItems;

        await TaskUtils.CheckEveryFrameWithThrow(() => invItems != null, new CancellationTokenSource(500).Token);

        Main.WaystoneItems = [];
        Main.DistilItems = [];

        Main.ClickWindowOffset = Main.GameController.Window.GetWindowRectangle().TopLeft;

        foreach (var invItem in invItems)
        {
            if (invItem.Item == null || invItem.Address == 0)
                continue;

            var testItem = new ItemData(invItem.Item, Main.GameController);
            var isMap = testItem.MapInfo.IsMap && testItem.BaseName == "Waystone (Tier 15)";
            // var isDistil = testItem.Path.Contains("Metadata/Items/Currency/DistilledEmotion");

            if (isMap && !testItem.IsCorrupted)
            {
                var waystoneItem = new WaystoneItem(testItem, 0, invItem.GetClientRect().Center);
                var shouldAdd = false;
                var deliriousCount = testItem.ModsNames.Count(m => m.Contains("InstilledMapDelirium"));
                var enchantedCount = testItem.ModsNames.Count - deliriousCount;

                if (enchantedCount < 6)
                {
                    if (testItem.Rarity == ItemRarity.Rare)
                        waystoneItem.ExaltLeft = 6 - enchantedCount;
                    else
                        waystoneItem.ExaltLeft = 4;
                }
                else
                {
                    waystoneItem.ExaltLeft = 0;
                }

                if (waystoneItem.ExaltLeft > 0)
                {
                    shouldAdd = true;
                    if (testItem.Rarity == ItemRarity.Normal)
                    {
                        waystoneItem.NeedAlchemy = true;
                        waystoneItem.NeedExalt = true;

                    }
                    else if (testItem.Rarity == ItemRarity.Magic)
                    {
                        if (enchantedCount == 1)
                        {
                            waystoneItem.NeedAugment = true;
                        }
                        waystoneItem.NeedRegal = true;
                        waystoneItem.NeedExalt = true;
                    }
                    else if (testItem.Rarity == ItemRarity.Rare)
                    {
                        waystoneItem.NeedExalt = true;
                    }
                }

                if (shouldAdd)
                    Main.WaystoneItems.Add(waystoneItem);
            }
        }
        return true;
    }

    public static async SyncTask<bool> CorruptRoutine()
    {
        var cursorPosPreMoving = Input.ForceMousePosition;

        // get list of map need augment

        // get list of map need alchemy

        // get list of map need regal

        // get list of map need exalted

        await ParseCorruptMapItems();

        // run prep map to finish up exal map


        //try stashing items 3 times
        // var originTab = ActionsHandler.GetIndexOfCurrentVisibleTab();
        for (var tries = 0; tries < 1 && Main.WaystoneItems.Count > 0; ++tries)
        {
            if (Main.WaystoneItems.Count > 0)
                await ActionsHandler.CorruptItemIncrementer();

            // await FilterManager.ParseItems();
            await Task.Delay(Main.Settings.DistilMap.ExtraDelay);
        }

        Input.SetCursorPos(cursorPosPreMoving);
        Input.MouseMove();
        StopCoroutine("Distil_Corruptmap");
        return true;
    }

    public static async SyncTask<bool> ParseCorruptMapItems()
    {
        // var _serverData = Main.GameController.Game.IngameState.Data.ServerData;
        // var invItems = _serverData.PlayerInventories[0].Inventory.InventorySlotItems;
        var panel = Main.GameController.Game.IngameState.IngameUi.InventoryPanel;
        var invItems = panel[InventoryIndex.PlayerInventory].VisibleInventoryItems;

        await TaskUtils.CheckEveryFrameWithThrow(() => invItems != null, new CancellationTokenSource(500).Token);

        Main.WaystoneItems = [];
        Main.DistilItems = [];

        Main.ClickWindowOffset = Main.GameController.Window.GetWindowRectangle().TopLeft;

        foreach (var invItem in invItems)
        {
            if (invItem.Item == null || invItem.Address == 0)
                continue;

            var testItem = new ItemData(invItem.Item, Main.GameController);
            var isMap = testItem.MapInfo.IsMap && testItem.BaseName == "Waystone (Tier 15)";
            // var isDistil = testItem.Path.Contains("Metadata/Items/Currency/DistilledEmotion");

            if (isMap && !testItem.IsCorrupted)
            {
                var waystoneItem = new WaystoneItem(testItem, 0, invItem.GetClientRect().Center);
                var deliriousCount = testItem.ModsNames.Count(m => m.Contains("InstilledMapDelirium"));
                var enchantedCount = testItem.ModsNames.Count - deliriousCount;

                if (enchantedCount < 6 || testItem.Rarity != ItemRarity.Rare || deliriousCount < 3)
                {
                    continue;
                }

                // Process Mods
                var itemMods = invItem.Item.GetComponent<Mods>();
                var shouldIgnore = false;
                foreach (var mod in itemMods.ItemMods)
                {
                    foreach (var ignoreMod in Main.IgnoreModifiers)
                    {
                        if (
                            mod.DisplayName.Contains(
                                ignoreMod.DisplayName,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            if (mod.Values[0] >= ignoreMod.Value_1)
                            {
                                shouldIgnore = true;
                                break;
                            }
                            if (mod.Values.Count() > 1)
                            {
                                if (mod.Values[1] >= ignoreMod.Value_2)
                                {
                                    shouldIgnore = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (shouldIgnore)
                    {
                        break;
                    }
                }


                if (!shouldIgnore)
                    Main.WaystoneItems.Add(waystoneItem);
            }
        }
        return true;
    }
}