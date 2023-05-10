﻿using DungeonFarming.DBTableFormat;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.RequestFormat;
public class AttendenceRequest : AuthRequest
{
    [Required]
    public Int32 UID { get; set; }
}