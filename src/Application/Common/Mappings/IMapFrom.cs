
namespace Application.Common.Mappings;

/// <summary>
/// Mapping
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IMapFrom<T>
{
    void Mapping(Profile profile) => profile.CreateMap(typeof(T), GetType(), MemberList.None).ReverseMap();
}
