using System.Drawing;
using System.Windows.Forms;
using ExileCore2;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using Newtonsoft.Json;

namespace MapCrafter;

public class MapCrafterSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(false);

    [Menu("Exalt Map Hotkey")]
    public HotkeyNode PrepHotkey { get; set; } = Keys.F5;
    [Menu("Distil Hotkey")]
    public HotkeyNode DropHotkey { get; set; } = Keys.F6;
    [Menu("Corrupt Map Hotkey")]
    public HotkeyNode CorruptHotkey { get; set; } = Keys.F7;

    [Menu("HoverItem Delay", "Delay used to wait between checks for the Hover item (in ms).")]
    public RangeNode<int> HoverItemDelay { get; set; } = new(50, 0, 2000);



    [Menu("Exalt Map Settings")]
    public PrepMapSettings PrepMap { get; set; } = new PrepMapSettings();

    [Menu("Distil Map Settings")]
    public DistilSettings DistilMap { get; set; } = new DistilSettings();

    [Menu("Corrupt Map Settings")]
    public CorruptSettings CorrupMap { get; set; } = new CorruptSettings();

}


[Submenu(CollapsedByDefault = false)]
public class PrepMapSettings
{
    [Menu("Select Currency Delay", "Delay used to wait after select currency(in ms).")]
    public RangeNode<int> CurrencyDelay { get; set; } = new(50, 0, 2000);
    [Menu("Prepmap Delay", "Delay used to wait after each round of currency apply currency(in ms).")]
    public RangeNode<int> PrepmapDelay { get; set; } = new(200, 0, 2000);

    [Menu("Cancel Timer", "Time to wait while premap before canceling (in ms).")]
    public RangeNode<int> StashingCancelTimer { get; set; } = new(2000, 0, 15000);
}


[Submenu(CollapsedByDefault = false)]
public class DistilSettings
{
    [Menu("Distil Map Delay", "Delay used to wait after moving the mouse on an item to distil until clicking it(in ms).")]
    public RangeNode<int> StashItemDelay { get; set; } = new(50, 0, 2000);

    [Menu("Extra Delay", "Delay to wait after each distil attempt(in ms).")]
    public RangeNode<int> ExtraDelay { get; set; } = new(50, 0, 2000);

    [Menu("Distil Delay", "Delay used to wait after click Distil Map(in ms).")]
    public RangeNode<int> DistilDelay { get; set; } = new(200, 0, 2000);
}


[Submenu(CollapsedByDefault = false)]
public class CorruptSettings
{
    [Menu(
        "Ignore modifier",
        "Mods you want to avoid corrupt, separated with ',' \n Locate them by alt-clicking on item and hovering over affix tier on the right"
    )]
    public TextNode IgnoreModifiers { get; set; } =
        new TextNode("plundering_20_70, collector_70, populated_30, brimming_30");
    [JsonIgnore]
    public ButtonNode UpdateModifier { get; set; } = new ButtonNode();
}