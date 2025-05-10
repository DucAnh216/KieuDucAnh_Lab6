using ASC.Model.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ASC.Business.Interfaces
{
    public interface IMasterDataOperations
    {
        Task<List<MasterDataKey>?> GetAllMasterKeysAsync();
        Task<MasterDataKey?> GetMasterKeyByIdAsync(string partitionKey, string rowKey);
        Task InsertMasterKeyAsync(MasterDataKey key);
        Task UpdateMasterKeyAsync(string? partitionKey, string? rowKey, MasterDataKey masterDataKey);

        Task<List<MasterDataValue>?> GetMasterValuesByKeyAsync(string? key);
        Task InsertMasterValueAsync(MasterDataValue value);
        Task UpdateMasterValueAsync(string? partitionKey, string? rowKey, MasterDataValue masterDataValue);
    }
}
