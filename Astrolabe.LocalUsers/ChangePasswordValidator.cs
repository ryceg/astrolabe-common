using FluentValidation;

namespace Astrolabe.LocalUsers;

public class ChangePasswordValidator : AbstractValidator<ChangePassword>
{
    public ChangePasswordValidator(bool oldOk, LocalUserMessages messages)
    {
        RuleFor(x => x.OldPassword).Must(x => oldOk).WithMessage(messages.PasswordWrong);
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.Confirm).Must((nu, c) => nu.Password == c).WithMessage(messages.PasswordMismatch);
    }
}