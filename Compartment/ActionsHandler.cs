using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExileCore2;
using ExileCore2.PoEMemory;
using ExileCore2.Shared;
using ExileCore2.Shared.Enums;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.PoEMemory.Components;
// using Stashie.Classes;
using static MapCrafter.MapCrafterCore;
using MapCrafter.Classes;
using Vector2N = System.Numerics.Vector2;
using System.Collections.Generic;

namespace MapCrafter.Compartments;

internal class ActionsHandler
{

    public static void HandleSwitchToTabEvent(object tab)
    {
        Func<SyncTask<bool>> task = null;
        switch (tab)
        {
            case int index:
                task = () => ActionCoRoutine.ProcessSwitchToTab(index);
                break;

            case string name:
                if (!RenamedAllStashNames.Contains(name))
                {
                    DebugWindow.LogMsg($"{Main.Name}: can't find tab with name '{name}'.");
                    break;
                }

                var tempIndex = RenamedAllStashNames.IndexOf(name);
                task = () => ActionCoRoutine.ProcessSwitchToTab(tempIndex);
                DebugWindow.LogMsg($"{Main.Name}: Switching to tab with index: {tempIndex} ('{name}').");
                break;

            default:
                DebugWindow.LogMsg("The received argument is not a string or an integer.");
                break;
        }

        if (task != null) TaskRunner.Run(task, CoroutineName);
    }
    public static int GetIndexOfCurrentVisibleTab()
    {
        return Main.GameController.Game.IngameState.IngameUi.StashElement.IndexVisibleStash;
    }

    public static void CleanUp()
    {
        Input.KeyUp(Keys.LControlKey);
        Input.KeyUp(Keys.Shift);
    }

    public static async SyncTask<bool> PressKey(Keys key, int repetitions = 1)
    {
        for (var i = 0; i < repetitions; i++)
        {
            Input.KeyDown(key);
            await Task.Delay(10);
            Input.KeyUp(key);
            await Task.Delay(10);
        }

        return true;
    }

    public static async SyncTask<bool> Delay(int ms = 0)
    {
        await Task.Delay(Main.Settings.DistilMap.ExtraDelay.Value + ms);
        return true;
    }

    public static async SyncTask<bool> SwitchToTab(int tabIndex)
    {
        Main.VisibleStashIndex = GetIndexOfCurrentVisibleTab();
        var travelDistance = Math.Abs(tabIndex - Main.VisibleStashIndex);
        if (travelDistance == 0)
        {
            return true;
        }

        Main.LogMessage($"Current index {Main.VisibleStashIndex}, need to switch to index {tabIndex}");
        await SwitchToTabViaArrowKeys(tabIndex);

        await Delay();
        return true;
    }

    public static async SyncTask<bool> SwitchToTabViaArrowKeys(int tabIndex, int numberOfTries = 1)
    {
        if (numberOfTries >= 3) return true;

        var indexOfCurrentVisibleTab = GetIndexOfCurrentVisibleTab();
        var travelDistance = tabIndex - indexOfCurrentVisibleTab;
        var tabIsToTheLeft = travelDistance < 0;
        travelDistance = Math.Abs(travelDistance);

        if (tabIsToTheLeft)
            await PressKey(Keys.Left, travelDistance);
        else
            await PressKey(Keys.Right, travelDistance);

        if (GetIndexOfCurrentVisibleTab() != tabIndex)
        {
            await Delay(20);
            await SwitchToTabViaArrowKeys(tabIndex, numberOfTries + 1);
        }

        return true;
    }

    public static async SyncTask<bool> DistilItemIncrementer()
    {
        await DistilMap();
        return true;
    }

    public static async SyncTask<bool> PrepMapItemIncrementer()
    {
        await PrepMap();
        return true;
    }
    public static async SyncTask<bool> CorruptItemIncrementer()
    {
        await CorruptMap();
        return true;
    }

    public static async SyncTask<bool> DistilMap()
    {
        Main.PublishEvent("distil_start_process", null);
        if (Main.DistilItems.Count == 0)
        {
            return false;
        }
        var totalDistilItemLeft = Main.DistilItems.Sum(item => item.StackSize);

        var itemsSortedByDelirious = Main.WaystoneItems
            .OrderBy(x => x.DeliriousLeft)
            .ToList();

        Input.KeyDown(Keys.LControlKey);
        Main.LogMessage($"Want to distil {itemsSortedByDelirious.Count} items.");

        var distilContainer = Main.GameController.Game.IngameState.IngameUi.AnointingWindow.GetChildAtIndex(3);
        if (distilContainer == null)
            return false;
        var distilBtn = distilContainer.GetChildAtIndex(5).GetClientRect().Center;
        var resultRect = distilContainer.GetChildAtIndex(6).GetClientRect().Center;

        foreach (var mapItem in itemsSortedByDelirious)
        {
            await DistilMap(mapItem);
            // mapItem.location = ItemLocation.AnoitingWindow;
            var needDistilCount = mapItem.DeliriousLeft;
            Main.LogMessage($"Map need {needDistilCount} distil.");

            if (totalDistilItemLeft < needDistilCount)
            {
                Main.LogMessage($"Lack of distil item, breaking");
                return false;
            }

            while (needDistilCount > 0 && totalDistilItemLeft >= needDistilCount)
            {
                totalDistilItemLeft--;
                needDistilCount--;
                var distilItem = Main.DistilItems.FirstOrDefault(x => x.StackSize > 0);
                distilItem.StackSize--;
                await SelectDistil(distilItem);
            }

            Main.LogMessage($"Press distil item");
            await ClickAtPos(distilBtn);
            Main.LogMessage($"Retrieve map");
            await ClickAtPos(resultRect);
            // mapItem.location = ItemLocation.Inventory;
            Main.DebugTimer.Restart();
        }

        return true;
    }

    public static async SyncTask<bool> DistilMap(WaystoneItem mapItem)
    {
        Main.LogMessage($"Insert map to distil");
        Input.SetCursorPos(mapItem.ClickPos + Main.ClickWindowOffset);
        await Task.Delay(Main.Settings.HoverItemDelay);

        // var shiftUsed = false;
        // if (stashResult.ShiftForStashing)
        // {
        //     Input.KeyDown(Keys.ShiftKey);
        //     shiftUsed = true;
        // }

        Input.Click(MouseButtons.Left);
        // if (shiftUsed) Input.KeyUp(Keys.ShiftKey);

        await Task.Delay(Main.Settings.DistilMap.StashItemDelay);
        return true;
    }

    public static async SyncTask<bool> SelectDistil(DistilItem distilItem)
    {
        Main.LogMessage($"Insert distil item");
        Input.SetCursorPos(distilItem.ClickPos + Main.ClickWindowOffset);
        await Task.Delay(Main.Settings.HoverItemDelay);

        // var shiftUsed = false;
        // if (stashResult.ShiftForStashing)
        // {
        //     Input.KeyDown(Keys.ShiftKey);
        //     shiftUsed = true;
        // }

        Input.Click(MouseButtons.Left);
        // if (shiftUsed) Input.KeyUp(Keys.ShiftKey);

        await Task.Delay(Main.Settings.DistilMap.StashItemDelay);
        return true;
    }

    public static async SyncTask<bool> PrepMap()
    {
        Main.PublishEvent("prepmap_start_process", null);
        if (Main.WaystoneItems.Count == 0)
        {
            return false;
        }
        // var totalDistilItemLeft = Main.DistilItems.Sum(item => item.StackSize);

        var itemSorted = Main.WaystoneItems
            .OrderBy(x => x.ExaltLeft)
            .ToList();
        Main.LogMessage($"Want to prep {itemSorted.Count} items.");

        var stashInventories = Main.GameController.Game.IngameState.IngameUi.StashElement.StashTabContainer.Inventories;
        var currencyIndex = stashInventories.FindIndex(x => x.Inventory != null && x.Inventory.InvType == InventoryType.CurrencyStash);

        await SwitchToTab(currencyIndex);
        await TaskUtils.CheckEveryFrameWithThrow(
                () => Main.GameController.IngameState.IngameUi.StashElement.AllInventories[Main.VisibleStashIndex] !=
                      null,
                new CancellationTokenSource(Main.Settings.PrepMap.StashingCancelTimer.Value).Token);
        //maybe replace waittime with Setting option

        await TaskUtils.CheckEveryFrameWithThrow(
            () => GetTypeOfCurrentVisibleStash() != InventoryType.InvalidInventory,
            new CancellationTokenSource(Main.Settings.PrepMap.StashingCancelTimer.Value).Token);
        // await Task.Delay(Main.Settings.PrepMap.PrepmapDelay);

        var stashPanel = Main.GameController.Game.IngameState?.IngameUi?.StashElement;
        var visibleStash = stashPanel.VisibleStash;
        var currencyInventoryItems = visibleStash.VisibleInventoryItems;
        // Switch to currency stash tab

        var needAugmentCount = Main.WaystoneItems.Count(x => x.NeedAugment);
        var needAlchemyCount = Main.WaystoneItems.Count(x => x.NeedAlchemy);
        var needRegalCount = Main.WaystoneItems.Count(x => x.NeedRegal);
        var needExaltCount = Main.WaystoneItems.FindAll(x => x.NeedExalt).Sum(x => x.ExaltLeft);


        // Input.KeyDown(Keys.LShiftKey);

        var augmentItemSlot = currencyInventoryItems.FirstOrDefault(x => x?.Item?.GetComponent<Base>()?.Name == "Orb of Augmentation");

        var alchemyItemSlot = currencyInventoryItems.FirstOrDefault(x => x?.Item?.GetComponent<Base>()?.Name == "Orb of Alchemy");

        var regalItemSlot = currencyInventoryItems.FirstOrDefault(x => x?.Item?.GetComponent<Base>()?.Name == "Regal Orb");
        var regalStackSize = regalItemSlot?.Item.GetComponent<Stack>().Size ?? 0;

        var exaltItemSlot = currencyInventoryItems.FirstOrDefault(x => x?.Item?.GetComponent<Base>()?.Name == "Exalted Orb");
        var exaltStackSize = exaltItemSlot?.Item.GetComponent<Stack>().Size ?? 0;


        Main.LogMessage($"Need to prep {needAugmentCount} augment, {needAlchemyCount} alchemy, {needRegalCount} regal and {needExaltCount} exalt.");

        // Process augment
        if (needAugmentCount > 0 && augmentItemSlot == null)
        {
            Main.LogMessage($"Breaking due to need Augment | null slot");
            return false;
        }
        var augmentStackSize = augmentItemSlot.Item.GetComponent<Stack>().Size;
        if (needAugmentCount > 0 && augmentStackSize < needAugmentCount)
        {
            Main.LogMessage($"Breaking due to need Augment | {augmentStackSize} stack vs {needAugmentCount} count");
            return false;
        }
        if (itemSorted.Any(x => x.NeedAugment))
        {
            await SelectCurrency(augmentItemSlot.GetClientRect().Center);
            Input.KeyDown(Keys.ShiftKey);
            foreach (var mapItem in itemSorted)
            {
                if (!mapItem.NeedAugment)
                    continue;

                await ApplyCurrency(mapItem.ClickPos);
            }
            Input.KeyUp(Keys.ShiftKey);
            await Task.Delay(Main.Settings.PrepMap.PrepmapDelay);
        }


        // Process Alchemy
        if (needAlchemyCount > 0 && alchemyItemSlot == null)
        {
            Main.LogMessage($"Breaking due to need Alchemy | null slot");
            return false;
        }
        var alchemyStackSize = alchemyItemSlot?.Item.GetComponent<Stack>().Size;
        if (needAlchemyCount > 0 && alchemyStackSize < needAlchemyCount)
        {
            Main.LogMessage($"Breaking due to need Alchemy | {alchemyStackSize} stack vs {needAlchemyCount} count");
            return false;
        }

        if (itemSorted.Any(x => x.NeedAlchemy))
        {
            await SelectCurrency(alchemyItemSlot.GetClientRect().Center);
            Input.KeyDown(Keys.ShiftKey);
            foreach (var mapItem in itemSorted)
            {
                if (!mapItem.NeedAlchemy)
                    continue;

                await ApplyCurrency(mapItem.ClickPos);
            }
            Input.KeyUp(Keys.ShiftKey);
            await Task.Delay(Main.Settings.PrepMap.PrepmapDelay);
        }

        // Process Regal
        if (needRegalCount > 0 && (regalItemSlot == null || regalStackSize < needRegalCount))
        {
            Main.LogMessage($"Breaking due to need Regal | {regalStackSize} stack vs {needRegalCount} count");
            return false;
        }


        if (itemSorted.Any(x => x.NeedRegal))
        {
            await SelectCurrency(regalItemSlot.GetClientRect().Center);
            Input.KeyDown(Keys.ShiftKey);
            foreach (var mapItem in itemSorted)
            {
                if (!mapItem.NeedRegal)
                    continue;

                await ApplyCurrency(mapItem.ClickPos);
            }
            Input.KeyUp(Keys.ShiftKey);
            await Task.Delay(Main.Settings.PrepMap.PrepmapDelay);
        }

        // Process Exalt
        if (needExaltCount > 0 && (exaltItemSlot == null || exaltStackSize < needExaltCount))
        {
            Main.LogMessage($"Breaking due to need Exalt | {exaltStackSize} stack vs {needExaltCount} count");
            return false;
        }

        if (itemSorted.Any(x => x.NeedExalt))
        {
            await SelectCurrency(exaltItemSlot.GetClientRect().Center);
            Input.KeyDown(Keys.ShiftKey);
            foreach (var mapItem in itemSorted)
            {
                if (!mapItem.NeedExalt)
                    continue;

                await ApplyCurrency(mapItem.ClickPos, mapItem.ExaltLeft);
            }
            Input.KeyUp(Keys.ShiftKey);
            await Task.Delay(Main.Settings.PrepMap.PrepmapDelay);
        }


        return true;
    }

    public static async SyncTask<bool> CorruptMap()
    {
        Main.PublishEvent("corruptmap_start_process", null);
        if (Main.WaystoneItems.Count == 0)
        {
            return false;
        }
        // var totalDistilItemLeft = Main.DistilItems.Sum(item => item.StackSize);

        var itemSorted = Main.WaystoneItems;
        Main.LogMessage($"Want to corrupt {itemSorted.Count} items.");

        var stashInventories = Main.GameController.Game.IngameState.IngameUi.StashElement.StashTabContainer.Inventories;
        var currencyIndex = stashInventories.FindIndex(x => x.Inventory.InvType == InventoryType.CurrencyStash);

        await SwitchToTab(currencyIndex);
        await TaskUtils.CheckEveryFrameWithThrow(
                () => Main.GameController.IngameState.IngameUi.StashElement.AllInventories[Main.VisibleStashIndex] !=
                      null,
                new CancellationTokenSource(Main.Settings.PrepMap.StashingCancelTimer.Value).Token);
        //maybe replace waittime with Setting option

        await TaskUtils.CheckEveryFrameWithThrow(
            () => GetTypeOfCurrentVisibleStash() != InventoryType.InvalidInventory,
            new CancellationTokenSource(Main.Settings.PrepMap.StashingCancelTimer.Value).Token);
        // await Task.Delay(Main.Settings.PrepMap.PrepmapDelay);

        var stashPanel = Main.GameController.Game.IngameState?.IngameUi?.StashElement;
        var visibleStash = stashPanel.VisibleStash;
        var currencyInventoryItems = visibleStash.VisibleInventoryItems;
        // Switch to currency stash tab


        // Input.KeyDown(Keys.LShiftKey);

        var vaalItemSlot = currencyInventoryItems.FirstOrDefault(x => x?.Item?.GetComponent<Base>()?.Name == "Vaal Orb");


        // Main.LogMessage($"Need to prep {needAugmentCount} augment, {needAlchemyCount} alchemy, {needRegalCount} regal and {needExaltCount} exalt.");

        // Process augment
        if (itemSorted.Count > 0 && vaalItemSlot == null)
        {
            Main.LogMessage($"Breaking due to need Vaal | null slot");
            return false;
        }
        var vaalStackSize = vaalItemSlot.Item.GetComponent<Stack>().Size;
        if (itemSorted.Count > 0 && vaalStackSize < itemSorted.Count)
        {
            Main.LogMessage($"Breaking due to need Vaal | {vaalStackSize} stack vs {itemSorted.Count} count");
            return false;
        }
        await SelectCurrency(vaalItemSlot.GetClientRect().Center);
        Input.KeyDown(Keys.ShiftKey);
        foreach (var mapItem in itemSorted)
        {
            await ApplyCurrency(mapItem.ClickPos);
        }
        Input.KeyUp(Keys.ShiftKey);
        await Task.Delay(Main.Settings.PrepMap.PrepmapDelay);

        return true;
    }

    public static async SyncTask<bool> PrepAugment(WaystoneItem mapItem)
    {
        // Main.LogMessage($"Insert map to distil");


        Input.SetCursorPos(mapItem.ClickPos + Main.ClickWindowOffset);
        await Task.Delay(Main.Settings.HoverItemDelay);

        // var shiftUsed = false;
        // if (stashResult.ShiftForStashing)
        // {
        //     Input.KeyDown(Keys.ShiftKey);
        //     shiftUsed = true;
        // }

        Input.Click(MouseButtons.Left);
        // if (shiftUsed) Input.KeyUp(Keys.ShiftKey);

        await Task.Delay(Main.Settings.DistilMap.StashItemDelay);
        return true;
    }

    public static async SyncTask<bool> SelectCurrency(Vector2N position)
    {
        Input.SetCursorPos(position + Main.ClickWindowOffset);
        await Task.Delay(Main.Settings.HoverItemDelay);
        Input.Click(MouseButtons.Right);
        await Task.Delay(Main.Settings.HoverItemDelay);
        return true;
    }

    public static async SyncTask<bool> ApplyCurrency(Vector2N position, int count = 1)
    {
        var applyTime = count;
        while (applyTime > 0)
        {
            Input.SetCursorPos(position + Main.ClickWindowOffset);
            await Task.Delay(Main.Settings.HoverItemDelay);
            Input.Click(MouseButtons.Left);
            await Task.Delay(Main.Settings.DistilMap.DistilDelay);
            applyTime--;
        }
        return true;
    }



    public static async SyncTask<bool> ClickAtPos(Vector2N position)
    {
        Input.SetCursorPos(position + Main.ClickWindowOffset);
        await Task.Delay(Main.Settings.HoverItemDelay);
        Input.Click(MouseButtons.Left);
        await Task.Delay(Main.Settings.DistilMap.DistilDelay);
        return true;
    }

    public static InventoryType GetTypeOfCurrentVisibleStash()
    {
        var stashPanelVisibleStash = Main.GameController.Game.IngameState.IngameUi?.StashElement?.VisibleStash;
        return stashPanelVisibleStash?.InvType ?? InventoryType.InvalidInventory;
    }

}