using DungeonFarming.MasterData;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.ResponseFormat;

public class StageStopResponse
{
    [Required]
    public ErrorCode Result { get; set; } = ErrorCode.None;

}