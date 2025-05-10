using ASC.Model.BaseTypes;
using ASC.Web.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ASC.Web.Data
{
    public class IdentitySeed : IIdentitySeed
    {
        public async Task Seed(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager,
                              IOptions<ApplicationSettings> options)
        {
            if (options == null || options.Value == null)
            {
                throw new ArgumentNullException(nameof(options), "Cấu hình ứng dụng không được null.");
            }

            var roles = options.Value.Roles?.Split(new char[] { ',' }) ?? Array.Empty<string>();

            foreach (var role in roles)
            {
                try
                {
                    if (!string.IsNullOrEmpty(role) && !roleManager.RoleExistsAsync(role).Result)
                    {
                        IdentityRole storageRole = new IdentityRole
                        {
                            Name = role
                        };
                        IdentityResult roleResult = await roleManager.CreateAsync(storageRole);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            if (!string.IsNullOrEmpty(options.Value.AdminEmail) && !string.IsNullOrEmpty(options.Value.AdminPassword))
            {
                var admin = await userManager.FindByEmailAsync(options.Value.AdminEmail);
                if (admin == null)
                {
                    IdentityUser user = new IdentityUser
                    {
                        UserName = options.Value.AdminName ?? options.Value.AdminEmail,
                        Email = options.Value.AdminEmail,
                        EmailConfirmed = true
                    };

                    IdentityResult result = await userManager.CreateAsync(user, options.Value.AdminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", options.Value.AdminEmail));
                        await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("IsActive", "True"));
                        await userManager.AddToRoleAsync(user, Roles.Admin.ToString());
                    }
                }
            }

            if (!string.IsNullOrEmpty(options.Value.EngineerEmail) && !string.IsNullOrEmpty(options.Value.EngineerPassword))
            {
                var engineer = await userManager.FindByEmailAsync(options.Value.EngineerEmail);
                if (engineer == null)
                {
                    IdentityUser user = new IdentityUser
                    {
                        UserName = options.Value.EngineerName ?? options.Value.EngineerEmail,
                        Email = options.Value.EngineerEmail,
                        EmailConfirmed = true,
                        LockoutEnabled = false
                    };

                    IdentityResult result = await userManager.CreateAsync(user, options.Value.EngineerPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", options.Value.EngineerEmail));
                        await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("IsActive", "True"));
                        await userManager.AddToRoleAsync(user, Roles.Engineer.ToString());
                    }
                }
            }
        }
    }
}