using Microsoft.EntityFrameworkCore;

namespace Domain.Common;

public interface IHasDomainEvent
{
    List<DomainEvent> DomainEvents { get; set; }
}

[Keyless]
public abstract class DomainEvent
{
    protected DomainEvent()
    {
        DateOccurred = DateTimeOffset.UtcNow;
    }
    public bool IsPublished { get; set; }
    public DateTimeOffset DateOccurred { get; protected set; } = DateTime.UtcNow;
}
