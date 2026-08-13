using FluentValidation;
using Microsoft.AspNetCore.Mvc;

//FIXME: MAKE SURE THE ROLES ARE VALIDATED LATTER

namespace koala.Data.ViewModels
{
    public class UserInfoVM
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
    }

    public class UserCreateVM
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public List<string> Roles { get; set; }
    }

    public class UserDeleteVM
    {
        public Guid Id { get; set; }
    }

    public class UserChangeEmailVM
    {
        public Guid Id { get; set; }
        public string Password { get; set; }
        public string NewEmail { get; set; }
    }

    public class UserChangePasswordVM
    {
        public Guid Id { get; set; }
        public string Password { get; set; }
        public string NewPassword { get; set; }
    }

    public class UserChangeRolesVM
    {
        public Guid Id { get; set; }
        public List<string> NewRoles { get; set; }
    }

    public class UserLoginVM
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class UserCreateNormalVM
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public List<string> Roles { get; set; }
    }

    public class UserCreateNormalValidator : AbstractValidator<UserCreateNormalVM>
    {
        private static readonly string[] AllowedRoles =
        {
            "CAPTAIN",
            "GUARDIAN"
        };

        public UserCreateNormalValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .WithMessage("This is not a valid email");

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Matches(@"[a-z]")
                .Matches(@"[A-Z]")
                .Matches(@"\d")
                .Matches(@"[\W_]")
                .WithMessage("This is not a valid password");

            RuleFor(x => x.Roles)
                .NotNull()
                .NotEmpty()
                .Must(roles =>
                    roles.Count == 1 &&
                    AllowedRoles.Contains(roles[0]))
                .WithMessage("Role must be either CAPTAIN or GUARDIAN");

            RuleForEach(x => x.Roles)
                .NotNull()
                .NotEmpty();
        }
    }

    public class UserCreateValidator : AbstractValidator<UserCreateVM>
    {
        public UserCreateValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .WithMessage("This is not a valid email");

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Matches(@"[a-z]")
                .Matches(@"[A-Z]")
                .Matches(@"\d")
                .Matches(@"[\W_]")
                .WithMessage("This is not a valid password");

            RuleFor(x => x.Roles)
                .NotNull()
                .NotEmpty()
            //    .Must(r => r.Distinct(StringComparer.OrdinalIgnoreCase).Count() == r.Count)
                .WithMessage("This field cannot contain duplicate");

            RuleForEach(x => x.Roles)
                .NotNull()
                .NotEmpty();
                // .Must(role => AllowedRoles.Contains(role))
                // .WithMessage(role => $"This role '{role}' is not valid.");
        }
    }

    public class UserDeleteValidator : AbstractValidator<UserDeleteVM>
    {
        public UserDeleteValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("This filed cannot be empty");
        }
    }

    public class UserChangeEmailValidator : AbstractValidator<UserChangeEmailVM>
    {
        public UserChangeEmailValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("This filed cannot be empty");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("This filed cannot be empty");

            RuleFor(x => x.NewEmail)
                .NotEmpty()
                .EmailAddress()
                .WithMessage("This is not a valid email");
        }
    }

    public class UserChangePasswordValidator : AbstractValidator<UserChangePasswordVM>
    {
        public UserChangePasswordValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("This filed cannot be empty");
            
            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("This filed cannot be empty");
            
            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .MinimumLength(8)
                .Matches(@"[a-z]")
                .Matches(@"[A-Z]")
                .Matches(@"\d")
                .Matches(@"[\W_]")
                .WithMessage("This is not a valid password");
        }
    }

    public class UserChangeRolesValidator : AbstractValidator<UserChangeRolesVM>
    {
        public UserChangeRolesValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("This filed cannot be empty");

            RuleFor(x => x.NewRoles)
                .NotNull()
                .NotEmpty()
                //.Must(r => r.Distinct(StringComparer.OrdinalIgnoreCase).Count() == r.Count)
                .WithMessage("This field cannot contain duplicate");

            RuleForEach(x => x.NewRoles)
                .Must(role => !string.IsNullOrWhiteSpace(role))
                .WithMessage("Role cannot be empty or whitespace.");
            //     .Must(role => AllowedRoles.Contains(role))
            //     .WithMessage(role => $"This role '{role}' is not valid.");
        }
    }

    public class UserLoginValidator : AbstractValidator<UserLoginVM>
    {
        public UserLoginValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .Must(email => !string.IsNullOrWhiteSpace(email))
                .WithMessage("This filed cannot be empty");

            RuleFor(x => x.Password)
                .NotEmpty()
                .Must(email => !string.IsNullOrWhiteSpace(email))
                .WithMessage("This filed cannot be empty");
        }
    }
}