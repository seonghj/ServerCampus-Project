﻿using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DungeonFarming.DBTableFormat;

public class Mail
{
    public string UID { get; set; }
    public string MailCode { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime ExpirationDate { get; set; }
    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class MailItem
{
    public string UID { get; set; }
    public string MailCode { get; set; }
    public string Items { get; set; }
}
