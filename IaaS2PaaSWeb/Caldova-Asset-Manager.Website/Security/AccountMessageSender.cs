using System.Net;
using System.Net.Mail;

namespace CaldovaAssetManager.Security;

public class AccountMessageSender(IConfiguration configuration)
{
    public async Task SendEmailAsync(string address, string subject, string body)
    {
        var host = configuration["Email:SmtpHost"];
        var from = configuration["Email:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Email:SmtpHost and Email:From must be configured for account email delivery.");
        using var client = new SmtpClient(host, configuration.GetValue("Email:SmtpPort", 587))
        {
            EnableSsl = true
        };
        var username = configuration["Email:UserName"];
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, configuration["Email:Password"]);
        using var message = new MailMessage(from, address, subject, body) { IsBodyHtml = true };
        await client.SendMailAsync(message);
    }
}
