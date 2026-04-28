using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace koala.Data.ViewModels
{
    public class FileInfoVM
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string FilePath { get; set; }
    }

    public class FileCreateVM
    {
        public string Title { get; set; }
        public IFormFile File { get; set; }
    }

    public class FileDeleteVM
    {
        public Guid Id { get; set; }
    }

}