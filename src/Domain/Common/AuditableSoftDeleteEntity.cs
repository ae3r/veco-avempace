namespace Domain.Common;

/// <summary>
/// AuditableSoftDeleteEntity abstract class
/// </summary>
public abstract class AuditableSoftDeleteEntity : AuditableEntity, ISoftDelete
{
    public DateTime? Deleted { get; set; }
    public int DeletedBy { get; set; }
}
