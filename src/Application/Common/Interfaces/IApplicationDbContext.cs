using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Application.Common.Interfaces;

/// <summary>
/// IApplicationDbContext interface
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Client> Clients { get; set; }
    //DbSet<Client> Clients { get; set; }
    //DbSet<Fabriquant> Fabriquants { get; set; }
    //DbSet<Destination> Destinations { get; set; }
    //DbSet<Saison> Saisons { get; set; }
    //DbSet<Couleur> Couleurs { get; set; }
    //DbSet<Chaine> Chaines { get; set; }
    //DbSet<TypeArticle> TypesArticles { get; set; }
    //DbSet<Commande> Commandes { get; set; }
    //DbSet<Lot> Lots { get; set; }
    //DbSet<Variante> Variantes { get; set; }
    //DbSet<Produit> Produits { get; set; }
    //DbSet<Audit> Audits { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
