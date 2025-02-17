using System;

namespace MapCrafter.Classes;

public class ModifierCheck(string display_name, int prop_1, int prop_2 = 0)
{
    public string DisplayName { get; } = display_name;
    public int Value_1 { get; } = prop_1;
    public int Value_2 { get; } = prop_2;
}