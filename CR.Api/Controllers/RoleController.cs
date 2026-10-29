using CR.Model.Data;
using CR.Service.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CR.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class RoleController(IRoleService RoleService, IDbContextFactory<CRContext> contextFactory)
    : BaseController(contextFactory)
{
    private readonly IRoleService _roleService = RoleService;

    [HttpGet]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await _roleService.GetAllRolesAsync();
        return Ok(roles);
    }
}
