using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.DBTableFormat;

public class PlayerInfo
{ 
    public string AccountID { get; set; }

    public string UID { get; set; }

    public Int32 Level { get; set; }

    public Int32 Exp { get; set; }


    public Int32 Hp { get; set; }

    public Int32 Mp { get; set; }

    public Int32 Gold { get; set; }

    public DateTime LastLoginTime { get; set; }

    public Int32 ConsecutiveLoginDays { get; set; }    

    public Int32 LastClearStage { get; set; }
}