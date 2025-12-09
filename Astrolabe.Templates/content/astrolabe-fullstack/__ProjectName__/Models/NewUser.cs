using Astrolabe.LocalUsers;

namespace AstrolabeApp.Models;

/// <summary>
/// DTO for creating a new user account
/// </summary>
public class NewUser : ICreateNewUser
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Confirm { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
