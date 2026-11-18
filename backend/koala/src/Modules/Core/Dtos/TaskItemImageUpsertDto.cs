using FluentValidation;
using koala.src.Shared;
using Microsoft.AspNetCore.Http;

namespace koala.src.Modules.Core.Dtos
{
    public record TaskItemImageUpsertDto(
        IFormFile File,
        int Version
    );
}
