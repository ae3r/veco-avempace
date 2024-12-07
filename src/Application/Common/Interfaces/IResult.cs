
namespace Application.Common.Interfaces;

/// <summary>
/// IResult interface
/// </summary>
public interface IResult
{
    string[] Errors { get; set; }

    bool Succeeded { get; set; }
}
public interface IResult<out T> : IResult
{
    T Data { get; }
}
