using FirstOfficer.Data.Attributes;

namespace FirstOfficer.Data
{
    public abstract class Entity : IEntity
    {
        [Queryable]
        [OrderBy]
        public virtual long Id { get; set; }

        [TextSize(64)] 
        [Queryable]
        public virtual string? Checksum { get; set; }
    }
}
