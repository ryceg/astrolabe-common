using System.Net;
using System.Net.Mail;

namespace Astrolabe.Email;

public class SmtpEmailSenderService : IEmailSenderService
{
    private readonly SmtpClient _smtpClient;
    private readonly string _from;

    public SmtpEmailSenderService(SmtpClient smtpClient, string from)
    {
        _smtpClient = smtpClient;
        _from = from;
    }

    public async Task SendEmail(string to, string subject, string message, bool notHtml = false)
    {
        await _smtpClient.SendMailAsync(new MailMessage(_from, to, subject, message) {IsBodyHtml = !notHtml});
    }
}
