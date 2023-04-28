using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.ModelReqRes;

public class PkCreateCharacterReq
{
    [Required] public string ID { get; set; }
}

public class PkCreateCharacterResp
{
    public ErrorCode Result { get; set; } = ErrorCode.None;
}

public class CreateCharacterInfo
{
    public Int32 Level { get; set; }

    public Int32 EXP { get; set; }

    public Int32 HP { get; set; }

    public Int32 MP { get; set; }

    public Int32 Gold { get; set; }

    public Int32 LastStage { get; set;}
}