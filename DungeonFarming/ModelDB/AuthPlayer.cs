namespace DungeonFarming.ModelDB;

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