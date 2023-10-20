using FluentValidation;

namespace Astrolabe.LocalUsers;

public class ChangePasswordValidator : AbstractValidator<ChangePassword>
{
    public ChangePasswordValidator(bool oldOk)
    {
        RuleFor(x => x.OldPassword).Must(x => oldOk).WithMessage("Incorrect password");
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.Confirm).Must((nu, c) => nu.Password == c).WithMessage("Password confirmation does not match");
    }
}