using Application.Common.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// DateTimeService class
/// </summary>
public class DateTimeService : IDateTime
{
    public DateTime Now => DateTime.Now;
}
