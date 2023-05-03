namespace DungeonFarming.MasterData;
public class InAppProductGetter
{
    public Int32 Code { get; set; }
    public String Item { get; set; }
}

public class InAppProduct
{
    public Int32 Code { get; set; }
    public List<ProductItems> Item { get; set; }
}

public class ProductItems
{
    public Int32 ItemCode { get; set; }

    public Int32 ItemCount { get; set; }
}