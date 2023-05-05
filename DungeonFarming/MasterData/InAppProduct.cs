namespace DungeonFarming.MasterData;
public class InAppProductGetter
{
    public Int32 Code { get; set; }
    public String Item { get; set; }
}

public class InAppProduct
{
    public Int32 Code { get; set; }
    public List<ItemCodeAndCount> Item { get; set; }
}