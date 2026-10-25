using FluentValidation;

namespace koala.src.Modules.Account.Dtos
{
    public record TeamMemberUpsertRequestDto
    (
        string NameFirst,
        string NameLast
    );

    public class TeamMemberUpsertRequestDtoValidator : AbstractValidator<TeamMemberUpsertRequestDto>
    {
        public TeamMemberUpsertRequestDtoValidator()
        {
            RuleFor(x => x.NameFirst)
                .NotEmpty().WithMessage("NameFirst field cannot be empty!")
                .MaximumLength(50).WithMessage("NameFirst field cannot be larger than 50 characters!")
                .Matches(@"^[\p{L}\s-]+$").WithMessage("NameFirst field can only contain letters, spaces, or hyphens!");

            RuleFor(x => x.NameLast)
                .NotEmpty().WithMessage("NameLast field cannot be empty!")
                .MaximumLength(50).WithMessage("NameLast field cannot be larger than 50 characters!")
                .Matches(@"^[\p{L}\s-]+$").WithMessage("NameLast field can only contain letters, spaces, or hyphens!");
        }
    }
}