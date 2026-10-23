using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class EditionSubEdition
    {
        [Column("edition_id")]
        public int EditionId { get; set; }

        [Column("subedition_id")]
        public int SubEditionId { get; set; }
        public Edition Edition { get; set; } = null!;
        public SubEdition SubEdition { get; set; } = null!;
    }
}