using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.ModelDB;

public class PlayerInfo
{ 
    public string AccountID { get; set; }

    public Int32 Level { get; set; }

    public Int32 Exp { get; set; }


    public Int32 Hp { get; set; }

    public Int32 Mp { get; set; }

    public Int32 Gold { get; set; }

    public Int32 LastStage { get; set; }
}
