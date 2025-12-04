using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Astrolabe.LocalUsers;
using AstrolabeApp.Data.EF;
using AstrolabeApp.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AstrolabeApp.Services;

public class LocalUserService : AbstractLocalUserService<NewUser, Guid>
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalUserService> _logger;

    public LocalUserService(
        AppDbContext context,
        IConfiguration configuration,
        IPasswordHasher passwordHasher,
        ILogger<LocalUserService> logger,
        LocalUserMessages? messages = null
    )
        : base(passwordHasher, messages)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task SendVerificationEmail(NewUser newUser, string verificationCode)
    {
        // TODO: Implement email sending with your preferred email service
        // For development, log the verification code
        _logger.LogInformation(
            "Verification email for {Email}: {VerificationCode}",
            newUser.Email,
            verificationCode
        );

        // Example implementation with an email service:
        // await _emailService.SendEmail(new EmailMessage
        // {
        //     To = newUser.Email,
        //     Subject = "Verify your email",
        //     Body = $"Your verification link: {_configuration["AppUrl"]}/verify?verificationCode={verificationCode}"
        // });

        await Task.CompletedTask;
    }

    protected override async Task CreateUnverifiedAccount(
        NewUser newUser,
        string hashedPassword,
        string verificationCode
    )
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = newUser.Email.ToLowerInvariant(),
            FirstName = newUser.FirstName,
            LastName = newUser.LastName,
            HashedPassword = hashedPassword,
            VerificationCode = verificationCode,
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    protected override async Task<int> CountExistingForEmail(string email)
    {
        return await _context.Users.CountAsync(u => u.Email == email.ToLowerInvariant());
    }

    protected override async Task<string?> VerifyAccountCode(string code)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.VerificationCode == code);

        if (user == null)
            return null;

        user.EmailVerified = true;
        user.VerificationCode = null;
        await _context.SaveChangesAsync();

        return GenerateToken(user);
    }

    protected override async Task<string?> MfaVerifyAccountForUserId(
        MfaAuthenticateRequest mfaAuthenticateRequest
    )
    {
        // Find user by token (in this case, we use email stored in token during signup)
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Email == mfaAuthenticateRequest.Token
        );

        if (user == null)
            return null;

        if (user.MfaCode != mfaAuthenticateRequest.Code || user.MfaCodeExpiry < DateTime.UtcNow)
            return null;

        user.MfaCode = null;
        user.MfaCodeExpiry = null;
        user.EmailVerified = true;
        await _context.SaveChangesAsync();

        return GenerateToken(user);
    }

    protected override async Task<string?> AuthenticatedHashed(
        AuthenticateRequest authenticateRequest,
        string hashedPassword
    )
    {
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Email == authenticateRequest.Username.ToLowerInvariant()
            && u.HashedPassword == hashedPassword
        );

        if (user == null || !user.EmailVerified)
            return null;

        // Check if MFA is required
        if (!string.IsNullOrEmpty(user.MfaNumber))
        {
            // Return a special token indicating MFA is required
            // The client should redirect to MFA page
            return $"mfa:{user.Email}";
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return GenerateToken(user);
    }

    protected override async Task<bool> SendCode(MfaCodeRequest mfaCodeRequest)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Email == mfaCodeRequest.Token.ToLowerInvariant()
        );

        if (user == null || string.IsNullOrEmpty(user.MfaNumber))
            return false;

        var code = GenerateMfaCode();
        user.MfaCode = code;
        user.MfaCodeExpiry = DateTime.UtcNow.AddMinutes(10);
        await _context.SaveChangesAsync();

        // TODO: Send SMS with code
        _logger.LogInformation("MFA code for {Phone}: {Code}", user.MfaNumber, code);

        return true;
    }

    protected override async Task<bool> SendCode(Guid userId, string? number = null)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        var phoneNumber = number ?? user.MfaNumber;
        if (string.IsNullOrEmpty(phoneNumber))
            return false;

        var code = GenerateMfaCode();
        user.MfaCode = code;
        user.MfaCodeExpiry = DateTime.UtcNow.AddMinutes(10);
        await _context.SaveChangesAsync();

        // TODO: Send SMS with code
        _logger.LogInformation("MFA code for {Phone}: {Code}", phoneNumber, code);

        return true;
    }

    protected override async Task<string?> VerifyMfaCode(string token, string code, string? number)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Email == token.ToLowerInvariant()
        );

        if (user == null)
            return null;

        if (user.MfaCode != code || user.MfaCodeExpiry < DateTime.UtcNow)
            return null;

        user.MfaCode = null;
        user.MfaCodeExpiry = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return GenerateToken(user);
    }

    protected override async Task<bool> VerifyMfaCode(Guid userId, string code, string? number)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        if (user.MfaCode != code || user.MfaCodeExpiry < DateTime.UtcNow)
            return false;

        user.MfaCode = null;
        user.MfaCodeExpiry = null;
        await _context.SaveChangesAsync();

        return true;
    }

    protected override async Task SetResetCodeAndEmail(string email, string resetCode)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Email == email.ToLowerInvariant()
        );

        if (user != null)
        {
            user.ResetCode = resetCode;
            user.ResetCodeExpiry = DateTime.UtcNow.AddHours(24);
            await _context.SaveChangesAsync();

            // TODO: Send password reset email
            _logger.LogInformation("Password reset for {Email}: {ResetCode}", email, resetCode);
        }
    }

    protected override async Task<bool> EmailChangeForUserId(
        Guid userId,
        string hashedPassword,
        string newEmail
    )
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.HashedPassword != hashedPassword)
            return false;

        // Check if new email already exists
        var existingEmail = await _context.Users.AnyAsync(u =>
            u.Email == newEmail.ToLowerInvariant() && u.Id != userId
        );
        if (existingEmail)
            return false;

        user.Email = newEmail.ToLowerInvariant();
        await _context.SaveChangesAsync();

        return true;
    }

    protected override async Task<(bool, Func<string, Task<string>>?)> PasswordChangeForUserId(
        Guid userId,
        string oldHashedPassword
    )
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return (false, null);

        var passwordOk = user.HashedPassword == oldHashedPassword;

        return (
            passwordOk,
            async (newHashedPassword) =>
            {
                user.HashedPassword = newHashedPassword;
                await _context.SaveChangesAsync();
                return GenerateToken(user);
            }
        );
    }

    protected override async Task<Func<string, Task>?> PasswordResetForResetCode(string resetCode)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.ResetCode == resetCode && u.ResetCodeExpiry > DateTime.UtcNow
        );

        if (user == null)
            return null;

        return async (newHashedPassword) =>
        {
            user.HashedPassword = newHashedPassword;
            user.ResetCode = null;
            user.ResetCodeExpiry = null;
            await _context.SaveChangesAsync();
        };
    }

    protected override async Task<bool> ChangeMfaNumberForUserId(
        Guid userId,
        string hashedPassword,
        string newNumber
    )
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.HashedPassword != hashedPassword)
            return false;

        user.MfaNumber = newNumber;
        await _context.SaveChangesAsync();

        return true;
    }

    private string GenerateToken(User user)
    {
        var key = _configuration["Jwt:Key"] ?? "DefaultDevKeyThatShouldBeChanged123!";
        var issuer = _configuration["Jwt:Issuer"] ?? "AstrolabeApp";
        var audience = _configuration["Jwt:Audience"] ?? "AstrolabeApp";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateMfaCode()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }
}
