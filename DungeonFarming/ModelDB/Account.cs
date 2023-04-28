using System;

namespace DungeonFarming.ModelDB;

public class Account
{
    public String ID { get; set; }
    public string Password { get; set; }
}

public class AuthUser
{
    public string ID { get; set; } = "";
    public string AuthToken { get; set; } = "";
    public string State { get; set; } = "";   
}

public enum UserState
{
    Default = 0,
    Login = 1,
    Matching = 2,
    Playing = 3
}

public class RediskeyExpireTime
{
    public const ushort NxKeyExpireSecond = 3;
    public const ushort RegistKeyExpireSecond = 6000;
    public const ushort LoginKeyExpireMin = 60;
    public const ushort TicketKeyExpireSecond = 6000;
}