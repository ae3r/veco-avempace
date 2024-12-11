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
                Port = _mailSettings.Port,
                Credentials = new NetworkCredential(_mailSettings.UserName, _mailSettings.Password),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_mailSettings.UserName, "Veco Avempace Contact"), // Use the authenticated user email here
                Subject = request.Subject,
                Body = $@"
        <p><strong>Nom & prénom :</strong> {request.Nom}</p>
        <p><strong>Adresse électronique :</strong> {request.Email}</p>
        <p><strong>Téléphone :</strong> {request.Phone}</p>
        <p><strong>Sujet :</strong> {request.Subject}</p>
        <p><strong>Adresse :</strong> {request.Adresse}</p>" + request.Body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(_mailSettings.To); // Send to the configured recipient
            mailMessage.ReplyToList.Add(new MailAddress(request.From)); // Set the Reply-To address to the user's email

            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            // Handle exception
            Console.WriteLine(ex.Message);
        }
    }
}
