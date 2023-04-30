using DungeonFarming.DBTableFormat;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.ResponseFormat;

public class NotificationResponse
{
    [Required]
    public ErrorCode Result { get; set; } = ErrorCode.None;

    [Required]
    public List<NoticeContent> NotificationList { get; set; }
}