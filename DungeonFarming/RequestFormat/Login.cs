using DungeonFarming.DBTableFormat;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.RequestFormat;
public class LoginRequest
{
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9\s]{1,20}$", ErrorMessage = "ID is not valid")]
    public string ID { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "PASSWORD CANNOT BE EMPTY")]
    [StringLength(20, ErrorMessage = "PASSWORD IS TOO LONG")]
    public string Password { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "ClientVersion CANNOT BE EMPTY")]
    public string ClientVersion { get; set; }
    public string MasterDataVersion { get; set; }
}