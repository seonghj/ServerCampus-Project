using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.ModelDB;

public class CharInfo
{ 
    public string ID { get; set; }

    public Int32 Level { get; set; }

    public Int32 Exp { get; set; }


    public Int32 HP { get; set; }

    public Int32 MP { get; set; }

    public Int32 Gold { get; set; }

    public Int32 LastStage { get; set; }
}
