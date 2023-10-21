namespace Astrolabe.LocalUsers;

public record ChangePassword(string OldPassword, string Password, string Confirm) : IPasswordHolder;

public record AuthenticateRequest(string Username, string Password, bool RememberMe);