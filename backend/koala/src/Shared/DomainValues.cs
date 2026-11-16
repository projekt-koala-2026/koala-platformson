global using koala.src.Shared;

namespace koala.src.Shared;

/// <summary>Shared persisted string values used across modules and tests.</summary>
public static class RoleNames
{
    public const string NAME_ORGANIZATION_ADMIN = "ORGANIZATION_ADMIN";
    public const string NAME_ORGANIZATION_EDITOR = "ORGANIZATION_EDITOR";
    public const string NAME_ORGANIZATION_REVIEWER = "ORGANIZATION_REVIEWER";
    public const string NAME_TEAM_CAPTAIN = "TEAM_CAPTAIN";
    public const string NAME_TEAM_GUARDIAN = "TEAM_GUARDIAN";
}

public static class LinkTypes
{
    public const string TYPE_PASSWORD_RESET = "PASSWORD_RESET";
    public const string TYPE_REGISTRATION = "REGISTRATION";
}

public static class RodoTypes
{
    public const string TYPE_JPG = ".jpg";
    public const string TYPE_JPEG = ".jpeg";
    public const string TYPE_PNG = ".png";
    public const string TYPE_WEBP = ".webp";
    public const string TYPE_PDF = ".pdf";

    public static IReadOnlyCollection<string> ALLOWED_EXTENSIONS { get; } = Array.AsReadOnly(new[]
    {
        TYPE_JPG,
        TYPE_JPEG,
        TYPE_PNG,
        TYPE_WEBP,
        TYPE_PDF
    });
}

public static class PublicFileTypes
{
    public const string TYPE_TXT = ".txt";
    public const string TYPE_CSV = ".csv";
    public const string TYPE_DOCS = ".docs";
    public const string TYPE_DOC = ".doc";
    public const string TYPE_DOCX = ".docx";
    public const string TYPE_XLS = ".xls";
    public const string TYPE_XLSX = ".xlsx";
    public const string TYPE_XLSM = ".xlsm";
    public const string TYPE_XLSB = ".xlsb";

    public static IReadOnlyCollection<string> ALLOWED_EXTENSIONS { get; } = Array.AsReadOnly(new[]
    {
        TYPE_TXT,
        TYPE_CSV,
        TYPE_DOCS,
        TYPE_DOC,
        TYPE_DOCX,
        TYPE_XLS,
        TYPE_XLSX,
        TYPE_XLSM,
        TYPE_XLSB
    }.Concat(RodoTypes.ALLOWED_EXTENSIONS).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());

    public static IReadOnlyCollection<string> ALLOWED_CONTENT_TYPES { get; } = Array.AsReadOnly(new[]
    {
        "text/plain",
        "text/csv",
        "application/csv",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel.sheet.macroenabled.12",
        "application/vnd.ms-excel.sheet.binary.macroenabled.12",
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/webp"
    });
}

public static class TeamMemberRodoState
{
    public const string RODOSTATE_ACCEPETED = "RODO_ACCEPTED";
    public const string RODOSTATE_REJECTED = "RODO_REJECTED";
    public const string RODOSTATE_UNVERIFIED = "RODO_UNVERIFIED";
    public const string RODOSTATE_NONE = "RODO_NONE";
}

public static class SchoolTypes
    {
        public const string TYPE_1_TIER_1 = "Ogólnokształcąca szkoła muzyczna I stopnia";
        public const string TYPE_2_TIER_1 = "Poznańska szkoła chóralna";
        public const string TYPE_3_TIER_1 = "Szkoła podstawowa";
        public const string TYPE_1_TIER_2 = "Bednarska Szkoła Realna";
        public const string TYPE_2_TIER_2 = "Branżowa szkoła I stopnia";
        public const string TYPE_3_TIER_2 = "Branżowa szkoła II stopnia";
        public const string TYPE_4_TIER_2 = "Liceum ogólnokształcące";
        public const string TYPE_5_TIER_2 = "Liceum ogólnokształcące uzupełniające dla absolwentów zasadniczych szkół zawodowych";
        public const string TYPE_6_TIER_2 = "Liceum profilowane";
        public const string TYPE_7_TIER_2 = "Szkoła policealna";
        public const string TYPE_8_TIER_2 = "Szkoła specjalna przysposabiająca do pracy";
        public const string TYPE_9_TIER_2 = "Technikum";
        public const string TYPE_10_TIER_2 = "Technikum uzupełniające dla absolwentów zasadniczych szkół zawodowych";
    }

public static class LifecycleStates
{
    public const string STATE_ACTIVE = "ACTIVE";
    public const string STATE_INACTIVE = "INACTIVE";
    public const string STATE_CLOSED = "CLOSED";
}

public static class CompetitionTiers
{
    public const string TIER_1 = "TIER_1";
    public const string TIER_2 = "TIER_2";
}

public static class TaskSubmissionStates
{
    public const string STATE_UNCHECKED = "UN_CHECKED";
}
