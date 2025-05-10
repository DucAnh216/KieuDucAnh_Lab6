using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace ASC.Web.Areas.Accounts.Models
{
    public class ServiceEngineerViewModel
    {
        public List<IdentityUser> ServiceEngineers { get; set; } = new List<IdentityUser>();
        public ServiceEngineerRegistrationViewModel Registration { get; set; } = new ServiceEngineerRegistrationViewModel();
    }
}