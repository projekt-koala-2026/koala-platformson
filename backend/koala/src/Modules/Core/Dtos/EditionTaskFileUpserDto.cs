using FluentValidation;
using koala.src.Shared;
using Microsoft.AspNetCore.Http;

namespace koala.src.Modules.Core.Dtos
{
    public record EditionTaskFileUpserDto(
        DateTime WindowOpen,
        DateTime WindowClose,
        IFormFile File,
        int Version
    );
}
