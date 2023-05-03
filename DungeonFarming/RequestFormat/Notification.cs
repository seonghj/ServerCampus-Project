using DungeonFarming.DBTableFormat;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.RequestFormat;
public class NotificationRequest
{
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9\s]{1,20}$", ErrorMessage = "ID is not valid")]
    public string AccountID { get; set; }
    [Required]
    [MinLength(1, ErrorMessage = "AuthToken can not Empty")]
    public string AuthToken { get; set; } = "";

    [Required]
    [MinLength(1, ErrorMessage = "ClientVersion CANNOT BE EMPTY")]
    public string ClientVersion { get; set; }
    [Required]
    [MinLength(1, ErrorMessage = "MasterDataVersion CANNOT BE EMPTY")]
    public string MasterDataVersion { get; set; }
}

public class AddNotificationRequest
{
    [Required]
    public string Title { get; set; } = "";
    [Required]
    public string Content { get; set; } = "";
}