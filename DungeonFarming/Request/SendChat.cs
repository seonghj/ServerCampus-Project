using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.RequestFormat;

public class SendChatRequest : AuthRequest
{
    [Required]
    public Int32 UID { get; set; }

    [Required]
    public string Message { get; set; }
}
