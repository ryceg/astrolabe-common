using FluentValidation;

namespace Astrolabe.LocalUsers;

public class CreateNewUserValidator<TNewUser> : AbstractValidator<TNewUser> where TNewUser : ICreateNewUser
{
    public CreateNewUserValidator(int sameEmail, LocalUserMessages messages)
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Email).Must(_ => sameEmail == 0).WithMessage(messages.AccountExists);
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.Confirm).Must((nu, c) => nu.Password == c)
            .WithMessage(messages.PasswordMismatch);
    }
}