using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Client
/// </summary>
public class Client : IEntity
{
    public int Id { get; set; }
    public string? Nom { get; set; }

    public string? Prenom { get; set; }

    public string? Firm { get; set; }

    public string? City { get; set; }

    public int? NumberOfStationsToInstall { get; set; }
    public string? Adresse { get; set; }
    public string? Email { get; set; }
    public string? Tel { get; set; }
    public string? Sujet { get; set; }
    public string? Message { get; set; }
    public DateTime Created { get; set; }
}


