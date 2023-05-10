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