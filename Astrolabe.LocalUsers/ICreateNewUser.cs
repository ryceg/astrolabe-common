namespace Astrolabe.LocalUsers;

public interface ICreateNewUser : IPasswordHolder
{
    public string Email { get; }
    public string Confirm { get; }
}