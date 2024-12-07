
namespace Application.Common.Interfaces;

/// <summary>
/// IApplicationDbContext interface
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Client> Clients { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
