using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class EditionSubEdition
    {
        [Column("edition_id")]
        public Guid EditionId { get; set; }

        [Column("subedition_id")]
        public Guid SubEditionId { get; set; }
        public Edition Edition { get; set; } = null!;
        public SubEdition SubEdition { get; set; } = null!;
    }
}