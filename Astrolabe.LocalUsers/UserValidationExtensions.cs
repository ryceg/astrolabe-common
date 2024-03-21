using System.Linq.Expressions;
using Astrolabe.Common;
using FluentValidation;

namespace Astrolabe.LocalUsers;

public static class UserValidationExtensions
{
    public static void RulesForEmail<T>(this AbstractValidator<T> v, Expression<Func<T, string>> prop, int sameEmail, LocalUserMessages messages)
    {
        v.RuleFor(prop).NotEmpty();
        v.RuleFor(prop).Must(ValidationUtils.IsValidEmail).WithMessage(messages.EmailInvalid);
        v.RuleFor(prop).Must(_ => sameEmail == 0).WithMessage(messages.AccountExists);
    }
}