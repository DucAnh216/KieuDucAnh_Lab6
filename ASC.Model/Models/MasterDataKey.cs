using ASC.Model.BaseTypes;
using System;

namespace ASC.Model.Models
{
    public class MasterDataKey : BaseEntity
    {
        public MasterDataKey()
        {
            PartitionKey = Guid.NewGuid().ToString();
            RowKey = Guid.NewGuid().ToString();
            Name = string.Empty;
            IsActive = false; // Mặc định false
            CreatedBy = string.Empty;
            UpdatedBy = string.Empty;
        }

        public string Name { get; set; }
        public bool IsActive { get; set; }
    }
}