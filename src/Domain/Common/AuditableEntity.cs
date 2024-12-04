namespace Domain.Common;

/// <summary>
/// AuditableEntity abstract class
/// </summary>
public abstract class AuditableEntity : IEntity
{
    public DateTime Created { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? LastModified { get; set; }

    public int LastModifiedBy { get; set; }
}

