using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ASC.Web.Areas.Accounts.Models;
using ASC.Web.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace ASC.Web.Areas.Accounts.Controllers;

[Authorize]
[Area("Accounts")]
public class AccountController(UserManager<IdentityUser> userManager, IEmailSender emailSender, SignInManager<IdentityUser> signInManager, ILogger<AccountController> logger) : Controller
{
    private readonly UserManager<IdentityUser> _userManager = userManager;
    private readonly IEmailSender _emailSender = emailSender;
    private readonly SignInManager<IdentityUser> _signInManager = signInManager;
    private readonly ILogger<AccountController> _logger = logger;

    // GET: /Accounts/Index
    public IActionResult Index()
    {
        _logger.LogInformation("Index action called");
        return View();
    }

    // GET: /Accounts/ServiceEngineers
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> ServiceEngineers()
    {
        _logger.LogInformation("ServiceEngineers GET action called");
        try
        {
            var engineers = await _userManager.GetUsersInRoleAsync(Roles.Engineer.ToString());
            _logger.LogInformation("Found {Count} service engineers", engineers.Count);
            if (engineers.Count > 0)
            {
                foreach (var engineer in engineers)
                {
                    _logger.LogInformation("Engineer details: Email={Email}, UserName={UserName}", engineer.Email, engineer.UserName);
                }
            }
            else
            {
                _logger.LogWarning("No service engineers found in the database");
            }

            HttpContext.Session.SetSession("ServiceEngineers", engineers);

            return View(new ServiceEngineerViewModel
            {
                ServiceEngineers = engineers.ToList(), // Chuyển IList thành List
                Registration = new ServiceEngineerRegistrationViewModel { IsEdit = false }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in ServiceEngineers GET action");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    // POST: /Accounts/ServiceEngineers
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceEngineers(ServiceEngineerViewModel model)
    {
        _logger.LogInformation("ServiceEngineers POST action called");
        try
        {
            model.ServiceEngineers = HttpContext.Session.GetSession<List<IdentityUser>>("ServiceEngineers");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state invalid in ServiceEngineers POST action");
                return View(model);
            }

            var reg = model.Registration;

            if (reg.IsEdit)
            {
                var user = await _userManager.FindByEmailAsync(reg.Email);
                if (user == null)
                {
                    _logger.LogWarning("User not found: Email={Email}", reg.Email);
                    ModelState.AddModelError("", "User not found.");
                    return View(model);
                }

                user.UserName = reg.UserName;
                user.NormalizedUserName = _userManager.NormalizeName(reg.UserName);

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogError("Failed to update user: Email={Email}, Errors={Errors}", reg.Email, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                    AddErrors(updateResult);
                    return View(model);
                }

                if (!string.IsNullOrEmpty(reg.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await _userManager.ResetPasswordAsync(user, token, reg.Password);
                    if (!passwordResult.Succeeded)
                    {
                        _logger.LogError("Failed to reset password for user: Email={Email}, Errors={Errors}", reg.Email, string.Join(", ", passwordResult.Errors.Select(e => e.Description)));
                        AddErrors(passwordResult);
                        return View(model);
                    }
                }

                await UpdateIsActiveClaim(user, reg.IsActive);

                if (!string.IsNullOrEmpty(user.Email))
                {
                    await SendAccountEmail(user.Email, reg.IsActive, "updated", reg.Password);
                }

                return RedirectToAction(nameof(ServiceEngineers));
            }
            else
            {
                if (string.IsNullOrEmpty(reg.Password))
                {
                    _logger.LogWarning("Password is required when creating a new engineer");
                    return Json(new { success = false, message = "Password is required." });
                }

                var user = new IdentityUser
                {
                    UserName = reg.UserName,
                    Email = reg.Email,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(user, reg.Password);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("Failed to create user: Email={Email}, Errors={Errors}", reg.Email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return Json(new { success = false, errors = createResult.Errors.Select(e => e.Description) });
                }

                await _userManager.AddToRoleAsync(user, Roles.Engineer.ToString());
                await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Email, reg.Email));
                await _userManager.AddClaimAsync(user, new Claim("IsActive", reg.IsActive.ToString()));

                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        await SendAccountEmail(user.Email, reg.IsActive, "created", reg.Password);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send email after creating account: Email={Email}", user.Email);
                        return Json(new { success = false, message = "Account created, but failed to send email." });
                    }
                }

                return Json(new
                {
                    success = true,
                    engineer = new
                    {
                        email = user.Email,
                        userName = user.UserName,
                        isActive = reg.IsActive
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in ServiceEngineers POST action");
            return Json(new { success = false, message = "Internal server error" });
        }
    }

    // GET: /Accounts/Customers
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Customers()
    {
        _logger.LogInformation("Customers GET action called");
        try
        {
            var allUsers = await _userManager.Users.ToListAsync();
            _logger.LogInformation("Found {Count} users in AspNetUsers", allUsers.Count);
            if (allUsers.Count == 0)
            {
                _logger.LogWarning("No users found in the database");
            }
            else
            {
                foreach (var user in allUsers)
                {
                    _logger.LogInformation("User details: Id={Id}, Email={Email}, UserName={UserName}", user.Id, user.Email, user.UserName);
                    var claims = await _userManager.GetClaimsAsync(user);
                    _logger.LogInformation("Claims for user: Email={Email}, Claims={Claims}", user.Email, string.Join(", ", claims.Select(c => $"{c.Type}:{c.Value}")));
                }
            }

            return View(new CustomerViewModel
            {
                Customers = allUsers,
                Registration = new CustomerRegistrationViewModel { IsEdit = false }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in Customers GET action");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    // POST: /Accounts/Customers
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Customers(CustomerViewModel model)
    {
        _logger.LogInformation("Customers POST action called");
        try
        {
            if (model.Registration.IsEdit)
            {
                ModelState.Remove("Registration.UserName");
                ModelState.Remove("Registration.Password");
                ModelState.Remove("Registration.ConfirmPassword");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state invalid in Customers POST action");
                var allUsers = await _userManager.Users.ToListAsync();
                model.Customers = allUsers;
                return View(model);
            }

            var reg = model.Registration;
            if (reg.IsEdit)
            {
                var user = await _userManager.FindByEmailAsync(reg.Email);
                if (user == null)
                {
                    _logger.LogWarning("User not found: Email={Email}", reg.Email);
                    ModelState.AddModelError("", "User not found.");
                    var allUsers = await _userManager.Users.ToListAsync();
                    model.Customers = allUsers;
                    return View(model);
                }

                await UpdateIsActiveClaim(user, reg.IsActive);

                if (!string.IsNullOrEmpty(user.Email))
                {
                    await SendAccountEmail(user.Email, reg.IsActive, "modified");
                }
            }

            return RedirectToAction(nameof(Customers));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in Customers POST action");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    // GET: /Accounts/Profile
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        _logger.LogInformation("Profile GET action called");
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in claims, redirecting to Login");
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: Id={Id}, redirecting to Login", userId);
                return RedirectToAction("Login", "Account");
            }

            var claims = await _userManager.GetClaimsAsync(user);
            var isActiveClaim = claims.FirstOrDefault(c => c.Type == "IsActive");

            bool isActive = false;
            if (isActiveClaim != null)
            {
                if (!bool.TryParse(isActiveClaim.Value, out isActive))
                {
                    _logger.LogWarning("Failed to parse IsActive claim for user: Id={Id}, Value={Value}", userId, isActiveClaim.Value);
                }
            }

            return View(new ProfileModel
            {
                UserName = user.UserName ?? string.Empty,
                IsActive = isActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in Profile GET action");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    // POST: /Accounts/Profile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileModel profile)
    {
        _logger.LogInformation("Profile POST action called");
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state invalid in Profile POST action");
                return Json(new { success = false, message = "Invalid data." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in claims");
                return Json(new { success = false, message = "User not authenticated." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: Id={Id}", userId);
                return Json(new { success = false, message = "User not found." });
            }

            var existingUser = await _userManager.FindByNameAsync(profile.UserName);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                _logger.LogWarning("Username already taken: UserName={UserName}", profile.UserName);
                return Json(new { success = false, message = "This username is already taken." });
            }

            user.UserName = profile.UserName;
            user.NormalizedUserName = _userManager.NormalizeName(profile.UserName);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to update profile: Id={Id}, Errors={Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return Json(new { success = false, message = "Failed to update profile: " + string.Join(", ", result.Errors.Select(e => e.Description)) });
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("Profile updated successfully: Id={Id}", userId);
            return Json(new { success = true, message = "Profile updated successfully.", userName = user.UserName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in Profile POST action");
            return Json(new { success = false, message = "Internal server error" });
        }
    }

    // GET: /Accounts/Login
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        _logger.LogInformation("Login GET action called");
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Accounts/ExternalLogin
    [HttpPost]
    [AllowAnonymous]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        _logger.LogInformation("ExternalLogin POST action called: Provider={Provider}", provider);
        try
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in ExternalLogin POST action");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    // GET: /Accounts/ExternalLoginCallback
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        _logger.LogInformation("ExternalLoginCallback action called");
        try
        {
            if (remoteError != null)
            {
                _logger.LogError("Error from external provider: RemoteError={RemoteError}", remoteError);
                ModelState.AddModelError("", $"Error from external provider: {remoteError}");
                return RedirectToAction("Login", "Account");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogError("External login info is null");
                return RedirectToAction("Login", "Account");
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in with external provider: LoginProvider={LoginProvider}", info.LoginProvider);
                return RedirectToAction("Customers");
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var userName = info.Principal.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(userName))
            {
                userName = email?.Split('@')[0] ?? "UnknownUser";
                _logger.LogWarning("UserName not provided by external provider, using default: UserName={UserName}", userName);
            }

            var user = new IdentityUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                _logger.LogError("Failed to create user: Email={Email}, Errors={Errors}", email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                AddErrors(createResult);
                return RedirectToAction("Login", "Account");
            }

            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded)
            {
                _logger.LogError("Failed to add login for user: Email={Email}, Errors={Errors}", email, string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
                await _userManager.DeleteAsync(user);
                AddErrors(addLoginResult);
                return RedirectToAction("Login", "Account");
            }

            await _userManager.AddClaimAsync(user, new Claim("UserType", "Customer"));
            await _userManager.AddClaimAsync(user, new Claim("IsActive", "True"));

            var claims = await _userManager.GetClaimsAsync(user);
            _logger.LogInformation("Claims added for user: Email={Email}, Claims={Claims}", email, string.Join(", ", claims.Select(c => $"{c.Type}:{c.Value}")));

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User created and signed in successfully: Email={Email}", email);

            return RedirectToAction("Customers");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in ExternalLoginCallback action");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    // POST: /Accounts/ChangeUserName
    [HttpPost]
    public async Task<IActionResult> ChangeUserName(string email, string newUserName)
    {
        _logger.LogInformation("ChangeUserName action called: Email={Email}, NewUserName={NewUserName}", email, newUserName);
        try
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(newUserName))
            {
                _logger.LogError("Invalid email or username provided: Email={Email}, NewUserName={NewUserName}", email, newUserName);
                return BadRequest(new { success = false, message = "Invalid email or username" });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogError("User not found: Email={Email}", email);
                return BadRequest(new { success = false, message = "User not found" });
            }

            var existingUser = await _userManager.FindByNameAsync(newUserName);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                _logger.LogError("Username already taken: NewUserName={NewUserName}", newUserName);
                return BadRequest(new { success = false, message = "This username is already taken" });
            }

            user.UserName = newUserName;
            user.NormalizedUserName = _userManager.NormalizeName(newUserName);
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to update username: Email={Email}, Errors={Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(new { success = false, message = $"Failed to update username: {string.Join(", ", result.Errors.Select(e => e.Description))}" });
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("Username updated successfully: Email={Email}, NewUserName={NewUserName}", email, newUserName);
            return Ok(new { success = true, message = "Username updated successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in ChangeUserName action");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private async Task SendAccountEmail(string email, bool isActive, string action, string? password = null)
    {
        try
        {
            string subject = $"Account {action}";
            string body = isActive
                ? $"Your account has been {action}.\nEmail: {email}\nPassword: {password ?? "N/A"}"
                : "Your account has been deactivated.";

            await _emailSender.SendEmailAsync(email, subject, body);
            _logger.LogInformation("Email sent successfully: Email={Email}, Action={Action}", email, action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email: Email={Email}", email);
            throw;
        }
    }

    private async Task UpdateIsActiveClaim(IdentityUser user, bool isActive)
    {
        try
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var existing = claims.FirstOrDefault(c => c.Type == "IsActive");

            if (existing != null)
            {
                await _userManager.RemoveClaimAsync(user, existing);
            }

            await _userManager.AddClaimAsync(user, new Claim("IsActive", isActive.ToString()));
            _logger.LogInformation("IsActive claim updated: Email={Email}, IsActive={IsActive}", user.Email, isActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update IsActive claim: Email={Email}", user.Email);
            throw;
        }
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }
    }
}

public static class SessionHelper
{
    public static void SetSession<T>(this ISession session, string key, T value)
    {
        var json = JsonConvert.SerializeObject(value);
        session.SetString(key, json);
    }

    public static T GetSession<T>(this ISession session, string key) where T : class, new()
    {
        var json = session.GetString(key);
        return json == null ? new T() : JsonConvert.DeserializeObject<T>(json) ?? new T();
    }
}

public enum Roles
{
    Admin,
    Engineer,
    Customer
}