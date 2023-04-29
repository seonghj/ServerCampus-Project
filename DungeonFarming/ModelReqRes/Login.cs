﻿using DungeonFarming.ModelDB;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.ModelReqRes;
public class LoginRequest
{
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9\s]{1,20}$", ErrorMessage = "ID is not valid")]
    public String ID { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "PASSWORD CANNOT BE EMPTY")]
    [StringLength(20, ErrorMessage = "PASSWORD IS TOO LONG")]
    public String Password { get; set; }
}

public class LoginResponse
{
    [Required] public ErrorCode Result { get; set; } = ErrorCode.None;
    [Required] public AuthPlayer P_Auth { get; set; } = null;

    [Required]
    public PlayerInfo P_Info { get; set; } = null;
}