﻿using System;
using System.ComponentModel.DataAnnotations;
using DungeonFarming.DBTableFormat;

namespace DungeonFarming.ResponseFormat;

public class MailItemResponse
{
    [Required]
    public ErrorCode Result { get; set; } = ErrorCode.None;

    public List<PlayerItem> Items { get; set; } = null;
}
