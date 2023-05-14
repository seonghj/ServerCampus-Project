namespace DungeonFarming.MasterData;
public class StageItemGetter
{
    public Int32 Code { get; set; }
    public String ItemCode { set; get; }
}

public class StageItem
{
    public Int32 Code { get; set; }
    public List<Int32> ItemCode { set; get; }

    public Dictionary<Int32, Int32> ItemCount { set; get; } = null;
}

public class ItemCodeGetter
{
    public Int32 ItemCode { get; set; }
}
