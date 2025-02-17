using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using ExileCore2;
using ExileCore2.PoEMemory;
// using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.Elements;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.PoEMemory.FilesInMemory;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.PoEMemory.Models;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Cache;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared.Helpers;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using Base = ExileCore2.PoEMemory.Components.Base;
using Map = ExileCore2.PoEMemory.Components.Map;
using Mods = ExileCore2.PoEMemory.Components.Mods;
using RectangleF = ExileCore2.Shared.RectangleF;
using Vector2N = System.Numerics.Vector2;
using MapCrafter.Compartments;
using MapCrafter.Classes;

namespace MapCrafter;

public class MapCrafterCore : BaseSettingsPlugin<MapCrafterSettings>
{
    public static MapCrafterCore Main;
    // private IngameState InGameState => GameController.IngameState;
    public const string CoroutineName = "Map Crafter";
    public static List<string> RenamedAllStashNames;
    public readonly Stopwatch DebugTimer = new();
    public List<WaystoneItem> WaystoneItems;
    public List<DistilItem> DistilItems;
    public List<ModifierCheck> IgnoreModifiers;
    public Vector2N ClickWindowOffset;
    public int VisibleStashIndex = -1;
    public MapCrafterCore()
    {
        Name = "NK Map Crafter";
    }

    public override bool Initialise()
    {
        Main = this;

        Input.RegisterKey(Settings.DropHotkey);
        Input.RegisterKey(Settings.PrepHotkey);
        Input.RegisterKey(Settings.CorruptHotkey);

        Settings.DropHotkey.OnValueChanged += () => { Input.RegisterKey(Settings.DropHotkey); };
        Settings.PrepHotkey.OnValueChanged += () => { Input.RegisterKey(Settings.PrepHotkey); };
        Settings.CorruptHotkey.OnValueChanged += () => { Input.RegisterKey(Settings.CorruptHotkey); };
        Settings.CorrupMap.UpdateModifier.OnPressed = ParseIgnoreModifiers;

        // Settings.DropHotkey.OnPressed += StartDistilling;
        // Settings.PrepHotkey.OnPressed += StartPrepmap;
        ParseIgnoreModifiers();
        return base.Initialise();
    }

    public override void Tick()
    {
        if (!DistilRequirementsMet() && TaskRunner.Has("Distil_Distiling"))
        {
            LogMessage($"Distil requirement not met");
            TaskRunner.Stop("Distil_Distiling");
            return;
        }

        if (!PrepMapRequirementsMet() && TaskRunner.Has("Distil_Prepmap"))
        {
            LogMessage($"Prepmap requirement not met");
            TaskRunner.Stop("Distil_Prepmap");
            return;
        }

        if (!PrepMapRequirementsMet() && TaskRunner.Has("Distil_Corruptmap"))
        {
            LogMessage($"Corruptmap requirement not met");
            TaskRunner.Stop("Distil_Corruptmap");
            return;
        }

        var dropHotkeyPressed = Settings.DropHotkey.PressedOnce();
        var prepmapHotKeyPressed = Settings.PrepHotkey.PressedOnce();
        var corruptHotKeyPressed = Settings.CorruptHotkey.PressedOnce();

        if (!dropHotkeyPressed && !prepmapHotKeyPressed && !corruptHotKeyPressed)
            return;

        if (TaskRunner.Has("Distil_Distiling"))
        {
            LogMessage($"Distil task found, stop coroutine");
            ActionCoRoutine.StopCoroutine("Distil_Distiling");
        }
        // else
        //     ActionCoRoutine.StartDistilingCoroutine();

        if (TaskRunner.Has("Distil_Prepmap"))
        {
            LogMessage($"Prepmap task found, stop coroutine");
            ActionCoRoutine.StopCoroutine("Distil_Prepmap");
        }


        if (TaskRunner.Has("Distil_Corruptmap"))
        {
            LogMessage($"Corrupt task found, stop coroutine");
            ActionCoRoutine.StopCoroutine("Distil_Corruptmap");
        }

        if (corruptHotKeyPressed)
        {
            LogMessage($"Start corrupt coroutine");
            ActionCoRoutine.StartCorruptCoroutine();
        }


        if (dropHotkeyPressed)
        {
            LogMessage($"Start distil coroutine");
            ActionCoRoutine.StartDistilingCoroutine();
        }

        if (prepmapHotKeyPressed)
        {
            LogMessage($"Start prepmap coroutine");
            ActionCoRoutine.StartPrepmapCoroutine();
        }


    }


    public bool DistilRequirementsMet()
    {
        return GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible &&
               GameController.Game.IngameState.IngameUi.AnointingWindow.IsVisibleLocal;
    }

    public bool PrepMapRequirementsMet()
    {
        return GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible &&
               GameController.Game.IngameState.IngameUi.StashElement.IsVisibleLocal;
    }

    private void ParseIgnoreModifiers()
    {
        IgnoreModifiers = [];
        var modifier_list = Settings
            .CorrupMap.IgnoreModifiers.Value.Split(',')
            .Select(x => x.Trim().ToLower())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        foreach (var mod in modifier_list)
        {
            var values = mod.Split('_').ToList();
            if (values.Count() == 2)
            {
                LogMessage($"Add modifier {values[0]} with value {values[1]}");
                IgnoreModifiers.Add(new ModifierCheck(values[0], int.Parse(values[1])));
            }
            else if (values.Count() > 2)
            {
                LogMessage($"Add modifier {values[0]} with value 1 {values[1]}, value 2 {values[2]}");
                IgnoreModifiers.Add(new ModifierCheck(values[0], int.Parse(values[1]), int.Parse(values[2])));
            }
        }
    }

}
