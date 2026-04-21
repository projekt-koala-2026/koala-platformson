using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace koala.Data
{
    public enum FileTypes
    {
        IMAGE = 0,
        TXT = 1
    }

    [Table("PublicFiles")]
    public class PublicFile
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid(); //ID WILL BE THE FILE NAME ON SYSTEM
        public string Title { get; set; }
        public string Format { get; set; } //STORE THE EXTENSION HERE .txt .jpg ETC
        public FileTypes Type { get; set; } //USE THE TYPE TO PATH TO CORECT SUB FOLDER public/IMAGE/id.jpg public/TXT/xdd.txt ETC
    }

}