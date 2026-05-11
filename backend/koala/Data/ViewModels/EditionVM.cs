using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace koala.Data.ViewModels
{
    public class EditionInfoVM
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
    }

    public class EditionCreateVM
    {
        public string Title { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
    }

    public class EditionUpdateStartDateVM
    {
        public Guid Id { get; set; }
        public DateTimeOffset StartDate { get; set; }
    }

    public class EditionUpdateEndDateVM
    {
        public Guid Id { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class EditionUpdateTitleVM
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
    }

    public class EditionDeleteVM
    {
        public Guid Id { get; set; }
    }

}