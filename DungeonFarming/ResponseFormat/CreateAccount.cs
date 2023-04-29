using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.ResponseFormat;

public class CreateAccountResponse
{
    public ErrorCode Result { get; set; } = ErrorCode.None;
}