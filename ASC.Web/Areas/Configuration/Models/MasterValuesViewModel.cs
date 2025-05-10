using System.Collections.Generic;

namespace ASC.Web.Areas.Configuration.Models
{
    public class MasterValuesViewModel
    {
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<MasterDataValueViewModel> MasterValues { get; set; } = new();
    }

}
