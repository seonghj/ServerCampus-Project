using DungeonFarming.MasterData;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DungeonFarming.DBTableFormat;

public class Mail
{
    public Int32 UID { get; set; }
    public Int32 MailCode { get; set; }
    public string Title { get; set; }
    public DateTime ExpirationDate { get; set; }
    public bool IsReceive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Int32 ItemCode { get; set; }
    public Int32 ItemCount { get; set; }
}

public class MailInfoForClient
{
    public Int32 MailCode { get; set; }
    public string Title { get; set; }
    public DateTime ExpirationDate { get; set; }
    public bool IsReceive { get; set; }
    public List<ItemCodeAndCount> items { get; set; }
}