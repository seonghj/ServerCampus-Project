using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.RequestFormat;

public class ReceiveChatRequest : AuthRequest
{
    [Required]
    public Int32 UID { get; set; }

    [Required]
    public string MessageID { get; set; }
}
