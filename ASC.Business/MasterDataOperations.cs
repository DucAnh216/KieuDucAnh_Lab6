using ASC.Model.Models;
using ASC.DataAccess;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ASC.Business.Interfaces;

namespace ASC.Business
{
    public class MasterDataOperations : IMasterDataOperations
    {
        private readonly IRepository<MasterDataKey> _keyRepository;
        private readonly IRepository<MasterDataValue> _valueRepository;
        private readonly ILogger<MasterDataOperations> _logger;

        public MasterDataOperations(
            IRepository<MasterDataKey> keyRepository,
            IRepository<MasterDataValue> valueRepository,
            ILogger<MasterDataOperations> logger)
        {
            _keyRepository = keyRepository ?? throw new ArgumentNullException(nameof(keyRepository));
            _valueRepository = valueRepository ?? throw new ArgumentNullException(nameof(valueRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<MasterDataKey>?> GetAllMasterKeysAsync()
        {
            var keys = await _keyRepository.FindAllAsync();
            return keys?.ToList() ?? new List<MasterDataKey>();
        }

        public async Task<MasterDataKey?> GetMasterKeyByIdAsync(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return null;

            return await _keyRepository.FindAsync(partitionKey, rowKey);
        }

        public async Task InsertMasterKeyAsync(MasterDataKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrEmpty(key.PartitionKey) || string.IsNullOrEmpty(key.RowKey))
                throw new ArgumentNullException("PartitionKey and RowKey of MasterDataKey cannot be null or empty.");

            await _keyRepository.AddAsync(key);
            _logger.LogInformation("Inserted Master Key: {Name}, IsActive={IsActive}", key.Name, key.IsActive);
        }

        public async Task UpdateMasterKeyAsync(string? partitionKey, string? rowKey, MasterDataKey updatedKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                throw new ArgumentNullException("PartitionKey and RowKey cannot be null or empty.");

            var existingKey = await _keyRepository.FindAsync(partitionKey, rowKey);
            if (existingKey == null)
            {
                _logger.LogWarning("Master Key not found: PartitionKey={PartitionKey}, RowKey={RowKey}", partitionKey, rowKey);
                return;
            }

            existingKey.Name = updatedKey.Name;
            existingKey.IsActive = updatedKey.IsActive;
            existingKey.UpdatedBy = updatedKey.UpdatedBy ?? "Anonymous";
            existingKey.UpdatedDate = DateTime.UtcNow;

            await _keyRepository.Update(existingKey);
            _logger.LogInformation("Updated Master Key: {Name}, IsActive={IsActive}", updatedKey.Name, updatedKey.IsActive);
        }

        public async Task<List<MasterDataValue>?> GetMasterValuesByKeyAsync(string? key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var values = await _valueRepository.FindAllByPartitionKeyAsync(key);
            return values?.ToList() ?? new List<MasterDataValue>();
        }

        public async Task InsertMasterValueAsync(MasterDataValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (string.IsNullOrEmpty(value.PartitionKey) || string.IsNullOrEmpty(value.RowKey))
                throw new ArgumentNullException("PartitionKey and RowKey of MasterDataValue cannot be null or empty.");

            await _valueRepository.AddAsync(value);
            _logger.LogInformation("Inserted Master Value: {Name}, Value={Value}, IsActive={IsActive}", value.Name, value.Value, value.IsActive);
        }

        public async Task UpdateMasterValueAsync(string? partitionKey, string? rowKey, MasterDataValue updatedValue)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                throw new ArgumentNullException("PartitionKey and RowKey cannot be null or empty.");

            var existingValue = await _valueRepository.FindAsync(partitionKey, rowKey);
            if (existingValue == null)
            {
                _logger.LogWarning("Master Value not found: PartitionKey={PartitionKey}, RowKey={RowKey}", partitionKey, rowKey);
                return;
            }

            existingValue.Name = updatedValue.Name;
            existingValue.Value = updatedValue.Value;
            existingValue.IsActive = updatedValue.IsActive;
            existingValue.UpdatedBy = updatedValue.UpdatedBy ?? "Anonymous";
            existingValue.UpdatedDate = DateTime.UtcNow;

            await _valueRepository.Update(existingValue);
            _logger.LogInformation("Updated Master Value: {Name}, Value={Value}, IsActive={IsActive}", updatedValue.Name, updatedValue.Value, updatedValue.IsActive);
        }
    }
}
