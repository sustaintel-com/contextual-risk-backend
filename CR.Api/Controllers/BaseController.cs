using System.Security.Claims;
using CR.Model.Data;
using CR.Model.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CR.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class BaseController(IDbContextFactory<CRContext> contextFactory) : ControllerBase
{
    private readonly IDbContextFactory<CRContext> _contextFactory = contextFactory;

    protected async Task<User> GetCurrentUserAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var currentUserIdString = User
            ?.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        if (string.IsNullOrEmpty(currentUserIdString))
            throw new UnauthorizedAccessException("User ID not found in claims.");

        if (!IdType.TryParse(currentUserIdString, out IdType currentUserId))
            throw new Exception("Invalid user ID format.");

        var user = await context.Users.Where(u => u.Id == currentUserId).FirstOrDefaultAsync();

        if (user == null)
            throw new Exception("User not found.");

        return user;
    }
}
