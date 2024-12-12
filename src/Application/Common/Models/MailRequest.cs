
using Domain.Enums;

namespace Application.Common.Models;
/// <summary>
/// MailRequest
/// </summary>
public class MailRequest
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public string From { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Adresse { get; set; }
    public string Nom { get; set; }
    public TypeWFEnum TypeWF { get; set; }
    public string Distance { get; set; }
    public string IsConsumptionMonitoring { get; set; }

}
