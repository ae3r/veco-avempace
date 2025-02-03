namespace Application.Features.Produits.Commands.AddEdit;

/// <summary>
/// AddEditProduitCommand class
/// </summary>
public class AddEditProduitCommand : ProduitDto, IRequest<Result<int>>, IMapFrom<Produit>, ICacheInvalidator
{
    public string CacheKey => ProduitCacheKey.GetAllCacheKey;

    public CancellationTokenSource ResetCacheToken => ProduitCacheKey.SharedExpiryTokenSource();
}

