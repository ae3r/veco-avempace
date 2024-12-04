namespace Domain.Common;

/// <summary>
/// ISoftDelete interface
/// </summary>
public interface ISoftDelete
{
    DateTime? Deleted { get; set; }
    int DeletedBy { get; set; }
}
