using System;
using System.ComponentModel.DataAnnotations;
using DungeonFarming.DBTableFormat;

namespace DungeonFarming.ResponseFormat;

public class AttendenceStatusResponse
{
    [Required]
    public ErrorCode Result { get; set; } = ErrorCode.None;

    public Int32 AttendenceDays { get; set; } = 0;
}
