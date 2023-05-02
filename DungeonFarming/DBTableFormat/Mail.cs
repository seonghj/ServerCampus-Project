namespace DungeonFarming.DBTableFormat;

public class Mail
{
    public string mailCode { get; set; }
    public string title { get; set; }
    public string content { get; set; }
    public string expirationPeriod { get; set; }
}

public class MailData
{
    public string mailCode { get; set; }
    public List<PlayerItem> items { get; set; }
}
