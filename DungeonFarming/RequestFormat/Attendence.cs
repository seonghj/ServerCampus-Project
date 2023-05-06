using DungeonFarming.DBTableFormat;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.RequestFormat;
public class AttendenceRequest : AuthRequest
{
    [Required]
    public string UID { get; set; }
}