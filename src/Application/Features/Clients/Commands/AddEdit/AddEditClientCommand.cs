
using Domain.Enums;

namespace Application.Features.Clients.Commands.AddEdit;

/// <summary>
/// AddEditClientCommand class
/// </summary>
public class AddEditClientCommand : ClientDto, IRequest<Result<int>>, IMapFrom<Client>, ICacheInvalidator
{
    public TypeWFEnum TypeWF { get; set; }
    public string Distance { get; set; }
    public string IsConsumptionMonitoring { get; set; }

    public string CacheKey => ClientCacheKey.GetAllCacheKey;

    public CancellationTokenSource ResetCacheToken => ClientCacheKey.SharedExpiryTokenSource();
}

