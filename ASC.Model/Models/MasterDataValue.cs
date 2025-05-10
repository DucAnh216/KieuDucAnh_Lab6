using ASC.Model.BaseTypes;
using System;

namespace ASC.Model.Models
{
    public class MasterDataValue : BaseEntity, IAuditTracker
    {
        public MasterDataValue()
        {
            PartitionKey = Guid.NewGuid().ToString();
            RowKey = Guid.NewGuid().ToString();
            Name = string.Empty;
            Value = string.Empty;
            CreatedBy = string.Empty;
            UpdatedBy = string.Empty;
        }

        public MasterDataValue(string partitionKey, string name, string value)
        {
            PartitionKey = partitionKey ?? throw new ArgumentNullException(nameof(partitionKey));
            RowKey = Guid.NewGuid().ToString();
            Name = name ?? string.Empty;
            Value = value ?? string.Empty;
            IsActive = true;
            CreatedBy = string.Empty;
            UpdatedBy = string.Empty;
        }

        public bool IsActive { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}