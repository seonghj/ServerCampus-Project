using DungeonFarming.DBTableFormat;
using DungeonFarming.MasterData;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.ResponseFormat;

public class SendChatResponse
{
    [Required]
    public ErrorCode Result { get; set; } = ErrorCode.None;
}