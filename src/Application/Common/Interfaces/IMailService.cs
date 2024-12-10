
namespace Application.Common.Interfaces;

/// <summary>
/// IMailService interface
/// </summary>
public interface IMailService
{
    Task SendAsync(MailRequest request);
}
