using DungeonFarming.DBTableFormat;
using DungeonFarming.MasterData;
using System;
using System.ComponentModel.DataAnnotations;

namespace DungeonFarming.ResponseFormat;

public class StageClearResponse
{
    [Required]
    public ErrorCode Result { get; set; } = ErrorCode.None;

    public List<PlayerItemForClient> EarnItemList { get; set; } = null;

    public Int32 EarnEXP { get; set; } = 0;

}