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
}

public class ItemCodeGetter
{
    public Int32 ItemCode { get; set; }
}
