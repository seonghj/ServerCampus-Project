using DungeonFarming.DBTableFormat;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.RequestFormat;
public class FarmingItemRequest : AuthRequest
{
    [Required]
    public Int32 UID { get; set; }

    [Required]
    public Int32 ItemCode { get; set; }

    [Required]
    public Int32 ItemCount { get; set; }

    [Required]
    public Int32 StageCode { get; set; }
}