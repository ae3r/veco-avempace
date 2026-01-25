using System.Reflection;
using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Identity;
using Infrastructure.Persistence.Extensions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class ApplicationDbContext
        : IdentityDbContext<
            ApplicationUser,
            ApplicationRole,
            int,
            ApplicationUserClaim,
            ApplicationUserRole,
            ApplicationUserLogin,
            ApplicationRoleClaim,
            ApplicationUserToken>,
          IApplicationDbContext
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTime _dateTime;
        private readonly IDomainEventService _domainEventService;

        public ApplicationDbContext() : base() { }

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ICurrentUserService currentUserService,
            IDomainEventService domainEventService,
            IDateTime dateTime
        ) : base(options)
        {
            _currentUserService = currentUserService;
            _domainEventService = domainEventService;
            _dateTime = dateTime;
        }

        // Domain sets
        public DbSet<Client> Clients { get; set; }
        public DbSet<Produit> Produits { get; set; }
        public DbSet<ChargingStation> ChargingStations { get; set; }

        public DbSet<ChargingSession> ChargingSession { get; set; } // (name can be singular)
        public DbSet<Network> Networks { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>().ToList())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedBy = _currentUserService.UserId;
                        entry.Entity.Created = _dateTime.Now;
                        break;
                    case EntityState.Modified:
                        entry.Entity.LastModifiedBy = _currentUserService.UserId;
                        entry.Entity.LastModified = _dateTime.Now;
                        break;
                    case EntityState.Deleted:
                        if (entry.Entity is ISoftDelete softDelete)
                        {
                            softDelete.DeletedBy = _currentUserService.UserId;
                            softDelete.Deleted = _dateTime.Now;
                            entry.State = EntityState.Modified;
                        }
                        break;
                }
            }

            var events = ChangeTracker.Entries<IHasDomainEvent>()
                .Select(x => x.Entity.DomainEvents)
                .SelectMany(x => x)
                .Where(domainEvent => !domainEvent.IsPublished)
                .ToArray();

            var result = await base.SaveChangesAsync(cancellationToken);

            await DispatchEvents(events);

            return result;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            builder.ApplyGlobalFilters<ISoftDelete>(s => s.Deleted == null);

            // Network -> Stations
            builder.Entity<ChargingStation>()
                   .HasOne(cs => cs.Network)
                   .WithMany(n => n.ChargingStations)
                   .HasForeignKey(cs => cs.NetworkId)
                   .OnDelete(DeleteBehavior.Cascade);

            // ChargingSession mapping
            builder.Entity<ChargingSession>(b =>
            {
                b.ToTable("ChargingSessions");
                b.HasKey(x => x.Id);

                b.HasIndex(x => x.StationId);
                b.HasIndex(x => x.TransactionId);
                b.HasIndex(x => x.StartTimeUtc);

                b.Property(x => x.EnergyKWh).HasColumnType("decimal(18,3)");
                b.Property(x => x.Cost).HasColumnType("decimal(18,2)");
                b.Property(x => x.Currency).HasMaxLength(8);
                b.Property(x => x.IdTag).HasMaxLength(100);

                b.HasOne(x => x.Station)
                 .WithMany(s => s.Sessions)
                 .HasForeignKey(x => x.StationId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Identity mappings (kept from your file)
            builder.Entity<ApplicationRoleClaim>(entity =>
            {
                entity.HasOne(rc => rc.Role)
                      .WithMany(r => r.RoleClaims)
                      .HasForeignKey(rc => rc.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ApplicationUserClaim>(entity =>
            {
                entity.HasOne(uc => uc.User)
                      .WithMany(u => u.UserClaims)
                      .HasForeignKey(uc => uc.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ApplicationUserLogin>(entity =>
            {
                entity.HasOne(ul => ul.User)
                      .WithMany(u => u.Logins)
                      .HasForeignKey(ul => ul.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ApplicationUserRole>(entity =>
            {
                entity.HasOne(ur => ur.User)
                      .WithMany(u => u.UserRoles)
                      .HasForeignKey(ur => ur.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ur => ur.Role)
                      .WithMany(r => r.UserRoles)
                      .HasForeignKey(ur => ur.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ApplicationUserToken>(entity =>
            {
                entity.HasOne(ut => ut.User)
                      .WithMany(u => u.Tokens)
                      .HasForeignKey(ut => ut.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private async Task DispatchEvents(DomainEvent[] events)
        {
            foreach (var @event in events)
            {
                @event.IsPublished = true;
                await _domainEventService.Publish(@event);
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    "Server=172.17.0.2,1433;Database=VecoAvempace_PROD;User Id=sa;Password=Avempace0000!;MultipleActiveResultSets=true;"
                );
            }
        }
    }
}
