using FluentValidation;

namespace Astrolabe.LocalUsers;

public static class LocalUserValidationExtensions
{

    public static void AddNewUserRules<TNewUser>(this AbstractValidator<TNewUser> validator, int sameEmail)
        where TNewUser : ICreateNewUser
    {
        validator.RuleFor(x => x.Email).NotEmpty();
        validator.RuleFor(x => x.Email).Must(_ => sameEmail == 0).WithMessage("Account already exists");
        validator.RuleFor(x => x.Password).NotEmpty();
        validator.RuleFor(x => x.Confirm).Must((nu, c) => nu.Password == c).WithMessage("Password confirmation does not match");
    }
    
}