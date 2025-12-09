using System.Security.Claims;
using Astrolabe.LocalUsers;
using AstrolabeApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstrolabeApp.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : AbstractLocalUserController<NewUser, Guid>
{
    public UsersController(ILocalUserService<NewUser, Guid> userService)
        : base(userService)
    {
    }

    protected override Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userId;
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [Authorize]
    [HttpGet("profile")]
    public IActionResult GetProfile()
    {
        var userId = GetUserId();
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var name = User.FindFirst(ClaimTypes.Name)?.Value;
        var firstName = User.FindFirst("firstName")?.Value;
        var lastName = User.FindFirst("lastName")?.Value;

        return Ok(new
        {
            UserId = userId,
            Email = email,
            Name = name,
            FirstName = firstName,
            LastName = lastName
        });
    }
}
