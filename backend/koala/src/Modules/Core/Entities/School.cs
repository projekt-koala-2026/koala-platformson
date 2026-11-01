using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core
{
    public static class SchoolTiers
    {
        public static string TIER_1 = "TIER_1";
        public static string TIER_2 = "TIER_2";
    };

    public static class SchoolTypes
    {
        public static string TYPE_1_TIER_1 = "Ogólnokształcąca szkoła muzyczna I stopnia";
        public static string TYPE_2_TIER_1 = "Poznańska szkoła chóralna";
        public static string TYPE_3_TIER_1 = "Szkoła podstawowa";
        public static string TYPE_1_TIER_2 = "Bednarska Szkoła Realna";
        public static string TYPE_2_TIER_2 = "Branżowa szkoła I stopnia";
        public static string TYPE_3_TIER_2 = "Branżowa szkoła II stopnia";
        public static string TYPE_4_TIER_2 = "Liceum ogólnokształcące";
        public static string TYPE_5_TIER_2 = "Liceum ogólnokształcące uzupełniające dla absolwentów zasadniczych szkół zawodowych";
        public static string TYPE_6_TIER_2 = "Liceum profilowane";
        public static string TYPE_7_TIER_2 = "Szkoła policealna";
        public static string TYPE_8_TIER_2 = "Szkoła specjalna przysposabiająca do pracy";
        public static string TYPE_9_TIER_2 = "Technikum";
        public static string TYPE_10_TIER_2 = "Technikum uzupełniające dla absolwentów zasadniczych szkół zawodowych";
    };

    public class School
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("name_full")]
        public string NameFull { get; set; }
        [Column("name_short")]
        public string NameShort { get; set; }
        [Column("rspo")]
        public string Rspo { get; set; }
        [Column("state")]
        public string State { get; set; }
        [Column("city")]
        public string City { get; set; }
        [Column("road")]
        public string Road { get; set; }
        [Column("building")]
        public string Building { get; set; }
        [Column("type")]
        public string Type { get; set; }
        [Column("tier")]
        public string Tier { get; set; }
        [Column("email")]
        public string Email { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }
    };
}