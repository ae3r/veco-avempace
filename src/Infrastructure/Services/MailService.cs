using Application.Common.Interfaces;
using Application.Common.Models;
using Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Services;

/// <summary>
/// MailService class
/// </summary>
public class MailService : IMailService
{
    public MailSettings _mailSettings { get; }
    public ILogger<MailService> _logger { get; }

    /// <summary>
    /// Constructor : Initializes a new instance of MailService 
    /// </summary>
    /// <param name="mailSettings"></param>
    /// <param name="logger"></param>
    public MailService(IOptions<MailSettings> mailSettings, ILogger<MailService> logger)
    {
        _mailSettings = mailSettings.Value;
        _logger = logger;
    }

    public async Task SendAsync(MailRequest request)
    {
        try
        {
            var smtpClient = new SmtpClient(_mailSettings.Host)
            {
                Port = 587,
                Credentials = new NetworkCredential(_mailSettings.UserName, _mailSettings.Password),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(request.From),
                Subject = request.Subject,
                Body = request.Body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(_mailSettings.To);

            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex.Message, ex);
        }
    }
}
