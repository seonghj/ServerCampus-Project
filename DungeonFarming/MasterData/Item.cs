using System;

namespace DungeonFarming.MasterData;

public class Item
{
    public Int32 Code { get; set; } = 0;
    public string Name { get; set; } = null;
    public Int32 Attribute { get; set; } = 0;
    public Int32 Sell{ get; set; } = 0;
    public Int32 Buy { get; set; } = 0;
    public Int32 UseLv { get; set; } = 0;
    public Int32 Attack { get; set; } = 0;
    public Int32 Defence { get; set; } = 0;
    public Int32 Magic { get; set; } = 0;
    public Int32 EnhanceMaxCount { get; set; } = 0;
}

public class ItemCodeAndCount
{
    public Int32 ItemCode { get; set; }

    public Int32 ItemCount { get; set; }
}