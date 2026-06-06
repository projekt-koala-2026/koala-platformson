namespace koala.Data.ViewModels
{

    public class SchoolFileCreate
    {
        public string Title { get; set; }
        public IFormFile File { get; set; }
    }

    //NOTE:
    // maybe store the schools files later on for versioning?
    // public class SchoolFileInfo
    // {

    // }

    public class SchoolInfoVM
    {
        public int RSPO { get; set; }
        public string Name { get; set; }
        public string? NameShort { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string Type { get; set; }
        public string Addres { get; set; }
    }

    public class SchoolCreateVM
    {
        public int RSPO { get; set; }
        public string Name { get; set; }
        public string? NameShort { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string Type { get; set; }
        public string Addres { get; set; }
    }

    public class SchoolEditNameVM
    {
        public int RSPO { get; set; }
        public string Name { get; set; }
    }

    public class SchoolEditNameShortVM
    {
        public int RSPO { get; set; }
        public string NameShort { get; set; }
    }

    public class SchoolDeleteVM
    {
        public int RSPO { get; set; }
    }
}
