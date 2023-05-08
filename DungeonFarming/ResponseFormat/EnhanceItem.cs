using System;
using System.ComponentModel.DataAnnotations;
using DungeonFarming.DBTableFormat;

namespace DungeonFarming.ResponseFormat;

public class EnhanceItemResponse
{
    [Required]
    public ErrorCode Result { get; set; } = ErrorCode.None;

    public PlayerItem ItemInfo { get; set; } = null;
}
