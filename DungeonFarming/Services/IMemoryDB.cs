using DungeonFarming.ModelDB;
using System;
using System.Threading.Tasks;

namespace DungeonFarming.Services;

public interface IMemoryDb
{
    public void Init(string address);

}