using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FirstOfficer.Data.Attributes;

namespace FirstOfficer.Data
{
    public abstract class Entity : IEntity
    {
        [Column("id")]
        [Key]
        [Queryable]
        [OrderBy]
        public virtual long Id { get; set; }

        [TextSize(64)]
        [Column("checksum")]  //for benchmarking for EF Core
        [Queryable]
        public virtual string Checksum { get; set; }
    }
}
