using DungeonFarming.DBTableFormat;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.RequestFormat;
public class MailItemRequest : AuthRequest
{
    [Required]
    public Int32 UID { get; set; }

    [Required]
    public Int32 MailCode { get; set; }
}