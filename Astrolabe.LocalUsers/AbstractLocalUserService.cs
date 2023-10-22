using Astrolabe.Common.Exceptions;
using FluentValidation;

namespace Astrolabe.LocalUsers;

public abstract class AbstractLocalUserService<TNewUser, TUserId> : ILocalUserService<TNewUser, TUserId> where TNewUser : ICreateNewUser
{
    private readonly IPasswordHasher _passwordHasher;

    protected AbstractLocalUserService(IPasswordHasher passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }
    
    public async Task CreateAccount(TNewUser newUser)
    {
        var existingAccounts = await CountExisting(newUser);
        var validator = new CreateNewUserValidator<TNewUser>(existingAccounts);
        await ApplyCreationRules(validator);
        ApplyPasswordRules(validator);
        await validator.ValidateAndThrowAsync(newUser);
        var emailCode = CreateEmailCode();
        await CreateUnverifiedAccount(newUser, _passwordHasher.Hash(newUser.Password), emailCode);
        await SendVerificationEmail(newUser, emailCode);
    }

    protected abstract Task SendVerificationEmail(TNewUser newUser, string verificationCode);

    protected abstract Task CreateUnverifiedAccount(TNewUser newUser, string hashedPassword, string verificationCode);
    
    protected virtual string CreateEmailCode()
    {
        return Guid.NewGuid().ToString();
    }

    protected virtual async Task ApplyCreationRules(CreateNewUserValidator<TNewUser> validator)
    {
        
    }

    protected virtual void ApplyPasswordRules<T>(AbstractValidator<T> validator) where T : IPasswordHolder
    {
        validator.RuleFor(x => x.Password).MinimumLength(8);
    }

    protected abstract Task<int> CountExisting(TNewUser newUser);
    
    public async Task<string> VerifyAccount(string code)
    {
        var token = await VerifyAccountCode(code);
        if (token == null) throw new UnauthorizedException();
        return token;
    }

    protected abstract Task<string?> VerifyAccountCode(string code);

    public async Task<string> Authenticate(AuthenticateRequest authenticateRequest)
    {
        var hashed = _passwordHasher.Hash(authenticateRequest.Password);
        var token = await AuthenticatedHashed(authenticateRequest, hashed);
        if (token == null) throw new UnauthorizedException();
        return token;
    }

    protected abstract Task<string> AuthenticatedHashed(AuthenticateRequest authenticateRequest, string hashedPassword);

    public async Task ForgotPassword(string email)
    {
        var resetCode = CreateEmailCode();
        await SetResetCodeAndEmail(email, resetCode);
    }
    
    protected abstract Task SetResetCodeAndEmail(string email, string resetCode);

    public async Task<string> ChangePassword(ChangePassword change, string? resetCode, Func<TUserId> userId)
    {
        (bool, Func<string, Task<string>>?) apply; 
        if (!string.IsNullOrWhiteSpace(resetCode))
        {
            apply = (true, await PasswordChangeForResetCode(resetCode));
        }
        else
        {
            apply = await PasswordChangeForUserId(userId(), _passwordHasher.Hash(change.OldPassword));
        }
        var (passwordOk, applyChange) = apply;
        if (applyChange == null)
            throw new NotFoundException();
        
        var validator = new ChangePasswordValidator(passwordOk);
        ApplyPasswordRules(validator);
        await validator.ValidateAndThrowAsync(change);
        return await applyChange(_passwordHasher.Hash(change.Password));
   }

    protected abstract Task<(bool, Func<string, Task<string>>?)> PasswordChangeForUserId(TUserId userId, string hashedPassword);

    protected abstract Task<Func<string, Task<string>>?> PasswordChangeForResetCode(string resetCode);
}