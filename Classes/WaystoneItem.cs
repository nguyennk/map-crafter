using ItemFilterLibrary;
using Vector2N = System.Numerics.Vector2;

namespace MapCrafter.Classes;

public class WaystoneItem(ItemData itemData, int deliriousLeft, Vector2N clickPos)
{
    public ItemData ItemData { get; } = itemData;
    public int DeliriousLeft { get; } = deliriousLeft;
    public int ExaltLeft { get; set; } = 4;
    public bool NeedAugment { get; set; } = false;
    public bool NeedAlchemy { get; set; } = false;
    public bool NeedRegal { get; set; } = false;
    public bool NeedExalt { get; set; } = false;
    public Vector2N ClickPos { get; } = clickPos;
}