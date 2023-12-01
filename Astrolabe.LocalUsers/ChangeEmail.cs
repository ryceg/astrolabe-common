namespace Astrolabe.LocalUsers;

public record ChangeEmail(string Password, string NewEmail) : IPasswordHolder;