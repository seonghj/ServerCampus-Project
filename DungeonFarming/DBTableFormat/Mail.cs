namespace DungeonFarming.DBTableFormat;

public class Mail
{
    public string UID { get; set; }
    public string MailCode { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string ExpirationPeriod { get; set; }
    public bool Read { get; set; }
}

public class MailData
{
    public string UID { get; set; }
    public string MailCode { get; set; }
    public List<PlayerItem> Items { get; set; }
}
