using CR.Model.Data;
using CR.Service.Dtos.User;
using Microsoft.EntityFrameworkCore;

namespace CR.Service.Roles;

public class RoleService(IDbContextFactory<CRContext> contextFactory) : IRoleService
{
    private readonly IDbContextFactory<CRContext> _contextFactory = contextFactory;

    public async Task<List<RoleDTO>> GetAllRolesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context
            .Roles.Where(role => role.Name != null)
            .OrderBy(role => role.Name)
            .Select(role => new RoleDTO(role.RoleId, role.Name!))
            .ToListAsync();
    }
}
