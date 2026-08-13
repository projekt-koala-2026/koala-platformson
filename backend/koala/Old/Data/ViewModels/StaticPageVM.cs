using FluentValidation;
using Microsoft.AspNetCore.Mvc;

//FIXME: MAKE SURE THE ROLES ARE VALIDATED LATTER

namespace koala.Data.ViewModels
{
    public class StaticPageInfoVM
    {
        public string MarkdownBody { get; set; }
    }

    public class StaticPageUpdateVM
    {
        public string MarkdownBody { get; set; }
    }
}