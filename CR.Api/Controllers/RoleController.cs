using CR.Model.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CR.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public sealed class RoleController(IDbContextFactory<CRContext> contextFactory) : ControllerBase
{
    private readonly IDbContextFactory<CRContext> _contextFactory = contextFactory;

    [HttpGet]
    public async Task<IActionResult> GetAllRoles()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var roles = await context.Roles
            .Where(role => role.Name != null)
            .OrderBy(role => role.Name)
            .Select(role => new
            {
                role.RoleId,
                Name = role.Name!,
            })
            .ToListAsync();

        return Ok(roles);
    }
}
