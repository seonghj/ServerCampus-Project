using DungeonFarming.DBTableFormat;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.ResponseFormat;

public class LoginResponse
{
    [Required] 
    public ErrorCode Result { get; set; } = ErrorCode.None;
    [Required] 
    public AuthPlayer PlayerAuth { get; set; }

    [Required]
    public PlayerInfo PlayerInfomation { get; set; } = null;

    public List<PlayerItem> PlayerItems { get; set; } = null;
}