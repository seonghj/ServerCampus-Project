using DungeonFarming.DBTableFormat;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.RequestFormat;
public class KillNPCRequest : AuthRequest
{
    [Required]
    public Int32 UID { get; set; }

    [Required]
    public Int32 NPCCode { get; set; }

    [Required]
    public Int32 StageCode { get; set; }
}