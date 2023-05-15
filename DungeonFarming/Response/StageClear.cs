using DungeonFarming.MasterData;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.ResponseFormat;

public class StageClearResponse
{
    [Required]
    public ErrorCode Result { get; set; } = ErrorCode.None;

    public List<Int32> ItemCodeList { get; set; } = null;

    public List<NPCInfo> NPCList { get; set; } = null;

}