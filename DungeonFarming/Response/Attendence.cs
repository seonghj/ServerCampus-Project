using System;
using System.ComponentModel.DataAnnotations;
using DungeonFarming.DBTableFormat;

namespace DungeonFarming.ResponseFormat;

public class AttendenceResponse
{
    [Required]
    public ErrorCode Result { get; set; } = ErrorCode.None;
}
