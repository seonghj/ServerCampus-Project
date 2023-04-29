using DungeonFarming.DBTableFormat;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.ResponseFormat;

public class LoginResponse
{
    [Required] public ErrorCode Result { get; set; } = ErrorCode.None;
    [Required] public AuthPlayer P_Auth { get; set; }

    [Required]
    public PlayerInfo P_Info { get; set; } = null;
}