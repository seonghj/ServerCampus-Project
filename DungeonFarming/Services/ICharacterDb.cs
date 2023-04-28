using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DungeonFarming.ModelReqRes;

namespace DungeonFarming.Services;

public interface ICharacterDb
{

    public Task<ErrorCode> InsertCharacter(string Id);

    public Task<ErrorCode> InsertCharacterItem(string Id);

    public Task<Tuple<ErrorCode, string>> GetCharacterInfo(string id); 
}