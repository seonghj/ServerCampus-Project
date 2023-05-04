using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DungeonFarming.DBTableFormat;

public class Mail
{
    public string UID { get; set; }
    public string MailCode { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string ExpirationDate { get; set; }
    public bool IsRead { get; set; }

    public string CreatedAt { get; set; }
}

public class MailData
{
    public string UID { get; set; }
    public string MailCode { get; set; }
    public List<PlayerItem> Items { get; set; }
}
