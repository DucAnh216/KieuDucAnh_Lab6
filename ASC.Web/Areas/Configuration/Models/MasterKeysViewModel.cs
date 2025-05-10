using System.Collections.Generic;

namespace ASC.Web.Areas.Configuration.Models
{
    public class MasterKeysViewModel
    {
        public List<MasterDataKeyViewModel> MasterKeys { get; set; } = new();
        public bool IsEdit { get; set; } = false;
        public MasterDataKeyViewModel MasterKeyInContext { get; set; } = new();
    }
}
