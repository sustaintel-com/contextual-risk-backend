using System.Security.Claims;
using CR.Service;
using CR.Service.Dtos.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CR.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDTO login)
    {
        var user = await _authService.AuthenticateUserAsync(login.Username, login.Password);

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var userDetails = await _authService.AllUserAsync(user);
        var token = await _authService.GenerateJSONWebToken(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
        };

        claims.AddRange(userDetails.RoleNames.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                // ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(1),
                AllowRefresh = false,
                //AllowRefresh = true,
            }
        );

        return Ok(new { user = userDetails });
    }

    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        var roleNames = User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToList();

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        return Ok(
            new
            {
                user = new
                {
                    id = userId,
                    username,
                    email,
                    roleNames,
                },
            }
        );
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "Logged out successfully" });
    }
}
