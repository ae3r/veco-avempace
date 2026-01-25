
namespace Application.Common.Interfaces;

/// <summary>
/// IApplicationDbContext interface
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Client> Clients { get; set; }
    DbSet<Produit> Produits { get; set; }
    DbSet<ChargingStation> ChargingStations { get; set; }

    DbSet<ChargingSession> ChargingSession { get; set; }

    DbSet<Network> Networks { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
