using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Produit
/// </summary>
public class Produit : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public string Type { get; set; }
    public string Info { get; set; }
    public string SrcImage { get; set; }
    public bool IsNew { get; set; }
    public bool IsDiscount { get; set; }
    public DateTime Created { get; set; }
}


