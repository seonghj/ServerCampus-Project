using DungeonFarming.MasterData;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.ResponseFormat;

public class StageStartResponse
{
    [Required]
    public ErrorCode Result { get; set; } = ErrorCode.None;

    [Required]
    public bool CanStart { get; set; } = false;

    public List<ItemCodeAndCount> ItemList { get; set; } = null;

    public List<NPCInfo> NPCList { get; set; } = null;

}