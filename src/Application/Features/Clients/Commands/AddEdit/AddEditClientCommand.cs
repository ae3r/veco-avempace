
namespace Application.Features.Clients.Commands.AddEdit;

/// <summary>
/// AddEditClientCommand class
/// </summary>
public class AddEditClientCommand : ClientDto, IRequest<Result<int>>, IMapFrom<Client>, ICacheInvalidator
{
    public string CacheKey => ClientCacheKey.GetAllCacheKey;

    public CancellationTokenSource ResetCacheToken => ClientCacheKey.SharedExpiryTokenSource();
}

