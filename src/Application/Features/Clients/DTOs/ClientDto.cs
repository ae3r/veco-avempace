
namespace Application.Features.Clients.DTOs;

/// <summary>
/// ClientDto class
/// </summary>
public class ClientDto : IMapFrom<Client>
{
    /// <summary>
    /// Mapping
    /// </summary>
    /// <param name="profile"></param>
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Client, ClientDto>();
        profile.CreateMap<ClientDto, Client>();
        profile.CreateMap<Client, AddEditClientCommand>();
        profile.CreateMap<AddEditClientCommand, Client>();
    }
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
