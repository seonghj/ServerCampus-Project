namespace DungeonFarming.DBTableFormat;

public class AuthPlayer
{
    public string AuthToken { get; set; } = "";
    public string State { get; set; } = "";
}
// 유저 상태
public enum PlayerState
{
    Default = 0,
    Login = 1,
}

// 공지
public class NoticeContent
{
    public string title { get; set; } = "";
    public string Content { get; set; } = "";
}

public class InStageItem
{
    public Int32 ItemCode { get; set; }
    public Int32 ItemCount { get; set; }
    public Int32 MaxCount { get; set; }
    public DateTime FarmingTime { get; set; }
}

public class InStageNpc
{
    public Int32 NpcCode { get; set; }
    public Int32 NpcCount { get; set; }
    public Int32 MaxCount { get; set; }
    public DateTime KillTime { get; set; }
}