namespace Astrolabe.LocalUsers;

public interface ILocalUserService<TNewUser, TUserId> where TNewUser : ICreateNewUser
{
    Task CreateAccount(TNewUser newUser);
    Task<string> VerifyAccount(string code);
    Task<string> Authenticate(AuthenticateRequest authenticateRequest);
    Task ForgotPassword(string email);
    Task<string> ChangePassword(ChangePassword change, string? resetCode, Func<TUserId> userId);
}