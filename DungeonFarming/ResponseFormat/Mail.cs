using System;
using System.ComponentModel.DataAnnotations;
using DungeonFarming.DBTableFormat;

namespace DungeonFarming.ResponseFormat;


public class MailResponse
{
    [Required]
    public ErrorCode Result { get; set; } = ErrorCode.None;
    public List<Mail> MailList {get; set;}
}
