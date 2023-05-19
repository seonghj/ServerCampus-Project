using System;
using System.ComponentModel.DataAnnotations;
using DungeonFarming.DBTableFormat;

namespace DungeonFarming.ResponseFormat;

public class EnhanceItemResponse
{
    [Required]
    public ErrorCode Result { get; set; } = ErrorCode.None;

    public PlayerItemForClient ItemInfo { get; set; } = null;

    public bool EnhanceResult { get; set; } = false;
}
