namespace FirstOfficer.Mapping;

public interface IMapper
{
    T Map<T>(object source);
}