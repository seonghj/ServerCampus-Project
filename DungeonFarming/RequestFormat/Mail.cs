using DungeonFarming.DBTableFormat;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.RequestFormat;
public class MailRequest: AuthRequest
{
    [Required]
    public string UID { get; set; }

    [Required]
    public Int32 Page { get; set; }
}