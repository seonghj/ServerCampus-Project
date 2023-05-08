using DungeonFarming.DBTableFormat;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.RequestFormat;
public class EnhanceItemRequest : AuthRequest
{
    [Required]
    public string UID { get; set; }

    [Required]
    public string ItemUniqueID { get; set; }
}