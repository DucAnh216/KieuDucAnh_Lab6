using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // Thư viện chứa IEmailSender
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ASC.Web.Areas.Identity.Pages.Account;

public class InitiateResetPasswordModel(
    UserManager<IdentityUser> userManager,
    IEmailSender emailSender, // Không dùng generic
    ILogger<InitiateResetPasswordModel> logger) : PageModel
{
    private readonly UserManager<IdentityUser> _userManager = userManager;
    private readonly IEmailSender _emailSender = emailSender;
    private readonly ILogger<InitiateResetPasswordModel> _logger = logger;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
        {
            _logger.LogWarning("User email not found in request.");
            return BadRequest("Invalid request.");
        }

        var user = await _userManager.FindByEmailAsync(userEmail);
        if (user == null)
        {
            _logger.LogWarning("No user found with email {Email}.", userEmail);
            return NotFound("User not found.");
        }

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        var callbackUrl = Url.Page(
            "/Account/ResetPassword",
            null,
            new { userId = user.Id, code },
            Request.Scheme);

        await _emailSender.SendEmailAsync( // Không cần IdentityUser
            userEmail,
            "Reset Password",
            $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");

        _logger.LogInformation("Password reset email sent to {Email}.", userEmail);
        return RedirectToPage("./ResetPasswordEmailConfirmation");
    }
}
