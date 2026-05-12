using System.ComponentModel.DataAnnotations;

namespace koala.Data.ViewModels
{
    public class KoalicjantCreateVM
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public string ProfilePicture {get; set; }

        public string Description { get; set; }
    }

    public class KoalicjantUpdateVM
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public string ProfilePicture {get; set; }

        public string Description { get; set; }
    }
}