namespace DungeonFarming.DBTableFormat;

public class PlayerItem
{
    public Int32 UID { get; set; }
    public Int32 ItemCode { get; set; }
    public Int32 ItemUniqueID { get; set; }
    public Int32 Attack { get; set; }
    public Int32 Defence { get; set; }
    public Int32 Magic { get; set; }
    public Int32 EnhanceCount { get; set; }
    public Int32 ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PlayerItemForClient
{
    public Int32 ItemCode { get; set; }
    public Int32 Attack { get; set; }
    public Int32 Defence { get; set; }
    public Int32 Magic { get; set; }
    public Int32 EnhanceCount { get; set; }
    public Int32 ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
