namespace DungeonFarming.DBTableFormat;

public class AuthPlayer
{
    public string AuthToken { get; set; } = "";
    public string State { get; set; } = "";
}

public enum PlayerState
{
    Default = 0,
    Login = 1,
}
public class NoticeContent
{
    public string title { get; set; } = "";
    public string Content { get; set; } = "";
}