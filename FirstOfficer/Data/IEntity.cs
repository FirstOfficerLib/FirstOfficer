namespace FirstOfficer.Data
{
    public interface IEntity
    {

        long Id { get; set; }
        string? Checksum { get; set; }

    }
}