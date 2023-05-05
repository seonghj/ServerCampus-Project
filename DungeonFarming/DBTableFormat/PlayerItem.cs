namespace DungeonFarming.DBTableFormat;

public class PlayerItem
{
    public string UID { get; set; }
    public Int32 ItemCode { get; set; }
    public string ItemUniqueID { get; set; }

    public string ItemName { get; set; }

    public Int32 Attack { get; set; }
    public Int32 Defence { get; set; }
    public Int32 Magic { get; set; }
    public Int32 EnhanceCount { get; set; }
    public Int32 Count { get; set; }
}
