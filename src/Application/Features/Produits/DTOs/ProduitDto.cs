
namespace Application.Features.Produits.DTOs;

/// <summary>
/// ProduitDto class
/// </summary>
public class ProduitDto : IMapFrom<Produit>
{
    /// <summary>
    /// Mapping
    /// </summary>
    /// <param name="profile"></param>
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Produit, ProduitDto>();
        profile.CreateMap<ProduitDto, Produit>();
        profile.CreateMap<Produit, AddEditProduitCommand>();
        profile.CreateMap<AddEditProduitCommand, Produit>();
    }
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
