using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.RequestFormat;

public class StageStart : AuthRequest
{
    [Required]
    public string UID { get; set; }

    [Required]
    public Int32 StageCode { get; set; }
}
