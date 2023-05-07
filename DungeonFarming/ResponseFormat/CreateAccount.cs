using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.ResponseFormat;

public class CreateAccountResponse
{
    [Required]
    public ErrorCode Result { get; set; } = ErrorCode.None;
}