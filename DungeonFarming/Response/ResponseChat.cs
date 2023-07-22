using DungeonFarming.DBTableFormat;
using DungeonFarming.MasterData;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.ResponseFormat;

public class ReceiveChatResponse
{
    [Required]
    public ErrorCode Result { get; set; } = ErrorCode.None;

    public List<ChatInfo> Chats { get; set; }
}