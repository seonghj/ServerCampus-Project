using System;

namespace DungeonFarming.DBTableFormat;

public class Account
{
    public String ID { get; set; }
    public string Salt { get; set; }
    public string HashedPW { get; set; }
}