using ItemFilterLibrary;
using Vector2N = System.Numerics.Vector2;

namespace MapCrafter.Classes;

public class DistilItem(ItemData itemData, Vector2N clickPos)
{
    public ItemData ItemData { get; } = itemData;
    public int StackSize { get; set; } = itemData.StackInfo.Count;
    public Vector2N ClickPos { get; } = clickPos;
}