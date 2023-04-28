using System;
using System.Threading.Tasks;


namespace DungeonFarming.Services;

public interface IAccountDb
{
    public Task<ErrorCode> CreateAccountAsync(String id, String pw);
    
    public Task<Tuple<ErrorCode, Int64>> VerifyAccount(String id, String pw);
}