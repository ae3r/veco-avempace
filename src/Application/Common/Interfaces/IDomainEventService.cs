
using Domain.Common;

namespace Application.Common.Interfaces;

/// <summary>
/// IDomainEventService interface
/// </summary>
public interface IDomainEventService
{
    Task Publish(DomainEvent domainEvent);
}
