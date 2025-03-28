using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Identity;
using Infrastructure.Persistence.Extensions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure
{
    /// <summary>
    /// ApplicationDbContext class
    /// </summary>
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

        /// <summary>
        /// Parameterless constructor
        /// </summary>
        public ApplicationDbContext() : base() { }

        /// <summary>
        /// Main constructor for DI
        /// </summary>
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

        // Your domain entities
        public DbSet<Client> Clients { get; set; }
        public DbSet<Produit> Produits { get; set; }
        public DbSet<ChargingStation> ChargingStations { get; set; }
        public DbSet<Network> Networks { get; set; }

        /// <summary>
        /// SaveChangesAsync override for auditing & domain events
        /// </summary>
        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = new CancellationToken())
        {
            // 1) Handle AuditableEntity & SoftDelete
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

            // 2) Gather domain events
            var events = ChangeTracker.Entries<IHasDomainEvent>()
                .Select(x => x.Entity.DomainEvents)
                .SelectMany(x => x)
                .Where(domainEvent => !domainEvent.IsPublished)
                .ToArray();

            // 3) Save
            var result = await base.SaveChangesAsync(cancellationToken);

            // 4) Dispatch domain events
            await DispatchEvents(events);

            return result;
        }

        /// <summary>
        /// OnModelCreating override
        /// </summary>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // 1) Let IdentityDbContext configure identity tables
            base.OnModelCreating(builder);

            // 2) Apply EF configurations in this assembly (if any)
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // 3) Apply global filter for soft delete
            builder.ApplyGlobalFilters<ISoftDelete>(s => s.Deleted == null);


            // One Network has many ChargingStations, each ChargingStation has one Network
            builder.Entity<ChargingStation>()
                   .HasOne(cs => cs.Network)
                   .WithMany(n => n.ChargingStations)
                   .HasForeignKey(cs => cs.NetworkId)
                   .OnDelete(DeleteBehavior.Cascade);

            // ==================================================
            // 5) IDENTITY: map each custom Identity class
            //    to a single FK property (no "UserId1"/"RoleId1").
            // ==================================================

            // a) ApplicationRoleClaim -> ApplicationRole
            builder.Entity<ApplicationRoleClaim>(entity =>
            {
                // Only the base class property "RoleId"
                entity.HasOne(rc => rc.Role)
                      .WithMany(r => r.RoleClaims)
                      .HasForeignKey(rc => rc.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // b) ApplicationUserClaim -> ApplicationUser
            builder.Entity<ApplicationUserClaim>(entity =>
            {
                entity.HasOne(uc => uc.User)
                      .WithMany(u => u.UserClaims)
                      .HasForeignKey(uc => uc.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // c) ApplicationUserLogin -> ApplicationUser
            builder.Entity<ApplicationUserLogin>(entity =>
            {
                entity.HasOne(ul => ul.User)
                      .WithMany(u => u.Logins)
                      .HasForeignKey(ul => ul.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // d) ApplicationUserRole -> (ApplicationUser, ApplicationRole)
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

            // e) ApplicationUserToken -> ApplicationUser
            builder.Entity<ApplicationUserToken>(entity =>
            {
                entity.HasOne(ut => ut.User)
                      .WithMany(u => u.Tokens)
                      .HasForeignKey(ut => ut.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Done! No references to "UserId1" or "RoleId1."
        }


        /// <summary>
        /// Dispatch domain events to the domainEventService
        /// </summary>
        private async Task DispatchEvents(DomainEvent[] events)
        {
            foreach (var @event in events)
            {
                @event.IsPublished = true;
                await _domainEventService.Publish(@event);
            }
        }

        /// <summary>
        /// Fallback OnConfiguring if no external config is provided
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Adjust connection string if you do not want to rely on "Trusted_Connection"
                optionsBuilder.UseSqlServer(
                    "Server=172.17.0.2,1433;Database=VecoAvempace_PROD;User Id=sa;Password=Avempace0000!;MultipleActiveResultSets=true;"
                );
            }
        }
    }
}
