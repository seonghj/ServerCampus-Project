﻿using DungeonFarming.DBTableFormat;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.RequestFormat;
public class NotificationRequest
{
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9\s]{1,20}$", ErrorMessage = "ID is not valid")]
    public String AccountID { get; set; }
    [Required]
    [MinLength(1, ErrorMessage = "AuthToken can not Empty")]
    public string AuthToken { get; set; } = "";
}