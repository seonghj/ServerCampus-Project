using DungeonFarming.DBTableFormat;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.RequestFormat;
public class MailItemRequest : AuthRequest
{
    [Required]
    public string UID { get; set; }

    [Required]
    public string MailCode { get; set; }
}