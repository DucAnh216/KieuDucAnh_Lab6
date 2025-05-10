using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MimeKit;
using ASC.Web.Configuration;

namespace ASC.Web.Services
{
    public class AuthMessageSender : Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, ASC.Web.Services.IEmailSender, ISmsSender
    {
        private readonly IOptions<ApplicationSettings> _settings;

        public AuthMessageSender(IOptions<ApplicationSettings> settings)
        {
            _settings = settings;
        }

        // Triển khai Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (string.IsNullOrEmpty(_settings.Value.SMTPServer) ||
                string.IsNullOrEmpty(_settings.Value.SMTPAccount) ||
                string.IsNullOrEmpty(_settings.Value.SMTPPassword))
            {
                throw new InvalidOperationException("SMTP configuration is missing.");
            }

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Admin", _settings.Value.SMTPAccount));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("html") { Text = htmlMessage };

            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync(_settings.Value.SMTPServer, _settings.Value.SMTPPort, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_settings.Value.SMTPAccount, _settings.Value.SMTPPassword);
                    await client.SendAsync(emailMessage);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
                }
                finally
                {
                    await client.DisconnectAsync(true);
                }
            }
        }

        // Triển khai ASC.Web.Services.IEmailSender
        async Task IEmailSender.SendEmailAsync(string email, string subject, string message)
        {
            await SendEmailAsync(email, subject, message);
        }

        public Task SendSmsAsync(string number, string message)
        {
            return Task.CompletedTask;
        }
    }
}