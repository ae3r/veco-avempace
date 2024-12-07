using Domain.Common;

namespace Application.Common.Models;

/// <summary>
/// DomainEventNotification class
/// </summary>
/// <typeparam name="TDomainEvent"></typeparam>
public class DomainEventNotification<TDomainEvent> : INotification where TDomainEvent : DomainEvent
{
    /// <summary>
    /// Constructor : Initializes a new instance of DomainEventNotification
    /// </summary>
    /// <param name="domainEvent"></param>
    public DomainEventNotification(TDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }

    public TDomainEvent DomainEvent { get; }
}
