using CR.Service;
using CR.Service.Dtos.User;
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

        return Ok(new { user = userDetails, token });
    }
}
