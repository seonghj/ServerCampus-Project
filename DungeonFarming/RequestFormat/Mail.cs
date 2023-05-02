using DungeonFarming.DBTableFormat;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.RequestFormat;
public class MailRequest
{
    [Required]
    public string UID { get; set; }

    [Required]
    public Int32 Page { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "ClientVersion CANNOT BE EMPTY")]
    public string ClientVersion { get; set; }
    [Required]
    [MinLength(1, ErrorMessage = "MasterDataVersion CANNOT BE EMPTY")]
    public string MasterDataVersion { get; set; }
}